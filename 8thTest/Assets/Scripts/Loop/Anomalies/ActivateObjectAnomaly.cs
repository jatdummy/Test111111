using UnityEngine;

// 평소엔 꺼져 있는 오브젝트가 켜짐 (정수기 핏물, 넘친 쓰레기 등 코드 없이 추가하는 이상현상)
public class ActivateObjectAnomaly : Anomaly
{
    // 이상현상일 때만 보이는 오브젝트
    [SerializeField] private GameObject _target;

    public override void Apply(int hour) => _target.SetActive(true);

    public override void Revert() => _target.SetActive(false);
}
