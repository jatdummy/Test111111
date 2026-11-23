using UnityEngine;

// 문이 살짝 열리고 틈 안쪽에서 눈이 보임
public class EyesInDoorAnomaly : Anomaly
{
    // 문짝의 경첩 축 (문짝과 손잡이가 자식)
    [SerializeField] private Transform _hinge;

    // 문이 안쪽으로 열리는 각도
    [SerializeField, Range(0f, 90f)] private float _openAngle = 20f;

    // 문 틈 안쪽에서 보이는 눈
    [SerializeField] private GameObject _eyes;

    private Quaternion _hingeRotation;

    private void Awake() => _hingeRotation = _hinge.localRotation;

    public override void Apply(int hour)
    {
        _hinge.localRotation = _hingeRotation * Quaternion.Euler(0f, _openAngle, 0f);
        _eyes.SetActive(true);
    }

    public override void Revert()
    {
        _hinge.localRotation = _hingeRotation;
        _eyes.SetActive(false);
    }
}
