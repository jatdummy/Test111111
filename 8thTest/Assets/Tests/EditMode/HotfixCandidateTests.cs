using System.Collections.Generic;
using NUnit.Framework;

// 핫픽스 후보 (금지 override 제외, 정상화 후보, 지울 줄 / 넣을 줄)
public class HotfixCandidateTests
{
    private TestAnomalyFactory _factory;

    [SetUp]
    public void SetUp() => _factory = new TestAnomalyFactory();

    [TearDown]
    public void TearDown() => _factory.DestroyAll();

    [Test]
    public void NormalBuildHasAllFourAddLines()
    {
        List<(HotfixKind Kind, OverrideKind Target)> candidates = HotfixScheduler.GetCandidates(new OfficeBuild());

        Assert.AreEqual(4, candidates.Count);
        Assert.IsTrue(candidates.TrueForAll(candidate => candidate.Kind == HotfixKind.AddLine));
    }

    [Test]
    public void BlockedOverrideIsNotACandidate()
    {
        var build = new OfficeBuild { Anomaly = _factory.Create("동료 시선", new[] { OverrideKind.NoColleague }) };

        List<(HotfixKind Kind, OverrideKind Target)> candidates = HotfixScheduler.GetCandidates(build);

        Assert.IsFalse(candidates.Exists(candidate => candidate.Target == OverrideKind.NoColleague));
    }

    [Test]
    public void LightMismatchKeepsLightsUntouched()
    {
        var build = new OfficeBuild
        {
            Anomaly = _factory.Create("조명 불일치", new[] { OverrideKind.RedLight, OverrideKind.BlueLight }, LightOverride.Blue),
            LightOverride = LightOverride.Blue,
        };

        List<(HotfixKind Kind, OverrideKind Target)> candidates = HotfixScheduler.GetCandidates(build);

        Assert.IsFalse(candidates.Exists(candidate => candidate.Target is OverrideKind.RedLight or OverrideKind.BlueLight));
    }

    [Test]
    public void LegitimizableAnomalyOffersLegitimize()
    {
        var build = new OfficeBuild { Anomaly = _factory.Create("책장 문", new[] { OverrideKind.Bookcase }, canBeLegitimized: true, legitimizeOverride: OverrideKind.Bookcase) };

        List<(HotfixKind Kind, OverrideKind Target)> candidates = HotfixScheduler.GetCandidates(build);

        Assert.IsTrue(candidates.Contains((HotfixKind.Legitimize, OverrideKind.Bookcase)));
        Assert.IsFalse(candidates.Contains((HotfixKind.AddLine, OverrideKind.Bookcase)));
    }

    [Test]
    public void ExistingOverrideCanBeRemoved()
    {
        var build = new OfficeBuild { HasNoColleague = true };

        List<(HotfixKind Kind, OverrideKind Target)> candidates = HotfixScheduler.GetCandidates(build);

        Assert.IsTrue(candidates.Contains((HotfixKind.RemoveLine, OverrideKind.NoColleague)));
    }
}
