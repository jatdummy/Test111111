using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Tab을 누를 때마다 들고 내리는 태블릿 (코드 / 리포트 / 수칙 페이지)
public class CodeTablet : MonoBehaviour
{
    private const string ActiveTabColor = "#E6E6E6";
    private const string InactiveTabColor = "#6E7681";
    private const string TabNumberColor = "#4B525C";
    private const float ScrollCooldown = 0.18f;
    private const string UnreadColor = "#F2C14E";
    private const string OfflineColor = "#FF6B6B";
    private const float NoticePulseSpeed = 5f;
    private const float GlitchInterval = 0.12f;
    private const string GlitchChars = "█▓▒░#@%&?!/\\|<>";
    // 알림 전구가 가장 어두울 때의 투명도
    private const float NoticeMinAlpha = 0.55f;

    // 먹통 화면의 깨진 글자 (줄 수, 줄 길이 범위, 글자가 깨질 확률)
    private const int OfflineLineCount = 6;
    private const int OfflineLineMin = 6;
    private const int OfflineLineMax = 30;
    private const float OfflineGlitchChance = 0.6f;

    private const string NoReportText = "<color=#6A9955>// 수신된 리포트 없음\n// QA 어시스턴트 단말에서 [E]로 받기</color>";

    // 페이지 번호 (코드 → 리포트 → 수칙 순서)
    private const int CodePage = 0;
    private const int ReportPage = 1;
    private const int RulesPage = 2;
    private const int PageCount = 3;

    // 들어 올리고 내리는 속도 (초당 진행도, 1이면 1초)
    [SerializeField] private float _raiseSpeed = 6f;

    // 태블릿 본체 (카메라 자식)
    [SerializeField] private Transform _tablet;

    // 내렸을 때 카메라 기준 위치와 각도 (화면 아래로 숨김)
    [SerializeField] private Vector3 _loweredPosition = new(0f, -0.4f, 0.3f);
    [SerializeField] private Vector3 _loweredEuler = new(60f, 0f, 0f);

    // 들었을 때 카메라 기준 위치와 각도
    [SerializeField] private Vector3 _raisedPosition = new(0f, -0.05f, 0.34f);
    [SerializeField] private Vector3 _raisedEuler = new(8f, 0f, 0f);

    // 코드 페이지 (현재 빌드 코드 글자가 자식)
    [SerializeField] private GameObject _codePage;

    // 리포트 페이지 (검수 로그 단말에서 받은 기록)
    [SerializeField] private GameObject _reportPage;

    // 리포트 페이지 글자
    [SerializeField] private TextMesh _reportText;

    // QA 수칙 페이지
    [SerializeField] private GameObject _rulesPage;

    // QA 수칙 글자
    [SerializeField] private TextMesh _rulesText;

    // 화면 위쪽 페이지 표시 (코드 / 리포트 / 수칙)
    [SerializeField] private TextMesh _tabText;

    // 화면 아래 페이지 점 (● ○ ○, 넘길 수 있는 페이지가 있다는 걸 보여 줌)
    [SerializeField] private TextMesh _pageDots;

    // 새 리포트가 오면 화면 오른쪽 아래에 뜨는 알림 (전구 + Tab 안내)
    [SerializeField] private CanvasGroup _notice;

    // 알림 제목 (새 리포트 / 긴급 핫픽스 / QA 수칙)
    [SerializeField] private Text _noticeTitle;

    [Header("입력")]
    // 들기 / 내리기 (Player/CodeView)
    [SerializeField] private InputActionReference _viewAction;

    // 페이지 바로 가기 (Player/TabletCode, TabletReport, TabletRules)
    [SerializeField] private InputActionReference _codePageAction;
    [SerializeField] private InputActionReference _reportPageAction;
    [SerializeField] private InputActionReference _rulesPageAction;

    // 페이지 넘기기 휠 (UI/ScrollWheel)
    [SerializeField] private InputActionReference _scrollAction;

    private int _page;
    private bool _hasUnread;
    private bool _hasNewCode;
    private bool _hasNewRules;
    private bool _isReportOffline;
    private bool _wasRaised;
    private float _glitchTimer;
    private float _scrollTimer;

    // Tab을 누를 때마다 들기 / 내리기가 바뀜
    private bool _isOpen;
    private float _progress;

    // 조금이라도 들려 있으면 태블릿을 보는 중
    public bool IsRaised => _progress > 0f;

    private void Start()
    {
        _reportText.text = NoReportText;
        ShowPage(CodePage);
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
        _hasUnread = true;
        ShowPage(_page);
    }

    // 긴급 핫픽스로 코드가 바뀜 (코드 페이지를 보면 알림이 꺼짐)
    public void NotifyCodeChanged()
    {
        _hasNewCode = true;
        ShowPage(_page);
    }

    // 출근하면 QA 수칙이 태블릿으로 옴 (수칙 페이지를 보면 알림이 꺼짐)
    public void SetRules(string rules)
    {
        _rulesText.text = rules;
        _hasNewRules = true;
        ShowPage(_page);
    }

    // 단말이 부서지면 리포트 페이지도 먹통 (글자가 계속 깨짐)
    public void SetReportOffline()
    {
        _isReportOffline = true;
        _hasUnread = false;
        _reportText.text = CreateOfflineText();
        ShowPage(_page);
    }

