using System.Collections.Generic;

// 태블릿에서 아직 안 본 알림 (새 코드 · 새 리포트 · 새 수칙)과 알림 제목
public class TabletNotice
{
    private bool _hasNewCode;
    private bool _hasUnreadReport;
    private bool _hasNewRules;

    public bool HasAny => _hasNewCode || _hasUnreadReport || _hasNewRules;

    // 지금 켜진 알림 조합 번호 (조합이 바뀔 때만 제목을 다시 만들기 위함)
    public int State => (_hasNewRules ? 1 : 0) | (_hasNewCode ? 2 : 0) | (_hasUnreadReport ? 4 : 0);

    public bool Has(TabletPage page)
    {
        return page switch
        {
            TabletPage.Code => _hasNewCode,
            TabletPage.Report => _hasUnreadReport,
            TabletPage.Rules => _hasNewRules,
            _ => false,
        };
    }

    public void Add(TabletPage page) => Set(page, true);

    public void Clear(TabletPage page) => Set(page, false);

    // 한 가지면 그 알림 문구, 여러 가지면 이어서 표시
    public string GetTitle()
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

        if (_hasUnreadReport)
        {
            items.Add("새 리포트");
        }

        if (items.Count > 1) return string.Join(" · ", items);

        if (_hasNewRules) return "QA 수칙 확인";
        return _hasNewCode ? "긴급 핫픽스 배포" : "새 리포트 도착";
    }

    private void Set(TabletPage page, bool isOn)
    {
        switch (page)
        {
            case TabletPage.Code:
                _hasNewCode = isOn;
                break;
            case TabletPage.Report:
                _hasUnreadReport = isOn;
                break;
            case TabletPage.Rules:
                _hasNewRules = isOn;
                break;
        }
    }
}
