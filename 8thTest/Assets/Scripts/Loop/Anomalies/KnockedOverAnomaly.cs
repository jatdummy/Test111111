using UnityEngine;

// 물건이 넘어져 있고 안에 있던 것이 바닥에 쏟아져 있음
public class KnockedOverAnomaly : Anomaly
{
    // 넘어지는 물건
    [SerializeField] private Transform _target;

    // 넘어질 때 더하는 회전 (월드 기준 각도)
    [SerializeField] private Vector3 _fallenEuler = new(90f, 0f, 0f);

    // 넘어졌을 때 바닥에 눕도록 옮기는 거리 (월드 기준)
    [SerializeField] private Vector3 _fallenOffset;

    // 바닥에 쏟아진 것들
    [SerializeField] private GameObject _spill;

    private Vector3 _position;
    private Quaternion _rotation;

    private void Awake()
    {
        _position = _target.position;
        _rotation = _target.rotation;
    }

    public override void Apply(int hour)
    {
        _target.SetPositionAndRotation(_position + _fallenOffset, Quaternion.Euler(_fallenEuler) * _rotation);
        _spill.SetActive(true);
    }

    public override void Revert()
    {
        _target.SetPositionAndRotation(_position, _rotation);
        _spill.SetActive(false);
    }
}
