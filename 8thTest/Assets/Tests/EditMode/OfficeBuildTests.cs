using NUnit.Framework;

// 빌드의 버그 판정표와 빌드 번호
public class OfficeBuildTests
{
    private TestAnomalyFactory _factory;
    private Anomaly _anomaly;

    [SetUp]
    public void SetUp()
    {
        _factory = new TestAnomalyFactory();
        _anomaly = _factory.Create("소화기");
    }

    [TearDown]
    public void TearDown() => _factory.DestroyAll();

    // 핫픽스 종류, 사무실 반영 여부, 이상현상 유무 → 버그인지
    [TestCase(HotfixKind.None, false, false, false)]
    [TestCase(HotfixKind.None, false, true, true)]
    [TestCase(HotfixKind.AddLine, false, false, true)]
    [TestCase(HotfixKind.AddLine, true, false, false)]
    [TestCase(HotfixKind.AddLine, true, true, true)]
    [TestCase(HotfixKind.RemoveLine, false, false, true)]
    [TestCase(HotfixKind.RemoveLine, true, false, false)]
    [TestCase(HotfixKind.Legitimize, false, true, false)]
    public void HasBugFollowsHotfixRules(HotfixKind hotfix, bool isApplied, bool hasAnomaly, bool expected)
    {
        var build = new OfficeBuild
        {
            Hotfix = hotfix,
            IsHotfixApplied = isApplied,
            Anomaly = hasAnomaly ? _anomaly : null,
        };

        Assert.AreEqual(expected, build.HasBug);
    }

    [Test]
    public void OriginalBuildNumberIsZero() => Assert.AreEqual(0, OfficeBuild.CreateOriginal().GetNumber(3));

    [Test]
    public void JudgedBuildNumberIsHourPlusOne() => Assert.AreEqual(4, new OfficeBuild().GetNumber(3));
}
