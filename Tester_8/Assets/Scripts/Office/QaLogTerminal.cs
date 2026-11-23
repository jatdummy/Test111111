using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 판정 기록을 모아 두었다가 E로 태블릿에 리포트를 보내 주는 QA 어시스턴트(AI) 단말
// 처음 하는 플레이어용 도우미: 한 판(22:00 → 06:00)에 몇 번만 보낼 수 있고, 다 쓴 뒤 누르면 고장 남
public class QaLogTerminal : MonoBehaviour
{
    private const int MaxStoredLines = 60;
    private const float GlitchInterval = 0.06f;

    // 단말 화면 (상태 글씨 · 화면 재질)
    [SerializeField] private TerminalScreen _terminalScreen;

    // 고장 연출
    [SerializeField] private TerminalBreakEffect _breakEffect;

    // 플레이어 (가까이 왔는지 확인용)
    [SerializeField] private Transform _player;

    // 플레이어 카메라 (화면을 바라보는지 확인용)
    [SerializeField] private Transform _cameraPivot;

    // 리포트를 받을 플레이어 태블릿
    [SerializeField] private CodeTablet _tablet;

    // 가까이 가면 화면 아래에 뜨는 [E] 사용 안내
    [SerializeField] private GameObject _prompt;

    // 사용 안내 문구
    [SerializeField] private UnityEngine.UI.Text _promptText;

    // 이 거리 안에 들어오면 화면이 켜지고 전송할 수 있음
    [SerializeField, Min(0f)] private float _useDistance = 1.2f;

    // 화면 정면에서 이 각도 안에 서 있어야 사용할 수 있음 (옆이나 뒤에서는 안 됨)
    [SerializeField, Range(0f, 180f)] private float _frontAngle = 40f;

    // 카메라가 화면을 이 각도 안으로 바라보고 있어야 사용할 수 있음
    [SerializeField, Range(0f, 180f)] private float _lookAngle = 25f;

    // 리포트에 담는 최근 기록 줄 수
    [SerializeField, Min(1)] private int _reportLines = 9;

    // 전송 중 문구를 보여 주는 시간(초)
    [SerializeField, Min(0.01f)] private float _sendSeconds = 0.8f;

    // 전송 완료 문구를 보여 주는 시간(초)
    [SerializeField, Min(0f)] private float _sentSeconds = 1.5f;

    [Header("고장")]
    // 한 판(22:00 → 06:00)에 태블릿으로 보낼 수 있는 횟수 (다 쓰고 한 번 더 누르면 고장)
    [SerializeField, Min(1)] private int _maxSends = 3;

    // 고장 날 때 글자가 깨지며 깜빡이는 시간(초)
    [SerializeField, Min(0.01f)] private float _breakSeconds = 1.4f;

    // 리포트 받기 (Player/Interact)
    [SerializeField] private InputActionReference _interactAction;

    private readonly QaReportLog _log = new(MaxStoredLines);
    private int _sendCount;
    private bool _hasNewFailure;
    private bool _isBusy;
    private bool _isBroken;

    // 씬이 새로 열릴 때마다(새 판, 엔딩 뒤 다시 시작) 멀쩡한 단말에 횟수도 처음부터
    private void Start() => _terminalScreen.ShowIdle(_hasNewFailure);

    private void Update()
    {
        if (_isBroken) return;

        bool isNear = Vector3.Distance(_player.position, transform.position) <= _useDistance;
        if (!_isBusy)
        {
            _terminalScreen.SetVisible(isNear);
        }

        _terminalScreen.Animate(_hasNewFailure, _isBusy);

        // 화면 바로 앞에서 화면을 보고 있어야 [E] 안내가 뜨고 사용할 수 있음
        bool isFacing = isNear && IsFacingScreen();
        UpdatePrompt(isFacing);
        if (!isFacing || _isBusy || !_interactAction.action.WasPressedThisFrame()) return;

        // 새로 틀린 판정이 없으면 리포트를 보내지 않고 횟수도 쓰지 않음
        if (!_hasNewFailure) return;

        // 보낼 수 있는 횟수를 다 쓴 뒤에 누르면 고장
        if (_sendCount >= _maxSends)
        {
            StartCoroutine(BreakRoutine());
        }
        else
        {
            StartCoroutine(SendRoutine());
        }
    }

