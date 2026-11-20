using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// 다음 빌드 뽑기 규칙 (연속 제한, 같은 이상현상 연속 금지, 금지 override, 필요한 조명)
public class BuildGeneratorTests
{
    // 확률 규칙이라 충분히 많이 뽑아서 확인
    private const int SampleCount = 2000;
    private const int Seed = 8;

    private TestAnomalyFactory _factory;
    private Anomaly _plain;
    private Anomaly _stare;
    private Anomaly _lightMismatch;
    private Anomaly _cabinet;
    private BuildGenerator _generator;

    [SetUp]
    public void SetUp()
    {
        Random.InitState(Seed);
        _factory = new TestAnomalyFactory();
        _plain = _factory.Create("소화기");
        _stare = _factory.Create("동료 시선", new[] { OverrideKind.NoColleague });
        _lightMismatch = _factory.Create("조명 불일치", new[] { OverrideKind.RedLight, OverrideKind.BlueLight }, LightOverride.Blue);
        _cabinet = _factory.Create("책장 문", new[] { OverrideKind.Bookcase }, canBeLegitimized: true, legitimizeOverride: OverrideKind.Bookcase);
        _generator = new BuildGenerator(new[] { _plain, _stare, _lightMismatch, _cabinet });
    }

    [TearDown]
    public void TearDown() => _factory.DestroyAll();

    [Test]
    public void NormalBuildsNeverExceedTwoInARow()
    {
        int streak = 0;
        foreach (OfficeBuild build in Generate())
        {
            streak = build.Anomaly == null ? streak + 1 : 0;
            Assert.LessOrEqual(streak, 2);
        }
    }

    [Test]
    public void BugBuildsNeverExceedFourInARow()
    {
        int streak = 0;
        foreach (OfficeBuild build in Generate())
        {
            streak = build.Anomaly != null ? streak + 1 : 0;
            Assert.LessOrEqual(streak, 4);
        }
    }

    [Test]
    public void SameAnomalyNeverAppearsTwiceInARow()
    {
        Anomaly last = null;
        foreach (OfficeBuild build in Generate())
        {
            if (build.Anomaly == null) continue;

            Assert.AreNotSame(last, build.Anomaly);
            last = build.Anomaly;
        }
    }

    [Test]
    public void BlockedOverrideNeverAppearsWithItsAnomaly()
    {
        foreach (OfficeBuild build in Generate())
        {
            if (build.Anomaly == null) continue;

            foreach (OverrideKind kind in new[] { OverrideKind.RedLight, OverrideKind.BlueLight, OverrideKind.NoColleague, OverrideKind.NoPlants, OverrideKind.Bookcase })
            {
                // 이상현상이 꼭 필요로 하는 조명은 금지 목록에 있어도 코드에 적힘 (금지는 핫픽스로 바꾸지 말라는 뜻)
                if (IsRequiredLight(build.Anomaly, kind)) continue;

                bool isBlockedPair = build.Anomaly.Data.IsBlocked(kind) && HasOverride(build, kind);
                Assert.IsFalse(isBlockedPair, $"{build.Anomaly.Data.ReportLabel} + {kind}");
            }
        }
    }

    [Test]
    public void RequiredLightIsAlwaysWrittenInCode()
    {
        int count = 0;
        foreach (OfficeBuild build in Generate())
        {
            if (build.Anomaly != _lightMismatch) continue;

            Assert.AreEqual(LightOverride.Blue, build.LightOverride);
            count++;
        }

        Assert.Greater(count, 0, "조명 불일치가 한 번도 뽑히지 않음");
    }

    [Test]
    public void SingleAnomalyDoesNotLoopForever()
    {
        var generator = new BuildGenerator(new[] { _plain });
        for (int i = 0; i < 100; i++)
        {
            generator.CreateJudged();
        }

        Assert.Pass();
    }

    private IEnumerable<OfficeBuild> Generate()
    {
        for (int i = 0; i < SampleCount; i++)
        {
            yield return _generator.CreateJudged();
        }
    }

    private static bool IsRequiredLight(Anomaly anomaly, OverrideKind kind)
    {
        LightOverride required = anomaly.Data.RequiredLight;
        return (required == LightOverride.Red && kind == OverrideKind.RedLight) || (required == LightOverride.Blue && kind == OverrideKind.BlueLight);
    }

    private static bool HasOverride(OfficeBuild build, OverrideKind kind)
    {
        switch (kind)
        {
            case OverrideKind.RedLight:
                return build.LightOverride == LightOverride.Red;
            case OverrideKind.BlueLight:
                return build.LightOverride == LightOverride.Blue;
            case OverrideKind.NoColleague:
                return build.HasNoColleague;
            case OverrideKind.NoPlants:
                return build.HasNoPlants;
            default:
                return build.IsCabinetOpen;
        }
    }
}
