using UnityEngine;

// 코드에 BookcaseOpen이 없는데 책장 문이 열려 있음
public class CabinetDoorOpenAnomaly : Anomaly
{
    // 열 책장
    [SerializeField] private Cabinet _cabinet;

    public override void Apply(int hour) => _cabinet.Open();

    public override void Revert() => _cabinet.Close();
}