    // 판정이 틀렸음을 알림 (다음 리포트를 받을 수 있게 됨)
    public void MarkFailure()
    {
        // 방금 틀린 세션이 다음 리포트 (리포트 한 번에 버그 하나)
        _log.EndFailedSession();
        _hasNewFailure = true;
        if (!_isBusy && !_isBroken)
        {
            _terminalScreen.ShowIdle(_hasNewFailure);
        }
    }

    // 판정 기록 한 줄 추가 (리포트에는 최근 몇 줄만 담김)
    public void AddLine(string line)
    {
        _log.Add(line);
        if (!_isBusy && !_isBroken)
        {
            _terminalScreen.ShowIdle(_hasNewFailure);
        }
    }

    // 단말 정면 쪽에 서 있고, 카메라가 화면을 바라보는지
    private bool IsFacingScreen()
    {
        Vector3 screenCenter = _terminalScreen.Center;
        Vector3 toPlayer = _player.position - screenCenter;
        toPlayer.y = 0f;
        if (Vector3.Angle(-transform.forward, toPlayer) > _frontAngle) return false;

        return Vector3.Angle(_cameraPivot.forward, screenCenter - _cameraPivot.position) <= _lookAngle;
    }

    // 쓸 수 있을 때만 [E] 안내 (전송 중 · 고장 뒤에는 숨김)
    private void UpdatePrompt(bool isNear)
    {
        bool isVisible = isNear && !_isBusy && !_isBroken;
        _prompt.SetActive(isVisible);
        if (isVisible)
        {
            // 새로 틀린 판정이 없으면 짧게 '새 리포트 없음' (한 줄에 들어가게, 지금 빌드의 정답 힌트로 읽히지 않게 '버그 없음'은 쓰지 않음)
            _promptText.text = _hasNewFailure ? "버그 리포트 받기" : "<color=#9AA0A6>새 리포트 없음</color>";
        }
    }

    // 전송 중 점이 하나씩 늘다가, 태블릿에 알림이 가고 전송 완료
    private IEnumerator SendRoutine()
    {
        _isBusy = true;
        _prompt.SetActive(false);
        _sendCount++;

        float elapsed = 0f;
        while (elapsed < _sendSeconds)
        {
            _terminalScreen.ShowSending(elapsed / _sendSeconds);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 지난번에 받은 기록은 빼고 이번 회차에 새로 쌓인 기록만 보냄
        _tablet.ReceiveReport(_log.BuildNew(_reportLines));
        _hasNewFailure = false;
        _terminalScreen.ShowSent();
        yield return new WaitForSeconds(_sentSeconds);

        _isBusy = false;
        _terminalScreen.ShowIdle(_hasNewFailure);
    }

    // 글자가 깨지며 화면이 깜빡이다가 터짐
    private IEnumerator BreakRoutine()
    {
        _isBusy = true;
        _prompt.SetActive(false);
        _terminalScreen.BeginGlitch();

        float elapsed = 0f;
        while (elapsed < _breakSeconds)
        {
            _terminalScreen.ShowGlitch(elapsed / _breakSeconds);
            _breakEffect.Tremble();
            elapsed += GlitchInterval;
            yield return new WaitForSeconds(GlitchInterval);
        }

        SetBroken();
    }

    private void SetBroken()
    {
        _isBusy = false;
        _isBroken = true;
        _prompt.SetActive(false);
        _terminalScreen.SetBroken();

        // 단말이 부서지면 태블릿 리포트 페이지도 먹통
        _tablet.SetReportOffline();
        _breakEffect.Break();
    }
}
