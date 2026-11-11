using UnityEngine;

// 사무실 직원(동료, 시니어)의 자세와 이동
public class WorkerCharacter : MonoBehaviour
{
    private const float ArrivalDistance = 0.05f;
    private const float TPoseAngle = 90f;
    private const float TurnSpeed = 360f;

    [Header("몸")]
    // 걸을 때 흔들리는 몸 전체
    [SerializeField] private Transform _visual;

    // 머리(고개 회전 축)
    [SerializeField] private Transform _head;

    // 왼팔 어깨 축
    [SerializeField] private Transform _leftArm;

    // 오른팔 어깨 축
    [SerializeField] private Transform _rightArm;

    [Header("이동")]
    // 왕복 순찰 지점(2개 미만이면 제자리 근무)
    [SerializeField] private Transform[] _patrolPoints;

    // 걷는 속도(초당 거리)
    [SerializeField] private float _walkSpeed = 1f;

    // 클리핑 버그일 때 순찰 경로를 옮기는 거리
    [SerializeField] private Vector3 _clippingOffset = new(-3f, 0f, 0f);

    // 걸음 흔들림 높이
    [SerializeField] private float _bobHeight = 0.03f;

    // 걸음 흔들림 빠르기
    [SerializeField] private float _bobFrequency = 9f;

    private Vector3 _homePosition;
    private Quaternion _homeRotation;
    private Vector3 _visualRestPosition;
    private Vector3 _pathOffset;
    private Vector3[] _leavePath;
    private Transform _stareTarget;
    private int _pathIndex;
    private float _bobTime;
    private bool _isPatrolling;

    private void Awake() => CacheHome();

    private void Update() => UpdateMovement();

    // 원본 상태(제자리, 기본 자세)로 복원
    public void ResetState()
    {
        gameObject.SetActive(true);
        transform.SetPositionAndRotation(_homePosition, _homeRotation);
        _visual.localPosition = _visualRestPosition;
        _pathOffset = Vector3.zero;
        _leavePath = null;
        _stareTarget = null;
        _isPatrolling = _patrolPoints.Length > 1;

        // 0번 지점에서 시작하므로 다음 목표는 1번
        _pathIndex = 1;

        SetTPose(false);
        SetHeadYaw(0f);
    }

    public void SetTPose(bool isTPose)
    {
        float angle = isTPose ? TPoseAngle : 0f;
        _leftArm.localRotation = Quaternion.Euler(0f, 0f, -angle);
        _rightArm.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetHeadYaw(float yaw) => _head.localRotation = Quaternion.Euler(0f, yaw, 0f);

    // 이동을 멈추고 대상을 계속 바라보기
    public void SetStareTarget(Transform target) => _stareTarget = target;

    // 순찰 경로를 옆으로 옮겨 책상을 뚫고 걷게 하기
    public void SetClipping(bool isClipping)
    {
        _pathOffset = isClipping ? _clippingOffset : Vector3.zero;
        transform.position = _homePosition + _pathOffset;
    }

    // 경로를 따라 걸어간 뒤 사라지기(퇴근)
    public void LeaveThrough(Vector3[] path)
    {
        _stareTarget = null;
        _isPatrolling = false;
        _leavePath = path;
        _pathIndex = 0;
    }

    private void CacheHome()
    {
        _homePosition = transform.position;
        _homeRotation = transform.rotation;
        _visualRestPosition = _visual.localPosition;
    }

    private void UpdateMovement()
    {
        if (_stareTarget != null)
        {
            FaceTowards(_stareTarget.position);
            return;
        }

        if (_leavePath != null)
        {
            FollowLeavePath();
            return;
        }

        if (_isPatrolling)
            Patrol();
    }

    private void Patrol()
    {
        Vector3 target = _patrolPoints[_pathIndex].position + _pathOffset;
        if (!StepTowards(target)) return;

        _pathIndex = (_pathIndex + 1) % _patrolPoints.Length;
    }

    private void FollowLeavePath()
    {
        if (!StepTowards(_leavePath[_pathIndex])) return;

        _pathIndex++;
        if (_pathIndex < _leavePath.Length) return;

        _leavePath = null;
        gameObject.SetActive(false);
    }

    // 목표 지점으로 한 프레임 이동, 도착하면 true
    private bool StepTowards(Vector3 target)
    {
        FaceTowards(target);
        transform.position = Vector3.MoveTowards(transform.position, target, _walkSpeed * Time.deltaTime);
        Bob();
        return (target - transform.position).sqrMagnitude <= ArrivalDistance * ArrivalDistance;
    }

    // 높이는 무시하고 목표 방향으로 몸 돌리기
    private void FaceTowards(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < ArrivalDistance * ArrivalDistance) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, TurnSpeed * Time.deltaTime);
    }

    private void Bob()
    {
        _bobTime += Time.deltaTime;
        _visual.localPosition = _visualRestPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(_bobTime * _bobFrequency)) * _bobHeight);
    }
}
