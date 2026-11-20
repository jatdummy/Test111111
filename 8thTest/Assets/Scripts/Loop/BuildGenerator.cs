using UnityEngine;

// 다음 루프의 빌드 뽑기: 정상 / 버그 확률, 연속 제한, 이상현상 고르기, 미끼 override
// 씬 오브젝트가 필요 없는 일반 클래스 (EditMode 테스트로 규칙을 검증할 수 있게)
public class BuildGenerator
{
    private const int MaxNormalStreak = 2;
    private const int MaxBugStreak = 4;
    private const float NormalBuildChance = 1f / 3f;
    private const float OverrideChance = 0.5f;
    // 미끼 override 종류 수 (빨간 조명, 파란 조명, 동료 없음, 책장 열림)
    private const int OverrideKindCount = 4;

    private readonly Anomaly[] _anomalies;
    private Anomaly _lastAnomaly;
    private int _normalStreak;
    private int _bugStreak;

    public BuildGenerator(Anomaly[] anomalies) => _anomalies = anomalies;

    // 이번 이상현상과 함께 나오면 안 되는 override인지 (이상현상 데이터에 적힌 목록)
    public static bool IsBlocked(OfficeBuild build, OverrideKind kind) => build.Anomaly != null && build.Anomaly.Data.IsBlocked(kind);

    public OfficeBuild CreateJudged()
    {
        var build = new OfficeBuild();
        if (ShouldBeNormal())
        {
            _normalStreak++;
            _bugStreak = 0;
            if (Random.value < OverrideChance)
            {
                AddRandomOverride(ref build);
            }

            return build;
        }

        _bugStreak++;
        _normalStreak = 0;
        build.Anomaly = PickAnomaly();

        // 코드에 특정 조명이 적혀 있어야 성립하는 이상현상 (조명 명세 불일치 → Blue)
        if (build.Anomaly.Data.RequiredLight != LightOverride.None)
        {
            build.LightOverride = build.Anomaly.Data.RequiredLight;
        }
        else if (Random.value < OverrideChance)
        {
            AddRandomOverride(ref build);
        }

        return build;
    }

    private bool ShouldBeNormal()
    {
        if (_normalStreak >= MaxNormalStreak) return false;
        if (_bugStreak >= MaxBugStreak) return true;

        return Random.value < NormalBuildChance;
    }

    // 직전과 같은 이상현상은 연속으로 내지 않음
    private Anomaly PickAnomaly()
    {
        Anomaly anomaly;
        do
        {
            anomaly = _anomalies[Random.Range(0, _anomalies.Length)];
        }
        while (anomaly == _lastAnomaly && _anomalies.Length > 1);

        _lastAnomaly = anomaly;
        return anomaly;
    }

    // 미끼 override 하나 추가 (동료 이상현상과 동료 없음은 함께 내지 않음)
    // 책장도 같은 소품의 이상현상과 그 소품을 바꾸는 override는 함께 내지 않음
    private static void AddRandomOverride(ref OfficeBuild build)
    {
        // 이번 이상현상과 겹치는 종류가 뽑히면 다시 뽑음
        while (true)
        {
            switch ((OverrideKind)Random.Range(0, OverrideKindCount))
            {
                case OverrideKind.RedLight:
                    if (IsBlocked(build, OverrideKind.RedLight)) continue;
                    build.LightOverride = LightOverride.Red;
                    return;
                case OverrideKind.BlueLight:
                    if (IsBlocked(build, OverrideKind.BlueLight)) continue;
                    build.LightOverride = LightOverride.Blue;
                    return;
                case OverrideKind.NoColleague:
                    if (IsBlocked(build, OverrideKind.NoColleague)) continue;
                    build.HasNoColleague = true;
                    return;
                default:
                    if (IsBlocked(build, OverrideKind.Bookcase)) continue;
                    build.IsCabinetOpen = true;
                    return;
            }
        }
    }
}
