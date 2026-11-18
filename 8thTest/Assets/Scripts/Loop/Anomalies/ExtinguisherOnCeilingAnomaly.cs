using UnityEngine;

// 소화기가 뒤집혀 천장에 붙어 있음
public class ExtinguisherOnCeilingAnomaly : Anomaly
{
    // 천장에 붙을 때의 높이
    [SerializeField, Min(0f)] private float _ceilingHeight = 2.7f;

    // 천장에 붙을 때 벽에서 떨어뜨리는 거리 (벽에 박히지 않고 천장 면에 보이게)
    [SerializeField] private Vector3 _ceilingInset = new(0f, 0f, -0.3f);

    private Vector3 _position;
    private Quaternion _rotation;

    private void Awake()
    {
        _position = transform.position;
        _rotation = transform.rotation;
    }

    public override void Apply(int hour)
    {
        Vector3 ceilingPosition = new(_position.x, _ceilingHeight, _position.z);
        // 앞뒤(Z축) 기준으로 뒤집어야 벽 쪽으로 넘어가지 않음
        transform.SetPositionAndRotation(ceilingPosition + _ceilingInset, _rotation * Quaternion.Euler(0f, 0f, 180f));
    }

    public override void Revert() => transform.SetPositionAndRotation(_position, _rotation);
}
