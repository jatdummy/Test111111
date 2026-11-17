using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Tab을 누르고 있는 동안 코드를 보여 주는 태블릿을 들어 올림
public class CodeTablet : MonoBehaviour
{
    private const string ActiveTabColor = "#E6E6E6";
    private const string InactiveTabColor = "#6E7681";
    private const string UnreadColor = "#F2C14E";
    private const float NoticePulseSpeed = 5f;
    private const string NoReportText = "<color=#6A9955>// 수신된 리포트 없음\n// 검수 로그 단말에서 [E]로 전송</color>";

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

    // 화면 위쪽 페이지 표시 (코드 / 리포트)
    [SerializeField] private TextMesh _tabText;

    // 새 리포트가 오면 화면 오른쪽 아래에 뜨는 알림 (전구 + Tab 안내)
    [SerializeField] private CanvasGroup _notice;

    // 알림 제목 (새 리포트 / 코드 변경)
    [SerializeField] private Text _noticeTitle;

    private InputAction _viewAction;
    private InputAction _flipAction;
    private InputAction _codePageAction;
    private InputAction _reportPageAction;
    private bool _isReportPage;
    private bool _hasUnread;
    private bool _hasNewCode;
    private bool _wasRaised;
    private float _progress;

    // 조금이라도 들려 있으면 태블릿을 보는 중
    public bool IsRaised => _progress > 0f;

    private void Awake()
    {
        _viewAction = InputSystem.actions.FindAction("Player/CodeView");

        // 든 동안 좌클릭으로 페이지 넘김, 1 · 2로 바로 고름
        _flipAction = InputSystem.actions.FindAction("Player/Attack");
        _codePageAction = InputSystem.actions.FindAction("Player/Previous");
        _reportPageAction = InputSystem.actions.FindAction("Player/Next");
    }

    private void Start()
    {
        _reportText.text = NoReportText;
        ShowPage(false);
        ApplyPose();
    }

    private void Update()
    {
        float target = _viewAction.IsPressed() ? 1f : 0f;
        _progress = Mathf.MoveTowards(_progress, target, _raiseSpeed * Time.deltaTime);
        ApplyPose();
        UpdatePage();
    }

    // 검수 로그 단말에서 보낸 기록을 받음 (안 읽었으면 페이지 표시에 점이 붙음)
    public void ReceiveReport(string report)
    {
        _reportText.text = report;
        _hasUnread = true;
        ShowPage(_isReportPage);
    }

    // 루프가 바뀌어 코드가 새로 올라옴 (코드 페이지를 보면 알림이 꺼짐)
    public void NotifyCodeChanged()
    {
        _hasNewCode = true;
        ShowPage(_isReportPage);
    }

    private void UpdatePage()
    {
        // 내려놓았다가 다시 들면 항상 코드 페이지부터
        if (!IsRaised)
        {
            if (_isReportPage) ShowPage(false);
            _wasRaised = false;
            UpdateNotice();
            return;
        }

        // 새 리포트가 와 있으면 드는 순간 리포트 페이지부터 (알림을 보고 Tab을 눌렀을 때)
        // 리포트가 없으면 코드 페이지를 보여 주며 코드 변경 알림을 끔
        if (!_wasRaised) ShowPage(_hasUnread);
        _wasRaised = true;
        UpdateNotice();

        if (_flipAction.WasPressedThisFrame()) ShowPage(!_isReportPage);
        else if (_codePageAction.WasPressedThisFrame()) ShowPage(false);
        else if (_reportPageAction.WasPressedThisFrame()) ShowPage(true);
    }

    // 안 읽은 리포트가 있고 태블릿을 내리고 있으면 화면 오른쪽 아래 전구 알림이 깜빡임
    private void UpdateNotice()
    {
        bool isVisible = (_hasUnread || _hasNewCode) && !IsRaised;
        _notice.gameObject.SetActive(isVisible);
        _noticeTitle.text = _hasUnread && _hasNewCode ? "코드 변경 · 새 리포트" : _hasUnread ? "새 리포트 도착" : "코드 변경됨";
        if (isVisible) _notice.alpha = Mathf.Lerp(0.55f, 1f, (Mathf.Sin(Time.time * NoticePulseSpeed) + 1f) * 0.5f);
    }

    private void ShowPage(bool isReport)
    {
        _isReportPage = isReport;

        // 들고 있는 상태에서 실제로 본 페이지의 알림만 꺼짐
        if (IsRaised && isReport) _hasUnread = false;
        if (IsRaised && !isReport) _hasNewCode = false;

        _codePage.SetActive(!isReport);
        _reportPage.SetActive(isReport);

        string code = Tab("코드", !isReport) + (_hasNewCode ? $"<color={UnreadColor}> •</color>" : string.Empty);
        string report = Tab("리포트", isReport) + (_hasUnread ? $"<color={UnreadColor}> •</color>" : string.Empty);
        _tabText.text = $"{code}    {report}";
    }

    // 지금 보는 페이지는 밝고 굵게, 나머지는 흐리게
    private static string Tab(string label, bool isActive)
    {
        return isActive ? $"<color={ActiveTabColor}><b>{label}</b></color>" : $"<color={InactiveTabColor}>{label}</color>";
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
