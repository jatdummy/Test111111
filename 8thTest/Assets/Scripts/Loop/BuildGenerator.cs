using System.Collections.Generic;
using UnityEngine;

// 다음 루프의 빌드 뽑기: 정상 / 버그 확률, 연속 제한, 이상현상 고르기 (시간 제한), 미끼 override (코드로 적은 이상현상 모습 포함)
// 씬 오브젝트가 필요 없는 일반 클래스 (EditMode 테스트로 규칙을 검증할 수 있게)
public class BuildGenerator
{
    private const int MaxNormalStreak = 2;
    private const int MaxBugStreak = 4;
    private const float NormalBuildChance = 1f / 3f;
    private const float OverrideChance = 0.5f;
    // 미끼 override 종류 (빨간 조명, 파란 조명, 책장 열림)
    private static readonly OverrideKind[] DecoyKinds = { OverrideKind.RedLight, OverrideKind.BlueLight, OverrideKind.Bookcase };
    // 미끼가 코드로 적은 이상현상 모습일 확률 (나머지는 조명 · 책장, 후보 수에 상관없이 반반이 되게)
    private const float CodedDecoyChance = 0.5f;

    private readonly Anomaly[] _anomalies;
    // 고를 때마다 새로 만들지 않게 재사용하는 후보 목록
    private readonly List<Anomaly> _candidates = new();
    private readonly List<Anomaly> _codedCandidates = new();
    private readonly List<OverrideKind> _allowedKinds = new();
    private Anomaly _lastAnomaly;
    private int _normalStreak;
    private int _bugStreak;

    public BuildGenerator(Anomaly[] anomalies) => _anomalies = anomalies;

    // 이번 이상현상과 함께 나오면 안 되는 override인지 (이상현상 데이터에 적힌 목록)
    public static bool IsBlocked(OfficeBuild build, OverrideKind kind) => build.Anomaly != null && build.Anomaly.Data.IsBlocked(kind);

    // hoursPassed : 22:00부터 지난 시간 (늦은 시간용 이상현상은 그 시간이 되어야 나옴)
    public OfficeBuild CreateJudged(int hoursPassed)
    {
        var build = new OfficeBuild();
        if (ShouldBeNormal())
        {
            _normalStreak++;
            _bugStreak = 0;
            if (Random.value < OverrideChance)
            {
                AddRandomOverride(ref build, hoursPassed);
            }

            return build;
        }

        _bugStreak++;
        _normalStreak = 0;
        build.Anomaly = PickAnomaly(hoursPassed);

        // 코드에 특정 조명이 적혀 있어야 성립하는 이상현상 (조명 명세 불일치 → Blue)
        if (build.Anomaly.Data.RequiredLight != LightOverride.None)
        {
            build.LightOverride = build.Anomaly.Data.RequiredLight;
        }
        else if (Random.value < OverrideChance)
        {
            AddRandomOverride(ref build, hoursPassed);
        }

        return build;
    }

    private bool ShouldBeNormal()
    {
        if (_normalStreak >= MaxNormalStreak) return false;
        if (_bugStreak >= MaxBugStreak) return true;

        return Random.value < NormalBuildChance;
    }

    // 직전과 같은 이상현상은 연속으로 내지 않고, 아직 나올 시간이 안 된 이상현상은 빼고 고름
    private Anomaly PickAnomaly(int hoursPassed)
    {
        FillCandidates(hoursPassed, true);

        // 고를 것이 없으면 (나올 수 있는 이상현상이 직전 것 하나뿐) 직전 것도 허용
        if (_candidates.Count == 0)
        {
            FillCandidates(hoursPassed, false);
        }

        // 그래도 없으면 (모두 늦은 시간용) 시간과 상관없이 전부에서 고름
        if (_candidates.Count == 0)
        {
            _candidates.AddRange(_anomalies);
        }

        Anomaly anomaly = _candidates[Random.Range(0, _candidates.Count)];
        _lastAnomaly = anomaly;
        return anomaly;
    }

    private void FillCandidates(int hoursPassed, bool excludesLast)
    {
        _candidates.Clear();
        foreach (Anomaly anomaly in _anomalies)
        {
            if (anomaly.Data.MinHour > hoursPassed) continue;
            if (excludesLast && anomaly == _lastAnomaly) continue;

            _candidates.Add(anomaly);
        }
    }

    // 미끼 override 하나 추가: 조명 · 책장, 또는 코드로 적을 수 있는 이상현상 모습 (코드에 적혀 있으니 정상)
    // 이상현상 데이터에 함께 나오면 안 된다고 적힌 종류는 다시 뽑음
    // 책장도 같은 소품의 이상현상과 그 소품을 바꾸는 override는 함께 내지 않음
    private void AddRandomOverride(ref OfficeBuild build, int hoursPassed)
    {
        FillCodedCandidates(build, hoursPassed);

        // 코드로 적을 이상현상이 많아도 조명 · 책장 미끼가 줄지 않게 먼저 반반으로 나눔
        if (_codedCandidates.Count > 0 && Random.value < CodedDecoyChance)
        {
            build.CodedAnomaly = _codedCandidates[Random.Range(0, _codedCandidates.Count)];
            return;
        }

        // 이번 이상현상과 함께 나오면 안 되는 종류는 빼고 고름 (다시 뽑기를 반복하지 않아 멈출 일이 없음)
        _allowedKinds.Clear();
        foreach (OverrideKind kind in DecoyKinds)
        {
            if (!IsBlocked(build, kind))
            {
                _allowedKinds.Add(kind);
            }
        }

        // 모두 막혀 있으면 이번 판은 미끼 없음
        if (_allowedKinds.Count == 0) return;

        switch (_allowedKinds[Random.Range(0, _allowedKinds.Count)])
        {
            case OverrideKind.RedLight:
                build.LightOverride = LightOverride.Red;
                return;
            case OverrideKind.BlueLight:
                build.LightOverride = LightOverride.Blue;
                return;
            default:
                build.IsCabinetOpen = true;
                return;
        }
    }

    // 코드에 적어 정상으로 보여 줄 수 있는 이상현상 (이번 버그 이상현상 자신, 아직 나올 시간이 안 된 것은 제외)
    private void FillCodedCandidates(OfficeBuild build, int hoursPassed)
    {
        _codedCandidates.Clear();
        foreach (Anomaly anomaly in _anomalies)
        {
            if (!anomaly.Data.HasCodeLine || anomaly.Data.MinHour > hoursPassed || anomaly == build.Anomaly) continue;

            _codedCandidates.Add(anomaly);
        }
    }
}
