using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// 판정 기록을 모아 두었다가 E로 태블릿에 리포트를 보내 주는 QA 어시스턴트(AI) 단말
// 처음 하는 플레이어용 도우미: 한 판(22:00 → 06:00)에 몇 번만 보낼 수 있고, 다 쓴 뒤 누르면 고장 남
public class QaLogTerminal : MonoBehaviour
{
    private const string IdleText = "<color=#5FD07A>QA 어시스턴트</color>\n\n판정 기록을 분석했습니다.\n\n<color=#F2C14E>[E] 태블릿으로 리포트 전송</color>";
    private const string EmptyText = "<color=#5FD07A>QA 어시스턴트</color>\n\n아직 분석할 판정 기록이 없습니다.\n\n<color=#6A9955>// 검수를 진행한 뒤 다시 오세요</color>";
    private const string SendingText = "<color=#5FD07A>QA 어시스턴트</color>\n\n리포트 전송 중";
    private const string SentText = "<color=#5FD07A>QA 어시스턴트</color>\n\n<color=#5FD07A>전송 완료</color>\n\n태블릿을 확인하세요.";
    private const string ReportHeader = "<color=#5FD07A>QA 어시스턴트 리포트</color>";
    private const int MaxStoredLines = 60;
    private const string GlitchChars = "█▓▒░#@%&?!/\\|<>";
    private const float GlitchInterval = 0.06f;
    private const int DotCount = 3;

    // 단말 화면 글자
    [SerializeField] private TextMesh _screenText;

    // 플레이어 (가까이 왔는지 확인용)
    [SerializeField] private Transform _player;

    // 리포트를 받을 플레이어 태블릿
    [SerializeField] private CodeTablet _tablet;

    // 이 거리 안에 들어오면 화면이 켜지고 전송할 수 있음
    [SerializeField] private float _useDistance = 1.8f;

    // 리포트에 담는 최근 기록 줄 수
    [SerializeField] private int _reportLines = 9;

    // 전송 중 문구를 보여 주는 시간(초)
    [SerializeField] private float _sendSeconds = 1.2f;

    // 전송 완료 문구를 보여 주는 시간(초)
    [SerializeField] private float _sentSeconds = 1.5f;

    [Header("고장")]
    // 한 판(22:00 → 06:00)에 태블릿으로 보낼 수 있는 횟수 (다 쓰고 한 번 더 누르면 고장)
    [SerializeField] private int _maxSends = 3;

    // 고장 날 때 글자가 깨지며 깜빡이는 시간(초)
    [SerializeField] private float _breakSeconds = 1.4f;

    // 모니터 화면 판 (고장 나면 꺼진 재질로 바뀜)
    [SerializeField] private Renderer _screen;

    // 꺼진 화면 재질
    [SerializeField] private Material _deadScreenMaterial;

    // 고장 나면 보이는 화면 금
    [SerializeField] private GameObject _cracks;

    // 부서질 때 뒤로 꺾이는 모니터 (몸체 · 화면 · 글자 · 금이 자식, 기준점은 받침대 위)
    [SerializeField] private Transform _monitor;

    // 부서진 뒤 모니터가 꺾여 있는 각도
    [SerializeField] private Vector3 _brokenTilt = new(-16f, 11f, -7f);

    // 꺾일 때 튕기는 시간(초)
    [SerializeField] private float _kickSeconds = 0.5f;

    // 화면에서 튀는 불꽃
    [SerializeField] private ParticleSystem _sparks;

    // 부서진 뒤 피어오르는 연기
    [SerializeField] private ParticleSystem _smoke;

    // 터질 때 번쩍이는 불빛
    [SerializeField] private Light _flash;

    // 터질 때 번쩍이는 세기
    [SerializeField] private float _flashIntensity = 8f;

    // 화면에서 떨어져 나가는 유리 조각들 (평소엔 꺼져 있음)
    [SerializeField] private Rigidbody[] _shards;

    // 유리 조각이 튀어 나가는 세기
    [SerializeField] private float _shardForce = 2.5f;

