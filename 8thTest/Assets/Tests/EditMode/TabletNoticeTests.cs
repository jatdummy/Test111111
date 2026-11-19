using NUnit.Framework;

// 태블릿 알림 (켜고 끄기, 알림 제목 조합)
public class TabletNoticeTests
{
    [Test]
    public void NewNoticeHasNothing()
    {
        var notice = new TabletNotice();

        Assert.IsFalse(notice.HasAny);
        Assert.AreEqual(0, notice.State);
    }

    [TestCase(TabletPage.Rules, "QA 수칙 확인")]
    [TestCase(TabletPage.Code, "긴급 핫픽스 배포")]
    [TestCase(TabletPage.Report, "새 리포트 도착")]
    public void SingleAlertHasOwnTitle(TabletPage page, string expected)
    {
        var notice = new TabletNotice();
        notice.Add(page);

        Assert.AreEqual(expected, notice.GetTitle());
    }

    [Test]
    public void MultipleAlertsJoinInFixedOrder()
    {
        var notice = new TabletNotice();
        notice.Add(TabletPage.Report);
        notice.Add(TabletPage.Code);
        notice.Add(TabletPage.Rules);

        Assert.AreEqual("QA 수칙 · 긴급 핫픽스 · 새 리포트", notice.GetTitle());
    }

    [Test]
    public void ClearRemovesOnlyThatPage()
    {
        var notice = new TabletNotice();
        notice.Add(TabletPage.Code);
        notice.Add(TabletPage.Report);
        int before = notice.State;

        notice.Clear(TabletPage.Code);

        Assert.IsFalse(notice.Has(TabletPage.Code));
        Assert.IsTrue(notice.Has(TabletPage.Report));
        Assert.AreNotEqual(before, notice.State);
    }
}
