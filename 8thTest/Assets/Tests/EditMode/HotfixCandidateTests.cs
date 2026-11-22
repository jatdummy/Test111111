using System.Collections.Generic;
using NUnit.Framework;

// 핫픽스 후보 (조명 줄만, 금지 override 제외, 정상화 후보, 지울 줄 / 넣을 줄)
public class HotfixCandidateTests
{
    private TestAnomalyFactory _factory;

    [SetUp]
    public void SetUp() => _factory = new TestAnomalyFactory();

    [TearDown]
    public void TearDown() => _factory.DestroyAll();

    [Test]
    public void NormalBuildHasBothLightAddLines()
    {
        List<(HotfixKind Kind, OverrideKind Target)> candidates = HotfixScheduler.GetCandidates(new OfficeBuild());

        Assert.AreEqual(2, candidates.Count);
        Assert.IsTrue(candidates.TrueForAll(candidate => candidate.Kind == HotfixKind.AddLine));
        Assert.IsTrue(candidates.TrueForAll(candidate => candidate.Target is OverrideKind.RedLight or OverrideKind.BlueLight));
    }

    // 책장은 자판기에서 멀어 되돌아가야 확인할 수 있으므로 핫픽스 대상이 아님
    [Test]
    public void BookcaseIsNeverAHotfixTarget()
    {
        List<(HotfixKind Kind, OverrideKind Target)> candidates = HotfixScheduler.GetCandidates(new OfficeBuild { IsCabinetOpen = true });

        Assert.IsFalse(candidates.Exists(candidate => candidate.Kind != HotfixKind.Legitimize && candidate.Target == OverrideKind.Bookcase));
    }

    [Test]
    public void BlockedOverrideIsNotACandidate()
    {
        var build = new OfficeBuild { Anomaly = _factory.Create("빨간 조명 금지", new[] { OverrideKind.RedLight }) };

        List<(HotfixKind Kind, OverrideKind Target)> candidates = HotfixScheduler.GetCandidates(build);

        Assert.IsFalse(candidates.Exists(candidate => candidate.Target == OverrideKind.RedLight));
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
        var build = new OfficeBuild { LightOverride = LightOverride.Red };

        List<(HotfixKind Kind, OverrideKind Target)> candidates = HotfixScheduler.GetCandidates(build);

        Assert.IsTrue(candidates.Contains((HotfixKind.RemoveLine, OverrideKind.RedLight)));
    }
}