    // 터질 때 흔들 플레이어 카메라
    [SerializeField] private Transform _cameraPivot;

    // 카메라 흔들림 세기와 시간(초)
    [SerializeField] private float _shakeAmount = 0.07f;
    [SerializeField] private float _shakeSeconds = 0.5f;

    // 부서진 뒤 이따금 불꽃이 튀는 간격(초, 최소 ~ 최대)
    [SerializeField] private Vector2 _sparkInterval = new(1.2f, 3.5f);

    private readonly List<string> _lines = new();
    private InputAction _interactAction;
    private int _sendCount;
    private bool _isBusy;
    private bool _isBroken;
    private Material _liveScreenMaterial;
    private Quaternion _monitorRotation;

    private void Awake() => _interactAction = InputSystem.actions.FindAction("Player/Interact");

    // 씬이 새로 열릴 때마다(새 판, 엔딩 뒤 다시 시작) 멀쩡한 단말에 횟수도 처음부터
    private void Start()
    {
        _liveScreenMaterial = _screen.sharedMaterial;
        _monitorRotation = _monitor.localRotation;
        ShowIdle();
    }

    private void Update()
    {
        if (_isBroken) return;

        bool isNear = Vector3.Distance(_player.position, transform.position) <= _useDistance;
        if (!_isBusy) _screenText.gameObject.SetActive(isNear);
        if (!isNear || _isBusy || !_interactAction.WasPressedThisFrame()) return;

        // 보낼 기록이 없으면 횟수를 쓰지 않음
        if (_lines.Count == 0) return;

        // 보낼 수 있는 횟수를 다 쓴 뒤에 누르면 고장
        if (_sendCount >= _maxSends) StartCoroutine(Break());
        else StartCoroutine(Send());
    }

    // 판정 기록 한 줄 추가 (리포트에는 최근 몇 줄만 담김)
    public void AddLine(string line)
    {
        _lines.Add(line);
        if (_lines.Count > MaxStoredLines) _lines.RemoveAt(0);
        if (!_isBusy && !_isBroken) ShowIdle();
    }

    private void ShowIdle() => _screenText.text = _lines.Count == 0 ? EmptyText : IdleText;

    // 전송 중 점이 하나씩 늘다가, 태블릿에 알림이 가고 전송 완료
    private IEnumerator Send()
    {
        _isBusy = true;
        _sendCount++;

        float elapsed = 0f;
        while (elapsed < _sendSeconds)
        {
            int dots = 1 + (int)(elapsed / _sendSeconds * DotCount * 2) % DotCount;
            _screenText.text = SendingText + new string('.', dots);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _tablet.ReceiveReport(BuildReport());
        _screenText.text = SentText;
        yield return new WaitForSeconds(_sentSeconds);

        _isBusy = false;
        ShowIdle();
    }

    // 태블릿에 보낼 리포트 (보낸 시각의 최근 판정 기록, 틀렸으면 이유가 붙어 있음)
    private string BuildReport()
    {
        var builder = new StringBuilder();
        builder.AppendLine(ReportHeader);
        builder.AppendLine();

        int start = Mathf.Max(0, _lines.Count - _reportLines);
        for (int i = start; i < _lines.Count; i++)
        {
            builder.Append(_lines[i]);
            if (i < _lines.Count - 1) builder.AppendLine();
        }

        return builder.ToString();
    }

    // 글자가 깨지며 화면이 깜빡이다가 터짐
    private IEnumerator Break()
    {
        _isBusy = true;
        _screenText.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < _breakSeconds)
        {
            _screenText.text = CreateGlitchText(elapsed / _breakSeconds);
            _screenText.gameObject.SetActive(Random.value > 0.25f);
            _screen.sharedMaterial = Random.value > 0.3f ? _liveScreenMaterial : _deadScreenMaterial;
            if (Random.value < 0.35f) _sparks.Emit(Random.Range(2, 6));
            _monitor.localRotation = _monitorRotation * Quaternion.Euler(Random.insideUnitSphere * 1.5f);
            elapsed += GlitchInterval;
            yield return new WaitForSeconds(GlitchInterval);
        }

        SetBroken();

        // 터짐: 유리 조각, 불꽃, 섬광, 모니터가 뒤로 꺾이고 화면이 흔들림
        Explode();
        StartCoroutine(Kick());
        StartCoroutine(Flash(_flashIntensity, 0.4f));
        StartCoroutine(Shake());
    }

