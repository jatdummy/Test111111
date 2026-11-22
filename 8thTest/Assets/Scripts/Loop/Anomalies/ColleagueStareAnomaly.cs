using UnityEngine;

// 동료가 걸어오는 내내 플레이어를 쳐다봄
public class ColleagueStareAnomaly : Anomaly
{
    // 걸어오는 동료
    [SerializeField] private ColleagueWalker _colleague;

    public override void Apply(int hour) => _colleague.ResetWalk(true);

    // 동료 걷기는 매 루프 OfficeState가 처음 상태로 되돌리므로 되돌릴 것이 없음
    public override void Revert()
    {
    }
}
