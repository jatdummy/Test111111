using NUnit.Framework;

// 단말 판정 기록장 (보관 줄 수 제한, 최근 줄만 담는 리포트, 리포트 한 번에 틀린 세션 하나)
public class QaReportLogTests
{
    private const string Header = "<color=#5FD07A>QA 어시스턴트 리포트</color>";

    [Test]
    public void BuildNewStartsWithHeader()
    {
        var log = new QaReportLog(10);

        StringAssert.StartsWith(Header, log.BuildNew(5));
    }

    [Test]
    public void BuildNewKeepsOnlyRecentLines()
    {
        var log = new QaReportLog(10);
        for (int i = 1; i <= 5; i++)
        {
            log.Add($"line{i}");
        }

        log.EndFailedSession();
        string report = log.BuildNew(3);

        StringAssert.DoesNotContain("line2", report);
        StringAssert.Contains("line3", report);
        StringAssert.EndsWith("line5", report);
    }

    [Test]
    public void AddDropsOldestBeyondMax()
    {
        var log = new QaReportLog(3);
        for (int i = 1; i <= 5; i++)
        {
            log.Add($"line{i}");
        }

        Assert.AreEqual(3, log.Count);
        log.EndFailedSession();
        StringAssert.DoesNotContain("line2", log.BuildNew(10));
    }

    [Test]
    public void BuildNewWithFewerLinesThanRequestedHasAllLines()
    {
        var log = new QaReportLog(10);
        log.Add("only");
        log.EndFailedSession();

        StringAssert.EndsWith("only", log.BuildNew(9));
    }

    // 두 번째 리포트에는 첫 번째 리포트로 이미 받은 기록이 섞이지 않음
    [Test]
    public void BuildNewHasOnlyLinesSinceLastSend()
    {
        var log = new QaReportLog(10);
        log.Add("first");
        log.EndFailedSession();
        log.BuildNew(9);
        log.Add("second");
        log.EndFailedSession();

        string report = log.BuildNew(9);

        StringAssert.DoesNotContain("first", report);
        StringAssert.EndsWith("second", report);
    }

    // 리포트를 받을 때마다 제목의 회차가 올라감
    [Test]
    public void BuildNewNumbersEachReport()
    {
        var log = new QaReportLog(10);
        log.Add("a");
        log.EndFailedSession();
        StringAssert.Contains("#1", log.BuildNew(9));

        log.Add("b");
        log.EndFailedSession();
        StringAssert.Contains("#2", log.BuildNew(9));
    }

    // 리포트를 받지 않고 두 번 틀리면 마지막에 틀린 세션만 담김
    [Test]
    public void BuildNewHasOnlyLastFailedSession()
    {
        var log = new QaReportLog(20);
        log.Add("old pass");
        log.Add("old fail");
        log.EndFailedSession();
        log.Add("new pass");
        log.Add("new fail");
        log.EndFailedSession();

        string report = log.BuildNew(9);

        StringAssert.DoesNotContain("old", report);
        StringAssert.Contains("new pass", report);
        StringAssert.EndsWith("new fail", report);
    }

    // 지금 진행 중인(아직 안 틀린) 세션의 기록은 리포트에 섞이지 않음
    [Test]
    public void BuildNewExcludesCurrentSession()
    {
        var log = new QaReportLog(20);
        log.Add("failed");
        log.EndFailedSession();
        log.Add("playing");

        string report = log.BuildNew(9);

        StringAssert.DoesNotContain("playing", report);
        StringAssert.EndsWith("failed", report);
    }
}
