using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Tab을 누를 때마다 들고 내리는 태블릿 (코드 / 리포트 / 수칙 페이지)
public class CodeTablet : MonoBehaviour
{
    private const float NoticePulseSpeed = 5f;
    private const float GlitchInterval = 0.12f;
    // 알림 전구가 가장 어두울 때의 투명도
    private const float NoticeMinAlpha = 0.55f;

    // 먹통 화면의 깨진 글자 (줄 수, 줄 길이 범위, 글자가 깨질 확률)
    private const int OfflineLineCount = 6;
    private const int OfflineLineMin = 6;
    private const int OfflineLineMax = 30;
    private const float OfflineGlitchChance = 0.6f;

    private const string NoReportText = "<color=#6A9955>// 수신된 리포트 없음\n// QA 어시스턴트 단말에서 [E]로 받기</color>";

    // 들어 올리고 내리는 속도 (초당 진행도, 1이면 1초)
    [SerializeField, Min(0f)] private float _raiseSpeed = 6f;

    // 태블릿 본체 (카메라 자식)
    [SerializeField] private Transform _tablet;

    // 내렸을 때 카메라 기준 위치와 각도 (화면 아래로 숨김)
    [SerializeField] private Vector3 _loweredPosition = new(0f, -0.4f, 0.3f);
    [SerializeField] private Vector3 _loweredEuler = new(60f, 0f, 0f);

    // 들었을 때 카메라 기준 위치와 각도
    [SerializeField] private Vector3 _raisedPosition = new(0f, -0.05f, 0.34f);
    [SerializeField] private Vector3 _raisedEuler = new(8f, 0f, 0f);

    // 페이지 전환 (탭 표시 · 페이지 점 · 페이지 입력)
    [SerializeField] private TabletPages _pages;

    // 리포트 페이지 글자
    [SerializeField] private TextMesh _reportText;

    // QA 수칙 글자
    [SerializeField] private TextMesh _rulesText;

    // 새 리포트가 오면 화면 오른쪽 아래에 뜨는 알림 (전구 + Tab 안내)
    [SerializeField] private CanvasGroup _notice;

    // 알림 제목 (새 리포트 / 긴급 핫픽스 / QA 수칙)
    [SerializeField] private Text _noticeTitle;

    [Header("입력")]
    // 들기 / 내리기 (Player/CodeView)
    [SerializeField] private InputActionReference _viewAction;

    // 아직 안 본 알림 (새 코드 · 새 리포트 · 새 수칙)
    private readonly TabletNotice _unseen = new();
    private bool _isReportOffline;
    private bool _wasRaised;
    private float _glitchTimer;

    // 지금 알림 제목에 반영된 상태 (바뀔 때만 제목을 다시 만듦, -1이면 아직 없음)
    private int _noticeState = -1;

    // Tab을 누를 때마다 들기 / 내리기가 바뀜
    private bool _isOpen;
    private float _progress;

    // 조금이라도 들려 있으면 태블릿을 보는 중
    public bool IsRaised => _progress > 0f;

    private void Start()
    {
        _reportText.text = NoReportText;
        ShowPage(TabletPage.Code);
        ApplyPose();
    }

    private void Update()
    {
        // 누르고 있는 동안이 아니라, 누를 때마다 들고 내림
        if (_viewAction.action.WasPressedThisFrame())
        {
            _isOpen = !_isOpen;
        }

        float target = _isOpen ? 1f : 0f;
        _progress = Mathf.MoveTowards(_progress, target, _raiseSpeed * Time.deltaTime);
        ApplyPose();
        UpdatePage();
        UpdateOfflineGlitch();
    }

    // 연출 중에는 태블릿을 내림 (엔딩 시작, 밤으로 돌아갈 때)
    public void Close() => _isOpen = false;

    // 검수 로그 단말에서 보낸 기록을 받음 (안 읽었으면 페이지 표시에 점이 붙음)
    public void ReceiveReport(string report)
    {
        if (_isReportOffline) return;

        _reportText.text = report;
        _unseen.Add(TabletPage.Report);
        ShowPage(_pages.Current);
    }

    // 긴급 핫픽스로 코드가 바뀜 (코드 페이지를 보면 알림이 꺼짐)
    public void NotifyCodeChanged()
    {
        _unseen.Add(TabletPage.Code);
        ShowPage(_pages.Current);
    }