    private void Explode()
    {
        _sparks.Emit(90);
        Vector3 outward = -_monitor.forward;
        foreach (Rigidbody shard in _shards)
        {
            shard.gameObject.SetActive(true);
            shard.transform.SetParent(null, true);
            shard.isKinematic = false;

            // 화면 앞쪽으로 흩어지며 떨어짐
            Vector3 spread = Random.insideUnitSphere * 0.8f + Vector3.up * 0.4f;
            shard.linearVelocity = (outward + spread) * _shardForce * Random.Range(0.6f, 1.2f);
            shard.angularVelocity = Random.insideUnitSphere * 20f;
        }
    }

    // 뒤로 확 꺾였다가 반동으로 조금 돌아와 멈춤
    private IEnumerator Kick()
    {
        _monitor.localRotation = _monitorRotation;
        float elapsed = 0f;
        while (elapsed < _kickSeconds)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _kickSeconds;
            float amount = t < 0.2f ? Mathf.Lerp(0f, 1.4f, t / 0.2f) : Mathf.Lerp(1.4f, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.2f) / 0.8f));
            _monitor.localRotation = _monitorRotation * Quaternion.Euler(_brokenTilt * amount);
            yield return null;
        }

        _monitor.localRotation = _monitorRotation * Quaternion.Euler(_brokenTilt);
    }

    // 불빛이 지직거리며 꺼짐
    private IEnumerator Flash(float intensity, float seconds)
    {
        _flash.enabled = true;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            _flash.intensity = intensity * (1f - elapsed / seconds) * Random.Range(0.4f, 1f);
            yield return null;
        }

        _flash.enabled = false;
    }

    private IEnumerator Shake()
    {
        Vector3 origin = _cameraPivot.localPosition;
        float elapsed = 0f;
        while (elapsed < _shakeSeconds)
        {
            elapsed += Time.deltaTime;
            float strength = _shakeAmount * (1f - elapsed / _shakeSeconds);
            _cameraPivot.localPosition = origin + Random.insideUnitSphere * strength;
            yield return null;
        }

        _cameraPivot.localPosition = origin;
    }

    // 부서진 뒤에도 가끔 합선된 듯 불꽃이 튐
    private IEnumerator SparkNow()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(_sparkInterval.x, _sparkInterval.y));
            _sparks.Emit(Random.Range(6, 16));
            yield return Flash(_flashIntensity * 0.25f, 0.12f);
        }
    }

    private void SetBroken()
    {
        _isBusy = false;
        _isBroken = true;
        _screenText.gameObject.SetActive(false);
        _screen.sharedMaterial = _deadScreenMaterial;
        _cracks.SetActive(true);

        // 꺾인 모니터, 계속 피어오르는 연기, 이따금 튀는 불꽃
        _monitor.localRotation = _monitorRotation * Quaternion.Euler(_brokenTilt);
        _smoke.Play();
        StartCoroutine(SparkNow());
    }

    // 시간이 갈수록 더 많이 깨지는 글자
    private string CreateGlitchText(float progress)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<color=#FF6B6B>FATAL  QA-ASSIST  0x08</color>");
        for (int line = 0; line < 6; line++)
        {
            int length = Random.Range(8, 30);
            for (int i = 0; i < length; i++)
            {
                bool isGlitch = Random.value < 0.3f + progress * 0.7f;
                builder.Append(isGlitch ? GlitchChars[Random.Range(0, GlitchChars.Length)] : (char)Random.Range('a', 'z' + 1));
            }

            builder.AppendLine();
        }

        builder.Append("<color=#FF6B6B>세션 기록 손상 — 전송 불가</color>");
        return builder.ToString();
    }
}
