using NUnit.Framework;

// 단말 판정 기록장 (보관 줄 수 제한, 최근 줄만 담는 리포트)
public class QaReportLogTests
{
    private const string Header = "<color=#5FD07A>QA 어시스턴트 리포트</color>";

    [Test]
    public void BuildStartsWithHeader()
    {
        var log = new QaReportLog(10);

        StringAssert.StartsWith(Header, log.Build(5));
    }

    [Test]
    public void BuildKeepsOnlyRecentLines()
    {
        var log = new QaReportLog(10);
        for (int i = 1; i <= 5; i++)
        {
            log.Add($"line{i}");
        }

        string report = log.Build(3);

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
        StringAssert.DoesNotContain("line2", log.Build(10));
    }

    [Test]
    public void BuildWithFewerLinesThanRequestedHasAllLines()
    {
        var log = new QaReportLog(10);
        log.Add("only");

        StringAssert.EndsWith("only", log.Build(9));
    }
}
