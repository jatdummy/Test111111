using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 근무 중 긴급 핫픽스: 통로에 들어오면 낼지 정하고, 잠시 뒤 코드(빌드)만 바꿔서 알림
// 사무실은 그대로 두고 코드만 바꿈 → 플레이어가 바뀐 코드로 다시 판단해야 함
public class HotfixScheduler : MonoBehaviour
{
    // 판정 대기 중 긴급 핫픽스로 코드가 바뀔 확률 (원본 빌드 제외)
    [SerializeField, Range(0f, 1f)] private float _hotfixChance = 0.3f;

    // 통로에 들어온 뒤 핫픽스가 배포되기까지 걸리는 시간(초, 최소 ~ 최대)
    [SerializeField] private Vector2 _hotfixDelay = new(6f, 14f);

    // 핫픽스가 나오기 시작하는 경과 시간 (22:00 기준, 1이면 23:00부터)
    [SerializeField, Range(0, LoopTime.ClearCount - 1)] private int _hotfixStartHour = 1;

    // 핫픽스가 사무실에도 바로 반영될 확률 (반영되면 정상, 코드만 바뀌면 버그)
    [SerializeField, Range(0f, 1f)] private float _hotfixApplyChance = 0.5f;

    private Coroutine _hotfixRoutine;
    private OfficeBuild _build;

    // 핫픽스가 반영된 새 빌드 (Hotfix, HotfixTarget, IsHotfixApplied가 채워짐)
    public event Action<OfficeBuild> OnHotfixDeployed;

    // 이번 루프에 긴급 핫픽스를 낼지 정하고, 내면 잠시 뒤 코드만 바꿈
    public void Schedule(OfficeBuild build, int hoursPassed)
    {
        if (build.IsOriginal || hoursPassed < _hotfixStartHour || UnityEngine.Random.value >= _hotfixChance) return;

        _build = build;
        _hotfixRoutine = StartCoroutine(DeployHotfixLaterRoutine());
    }

    public void Cancel()
    {
        if (_hotfixRoutine != null)
        {
            StopCoroutine(_hotfixRoutine);
        }

        _hotfixRoutine = null;
    }

    // 이번 빌드에서 낼 수 있는 핫픽스 후보 (테스트에서도 부를 수 있게 static으로 분리)
    public static List<(HotfixKind Kind, OverrideKind Target)> GetCandidates(OfficeBuild build)
    {
        var candidates = new List<(HotfixKind Kind, OverrideKind Target)>();

        // 이상현상을 코드에 반영해 정상으로 만듦 (책장 문이 열린 버그 → BookcaseOpen 추가)
        if (build.Anomaly != null && build.Anomaly.Data.CanBeLegitimized)
        {
            candidates.Add((HotfixKind.Legitimize, build.Anomaly.Data.LegitimizeOverride));
        }

        // 이상현상과 함께 나오면 안 되는 override는 핫픽스로도 바꾸지 않음 (조명 명세 불일치는 Blue 줄이 있어야 성립 등)
        if (build.LightOverride == LightOverride.None)
        {
            AddCandidate(candidates, build, HotfixKind.AddLine, OverrideKind.RedLight);
            AddCandidate(candidates, build, HotfixKind.AddLine, OverrideKind.BlueLight);
        }
        else
        {
            AddCandidate(candidates, build, HotfixKind.RemoveLine, build.LightOverride == LightOverride.Red ? OverrideKind.RedLight : OverrideKind.BlueLight);
        }

        AddCandidate(candidates, build, build.HasNoColleague ? HotfixKind.RemoveLine : HotfixKind.AddLine, OverrideKind.NoColleague);
        AddCandidate(candidates, build, build.IsCabinetOpen ? HotfixKind.RemoveLine : HotfixKind.AddLine, OverrideKind.Bookcase);
        return candidates;
    }

    private IEnumerator DeployHotfixLaterRoutine()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(_hotfixDelay.x, _hotfixDelay.y));
        _hotfixRoutine = null;
        Deploy();
    }

    private void Deploy()
    {
        OfficeBuild build = _build;
        List<(HotfixKind Kind, OverrideKind Target)> candidates = GetCandidates(build);
        (HotfixKind kind, OverrideKind target) = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        build.Hotfix = kind;
        build.HotfixTarget = target;

        // 절반쯤은 사무실도 눈앞에서 같이 바뀜 (코드와 같아져 정상), 나머지는 코드만 바뀜 (버그)
        build.IsHotfixApplied = kind != HotfixKind.Legitimize && UnityEngine.Random.value < _hotfixApplyChance;
        OnHotfixDeployed?.Invoke(build);
    }

    private static void AddCandidate(List<(HotfixKind Kind, OverrideKind Target)> candidates, OfficeBuild build, HotfixKind kind, OverrideKind target)
    {
        if (BuildGenerator.IsBlocked(build, target)) return;

        candidates.Add((kind, target));
    }
}
