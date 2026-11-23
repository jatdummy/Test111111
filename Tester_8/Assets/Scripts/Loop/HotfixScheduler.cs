using System;
using System.Collections.Generic;
using UnityEngine;

// 근무 중 긴급 핫픽스: 시작선을 넘으면 이번 판에 낼지 정해 두고, 자판기를 지나는 순간 코드(빌드)만 바꿔서 알림
// 사무실은 그대로 두고 코드만 바꿈 → 플레이어가 바뀐 코드로 다시 판단해야 함
public class HotfixScheduler : MonoBehaviour
{
    // 판정 대기 중 긴급 핫픽스로 코드가 바뀔 확률 (원본 빌드 제외)
    [SerializeField, Range(0f, 1f)] private float _hotfixChance = 0.3f;

    // 핫픽스가 나오기 시작하는 경과 시간 (22:00 기준, 1이면 23:00부터)
    [SerializeField, Range(0, LoopTime.ClearCount - 1)] private int _hotfixStartHour = 1;

    // 핫픽스가 사무실에도 바로 반영될 확률 (반영되면 정상, 코드만 바뀌면 버그)
    [SerializeField, Range(0f, 1f)] private float _hotfixApplyChance = 0.5f;

    // 이번 판에 핫픽스가 예약돼 있는지 (자판기를 지나면 배포하고 지움)
    private bool _isPending;
    private OfficeBuild _build;

    // 핫픽스가 반영된 새 빌드 (Hotfix, HotfixTarget, IsHotfixApplied가 채워짐)
    public event Action<OfficeBuild> OnHotfixDeployed;

    // 이번 루프에 긴급 핫픽스를 낼지 정해 둠 (실제 배포는 자판기를 지날 때)
    public void Schedule(OfficeBuild build, int hoursPassed)
    {
        _isPending = false;
        if (build.IsOriginal || hoursPassed < _hotfixStartHour || UnityEngine.Random.value >= _hotfixChance) return;

        _build = build;
        _isPending = true;
    }

    // 자판기를 지나면 예약된 핫픽스를 배포 (한 판에 한 번)
    public void DeployIfPending()
    {
        if (!_isPending) return;

        _isPending = false;
        Deploy();
    }

    // 판정이 끝나면 예약을 지움 (자판기까지 가지 않고 판정한 판은 핫픽스 없음)
    public void Cancel() => _isPending = false;

    // 이번 빌드에서 낼 수 있는 핫픽스 후보 (테스트에서도 부를 수 있게 static으로 분리)
    public static List<(HotfixKind Kind, OverrideKind Target)> GetCandidates(OfficeBuild build)
    {
        var candidates = new List<(HotfixKind Kind, OverrideKind Target)>();

        // 이상현상을 코드에 반영해 정상으로 만듦 (데이터에서 정상화 가능으로 표시한 이상현상만)
        if (build.Anomaly != null && build.Anomaly.Data.CanBeLegitimized)
        {
            candidates.Add((HotfixKind.Legitimize, build.Anomaly.Data.LegitimizeOverride));
        }

        // 핫픽스는 조명 줄만 바꿈 (사무실 전체에 보여서, 자판기를 지난 뒤에도 되돌아가지 않고 확인할 수 있게)
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

        return candidates;
    }

    private void Deploy()
    {
        OfficeBuild build = _build;
        List<(HotfixKind Kind, OverrideKind Target)> candidates = GetCandidates(build);

        // 바꿀 수 있는 줄이 없으면 (조명 명세 불일치처럼 조명을 건드리면 안 되는 판) 이번 판은 핫픽스 없음
        if (candidates.Count == 0) return;

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
