using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// 태블릿 페이지 전환 (페이지 켜고 끄기 · 위쪽 탭 표시 · 아래 페이지 점 · 바로 가기와 휠 입력)
public class TabletPages : MonoBehaviour
{
    public const string OfflineColor = "#FF6B6B";
    private const string ActiveTabColor = "#E6E6E6";
    private const string InactiveTabColor = "#6E7681";
    private const string TabNumberColor = "#4B525C";
    private const string UnreadColor = "#F2C14E";
    private const float ScrollCooldown = 0.18f;
    private const int PageCount = 3;

    // 코드 페이지 (현재 빌드 코드 글자가 자식)
    [SerializeField] private GameObject _codePage;

    // 리포트 페이지 (검수 로그 단말에서 받은 기록)
    [SerializeField] private GameObject _reportPage;

    // QA 수칙 페이지
    [SerializeField] private GameObject _rulesPage;

    // 화면 위쪽 페이지 표시 (코드 / 리포트 / 수칙)
    [SerializeField] private TextMesh _tabText;

    // 화면 아래 페이지 점 (● ○ ○, 넘길 수 있는 페이지가 있다는 걸 보여 줌)
    [SerializeField] private TextMesh _pageDots;

    [Header("입력")]
    // 페이지 바로 가기 (Player/TabletCode, TabletReport, TabletRules)
    [SerializeField] private InputActionReference _codePageAction;
    [SerializeField] private InputActionReference _reportPageAction;
    [SerializeField] private InputActionReference _rulesPageAction;

    // 페이지 넘기기 휠 (UI/ScrollWheel)
    [SerializeField] private InputActionReference _scrollAction;

    private float _scrollTimer;

    public TabletPage Current { get; private set; }

    // 든 동안 마우스 휠로 이전 · 다음 페이지, 숫자 1 · 2 · 3으로 바로 가기 (고른 페이지가 있으면 true)
    public bool TryReadInput(out TabletPage page)
    {
        page = Current;
        if (_codePageAction.action.WasPressedThisFrame())
        {
            page = TabletPage.Code;
            return true;
        }

        if (_reportPageAction.action.WasPressedThisFrame())
        {
            page = TabletPage.Report;
            return true;
        }

        if (_rulesPageAction.action.WasPressedThisFrame())
        {
            page = TabletPage.Rules;
            return true;
        }

        // 휠은 한 칸씩만 (터치패드처럼 값이 연달아 들어와도 휙휙 넘어가지 않게), 끝에서 멈춤
        _scrollTimer -= Time.deltaTime;
        float scroll = _scrollAction.action.ReadValue<Vector2>().y;
        if (scroll == 0f || _scrollTimer > 0f) return false;

        _scrollTimer = ScrollCooldown;
        page = (TabletPage)Mathf.Clamp((int)Current + (scroll < 0f ? 1 : -1), 0, PageCount - 1);
        return page != Current;
    }

    // 페이지를 켜고, 탭 표시(알림 점 · 먹통 표시)와 페이지 점을 다시 그림
    public void Show(TabletPage page, TabletNotice notice, bool isReportOffline)
    {
        Current = page;
        _codePage.SetActive(page == TabletPage.Code);
        _reportPage.SetActive(page == TabletPage.Report);
        _rulesPage.SetActive(page == TabletPage.Rules);

        string code = Tab(1, "코드", page == TabletPage.Code) + Dot(notice.Has(TabletPage.Code));
        string report = Tab(2, "리포트", page == TabletPage.Report) + (isReportOffline ? $"<color={OfflineColor}> ✕</color>" : Dot(notice.Has(TabletPage.Report)));
        string rules = Tab(3, "수칙", page == TabletPage.Rules) + Dot(notice.Has(TabletPage.Rules));
        _tabText.text = $"{code}    {report}    {rules}";

        // 지금 페이지만 채운 점
        var dots = new StringBuilder();
        for (int i = 0; i < PageCount; i++)
        {
            dots.Append(i == (int)page ? $"<color={ActiveTabColor}>●</color>" : $"<color={InactiveTabColor}>○</color>");
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
}