    // 출근하면 QA 수칙이 태블릿으로 옴 (수칙 페이지를 보면 알림이 꺼짐)
    public void SetRules(string rules)
    {
        _rulesText.text = rules;
        _unseen.Add(TabletPage.Rules);
        ShowPage(_pages.Current);
    }

    // 단말이 부서지면 리포트 페이지도 먹통 (글자가 계속 깨짐)
    public void SetReportOffline()
    {
        _isReportOffline = true;
        _unseen.Clear(TabletPage.Report);
        _reportText.text = CreateOfflineText();
        ShowPage(_pages.Current);
    }

    private void UpdatePage()
    {
        // 내려놓았다가 다시 들면 항상 코드 페이지부터
        if (!IsRaised)
        {
            if (_pages.Current != TabletPage.Code)
            {
                ShowPage(TabletPage.Code);
            }

            _wasRaised = false;
            UpdateNotice();
            return;
        }

        // 새 리포트가 와 있으면 드는 순간 리포트 페이지부터 (알림을 보고 Tab을 눌렀을 때)
        // 리포트가 없으면 코드 페이지를 보여 주며 코드 변경 알림을 끔
        // 아직 안 본 수칙이 있으면 수칙이 가장 먼저
        if (!_wasRaised)
        {
            ShowPage(_unseen.Has(TabletPage.Rules) ? TabletPage.Rules : _unseen.Has(TabletPage.Report) ? TabletPage.Report : TabletPage.Code);
        }

        _wasRaised = true;
        UpdateNotice();

        if (_pages.TryReadInput(out TabletPage page))
        {
            ShowPage(page);
        }
    }

    // 안 읽은 리포트가 있고 태블릿을 내리고 있으면 화면 오른쪽 아래 전구 알림이 깜빡임
    private void UpdateNotice()
    {
        bool isVisible = _unseen.HasAny && !IsRaised;
        _notice.gameObject.SetActive(isVisible);
        if (!isVisible) return;

        // 알림 종류가 바뀐 경우에만 제목 문자열을 새로 만듦 (매 프레임 List와 문자열을 만들지 않게)
        int state = _unseen.State;
        if (state != _noticeState)
        {
            _noticeState = state;
            _noticeTitle.text = _unseen.GetTitle();
        }

        _notice.alpha = Mathf.Lerp(NoticeMinAlpha, 1f, (Mathf.Sin(Time.time * NoticePulseSpeed) + 1f) * 0.5f);
    }

    private void ShowPage(TabletPage page)
    {
        // 들고 있는 상태에서 실제로 본 페이지의 알림만 꺼짐
        if (IsRaised)
        {
            _unseen.Clear(page);
        }

        _pages.Show(page, _unseen, _isReportOffline);
    }

    // 먹통이 된 리포트 페이지는 볼 때마다 글자가 지직거림
    private void UpdateOfflineGlitch()
    {
        if (!_isReportOffline || _pages.Current != TabletPage.Report || !IsRaised) return;

        _glitchTimer -= Time.deltaTime;
        if (_glitchTimer > 0f) return;

        _glitchTimer = GlitchInterval;
        _reportText.text = CreateOfflineText();
    }

    private static string CreateOfflineText()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"<color={TabletPages.OfflineColor}>QA 어시스턴트 응답 없음</color>");
        builder.AppendLine($"<color={TabletPages.OfflineColor}>연결 끊김 — 리포트를 불러올 수 없음</color>");
        builder.AppendLine();
        GlitchText.AppendLines(builder, OfflineLineCount, OfflineLineMin, OfflineLineMax, OfflineGlitchChance, false);
        return builder.ToString();
    }

    // 진행도에 맞춰 내린 자세와 든 자세 사이를 부드럽게 이동
    private void ApplyPose()
    {
        _tablet.gameObject.SetActive(IsRaised);

        float t = Mathf.SmoothStep(0f, 1f, _progress);
        _tablet.localPosition = Vector3.Lerp(_loweredPosition, _raisedPosition, t);
        _tablet.localRotation = Quaternion.Slerp(Quaternion.Euler(_loweredEuler), Quaternion.Euler(_raisedEuler), t);
    }
}
