using UnityEngine;

// 평소 고정된 감시카메라가 플레이어를 천천히 따라 고개를 돌림
public class SecurityCameraAnomaly : Anomaly
{
    // 돌아가는 카메라 머리 (렌즈가 앞쪽 +Z를 향함)
    [SerializeField] private Transform _head;

    // 따라 볼 대상 (플레이어 카메라)
    [SerializeField] private Transform _target;

    // 고개를 돌리는 속도 (초당 각도, 느릴수록 섬뜩함)
    [SerializeField, Min(0f)] private float _turnSpeed = 30f;

    // 원래 방향에서 최대로 돌아가는 각도 (벽 뒤를 보는 것처럼 꺾이지 않게)
    [SerializeField, Range(0f, 180f)] private float _maxAngle = 100f;

    private Quaternion _originalRotation;
    private bool _isTracking;

    private void Awake() => _originalRotation = _head.rotation;

    // 다른 움직임(플레이어 이동)이 끝난 뒤 바라보게 LateUpdate에서 돌림
    private void LateUpdate()
    {
        if (!_isTracking) return;

        Quaternion lookRotation = Quaternion.LookRotation(_target.position - _head.position);

        // 원래 방향에서 너무 멀리 돌아가면 한계 각도에서 멈춤
        Quaternion limitedRotation = Quaternion.RotateTowards(_originalRotation, lookRotation, _maxAngle);
        _head.rotation = Quaternion.RotateTowards(_head.rotation, limitedRotation, _turnSpeed * Time.deltaTime);
    }

    public override void Apply(int hour) => _isTracking = true;

    public override void Revert()
    {
        _isTracking = false;
        _head.rotation = _originalRotation;
    }
}
