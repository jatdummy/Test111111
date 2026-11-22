using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// 다음 빌드 뽑기 규칙 (연속 제한, 같은 이상현상 연속 금지, 금지 override, 필요한 조명, 나오는 시간, 코드로 적은 이상현상)
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
    private Anomaly _late;
    private Anomaly _coded;
    private BuildGenerator _generator;

    [SetUp]
    public void SetUp()
    {
        Random.InitState(Seed);
        _factory = new TestAnomalyFactory();
        _plain = _factory.Create("소화기");
        _stare = _factory.Create("동료 시선");
        _lightMismatch = _factory.Create("조명 불일치", new[] { OverrideKind.RedLight, OverrideKind.BlueLight }, LightOverride.Blue);
        _cabinet = _factory.Create("책장 문", new[] { OverrideKind.Bookcase }, canBeLegitimized: true, legitimizeOverride: OverrideKind.Bookcase);
        _late = _factory.Create("늦은 시간", minHour: 4);
        _coded = _factory.Create("코드로 적는 화분", codeLine: "PlantB.State = PlantState.Withered;");
        _generator = new BuildGenerator(new[] { _plain, _stare, _lightMismatch, _cabinet, _late, _coded });
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

            foreach (OverrideKind kind in new[] { OverrideKind.RedLight, OverrideKind.BlueLight, OverrideKind.Bookcase })
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
            generator.CreateJudged(LoopTime.ClearCount);
        }

        Assert.Pass();
    }

    // 늦은 시간용 이상현상은 그 시간 전에는 나오지 않고, 그 뒤에는 나옴
    [Test]
    public void LateAnomalyWaitsForItsHour()
    {
        for (int i = 0; i < SampleCount; i++)
        {
            OfficeBuild early = _generator.CreateJudged(3);
            Assert.AreNotSame(_late, early.Anomaly);
            Assert.AreNotSame(_late, early.CodedAnomaly);
        }

        bool hasAppeared = false;
        foreach (OfficeBuild build in Generate())
        {
            hasAppeared |= build.Anomaly == _late;
        }

        Assert.IsTrue(hasAppeared, "늦은 시간 이상현상이 한 번도 뽑히지 않음");
    }

    // 코드로 적은 이상현상은 코드 줄이 있는 것만, 이번 버그 이상현상 자신과는 겹치지 않음
    [Test]
    public void CodedAnomalyHasCodeLineAndIsNotTheBug()
    {
        int count = 0;
        foreach (OfficeBuild build in Generate())
        {
            if (build.CodedAnomaly == null) continue;

            Assert.IsTrue(build.CodedAnomaly.Data.HasCodeLine);
            Assert.AreNotSame(build.Anomaly, build.CodedAnomaly);
            if (build.Anomaly == null)
            {
                Assert.IsFalse(build.HasBug, "코드에 적힌 모습만 있는 빌드는 정상이어야 함");
            }

            count++;
        }

        Assert.Greater(count, 0, "코드로 적은 이상현상이 한 번도 나오지 않음");
    }

    private IEnumerable<OfficeBuild> Generate()
    {
        for (int i = 0; i < SampleCount; i++)
        {
            yield return _generator.CreateJudged(LoopTime.ClearCount);
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
            default:
                return build.IsCabinetOpen;
        }
    }
}