    private void UpdatePage()
    {
        // 내려놓았다가 다시 들면 항상 코드 페이지부터
        if (!IsRaised)
        {
            if (_page != CodePage)
            {
                ShowPage(CodePage);
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
            ShowPage(_hasNewRules ? RulesPage : _hasUnread ? ReportPage : CodePage);
        }

        _wasRaised = true;
        UpdateNotice();

        UpdatePageInput();
    }

    // 든 동안 마우스 휠로 이전 · 다음 페이지, 숫자 1 · 2 · 3으로 바로 가기
    private void UpdatePageInput()
    {
        if (_codePageAction.action.WasPressedThisFrame())
        {
            ShowPage(CodePage);
        }
        else if (_reportPageAction.action.WasPressedThisFrame())
        {
            ShowPage(ReportPage);
        }
        else if (_rulesPageAction.action.WasPressedThisFrame())
        {
            ShowPage(RulesPage);
        }

        // 휠은 한 칸씩만 (터치패드처럼 값이 연달아 들어와도 휙휙 넘어가지 않게), 끝에서 멈춤
        _scrollTimer -= Time.deltaTime;
        float scroll = _scrollAction.action.ReadValue<Vector2>().y;
        if (scroll == 0f || _scrollTimer > 0f) return;

        int next = Mathf.Clamp(_page + (scroll < 0f ? 1 : -1), 0, PageCount - 1);
        if (next != _page)
        {
            ShowPage(next);
        }

        _scrollTimer = ScrollCooldown;
    }

    // 안 읽은 리포트가 있고 태블릿을 내리고 있으면 화면 오른쪽 아래 전구 알림이 깜빡임
    private void UpdateNotice()
    {
        bool isVisible = (_hasUnread || _hasNewCode || _hasNewRules) && !IsRaised;
        _notice.gameObject.SetActive(isVisible);
        if (!isVisible) return;

        _noticeTitle.text = GetNoticeTitle();
        _notice.alpha = Mathf.Lerp(NoticeMinAlpha, 1f, (Mathf.Sin(Time.time * NoticePulseSpeed) + 1f) * 0.5f);
    }

    // 한 가지면 그 알림 문구, 여러 가지면 이어서 표시
    private string GetNoticeTitle()
    {
        var items = new List<string>();
        if (_hasNewRules)
        {
            items.Add("QA 수칙");
        }

        if (_hasNewCode)
        {
            items.Add("긴급 핫픽스");
        }

        if (_hasUnread)
        {
            items.Add("새 리포트");
        }

        if (items.Count > 1) return string.Join(" · ", items);

        if (_hasNewRules) return "QA 수칙 확인";
        return _hasNewCode ? "긴급 핫픽스 배포" : "새 리포트 도착";
    }

    private void ShowPage(int page)
    {
        _page = page;

        // 들고 있는 상태에서 실제로 본 페이지의 알림만 꺼짐
        if (IsRaised && page == ReportPage)
        {
            _hasUnread = false;
        }

        if (IsRaised && page == CodePage)
        {
            _hasNewCode = false;
        }

        if (IsRaised && page == RulesPage)
        {
            _hasNewRules = false;
        }

        _codePage.SetActive(page == CodePage);
        _reportPage.SetActive(page == ReportPage);
        _rulesPage.SetActive(page == RulesPage);

        string code = Tab(1, "코드", page == CodePage) + Dot(_hasNewCode);
        string report = Tab(2, "리포트", page == ReportPage) + (_isReportOffline ? $"<color={OfflineColor}> ✕</color>" : Dot(_hasUnread));
        string rules = Tab(3, "수칙", page == RulesPage) + Dot(_hasNewRules);
        _tabText.text = $"{code}    {report}    {rules}";

        // 지금 페이지만 채운 점
        var dots = new System.Text.StringBuilder();
        for (int i = 0; i < PageCount; i++)
        {
            dots.Append(i == page ? $"<color={ActiveTabColor}>●</color>" : $"<color={InactiveTabColor}>○</color>");
            if (i < PageCount - 1)
            {
                dots.Append("  ");
            }
        }

        _pageDots.text = dots.ToString();
    }

    private static string Dot(bool isOn) => isOn ? $"<color={UnreadColor}> •</color>" : string.Empty;

    // 지금 보는 페이지는 밝고 굵게, 나머지는 흐리게 (앞의 작은 숫자가 바로 가기 키)
    private static string Tab(int number, string label, bool isActive)
    {
        string name = isActive ? $"<color={ActiveTabColor}><b>{label}</b></color>" : $"<color={InactiveTabColor}>{label}</color>";
        return $"<color={TabNumberColor}>{number}</color> {name}";
    }

    // 먹통이 된 리포트 페이지는 볼 때마다 글자가 지직거림
    private void UpdateOfflineGlitch()
    {
        if (!_isReportOffline || _page != ReportPage || !IsRaised) return;

        _glitchTimer -= Time.deltaTime;
        if (_glitchTimer > 0f) return;

        _glitchTimer = GlitchInterval;
        _reportText.text = CreateOfflineText();
    }

    private static string CreateOfflineText()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"<color={OfflineColor}>QA 어시스턴트 응답 없음</color>");
        builder.AppendLine($"<color={OfflineColor}>연결 끊김 — 리포트를 불러올 수 없음</color>");
        builder.AppendLine();
        for (int line = 0; line < OfflineLineCount; line++)
        {
            int length = Random.Range(OfflineLineMin, OfflineLineMax);
            for (int i = 0; i < length; i++)
            {
                builder.Append(Random.value < OfflineGlitchChance ? GlitchChars[Random.Range(0, GlitchChars.Length)] : ' ');
            }

            builder.AppendLine();
        }

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
