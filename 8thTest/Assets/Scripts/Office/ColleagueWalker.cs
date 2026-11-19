using UnityEngine;

// 동료 NPC의 통로 걷기와 시선 처리
public class ColleagueWalker : MonoBehaviour
{
    private const float MinFaceDistance = 0.01f;

    // 앉을 방향과 이 각도 안으로 맞으면 앉기 시작
    private const float SitFacingTolerance = 2f;

    // 걷기 속도(초당 거리)
    [SerializeField, Min(0f)] private float _walkSpeed = 1.1f;

    // 걸으며 방향을 트는 속도(초당 각도, 꺾이는 지점에서 한 번에 휙 돌지 않게)
    [SerializeField, Min(0f)] private float _turnSpeed = 360f;

    // 걷기 시작 위치
    [SerializeField] private Vector3 _startPosition;

    // 탕비실 문 앞에서 통로로 나오는 위치 (책상 섬을 가로지르지 않게 통로로 먼저 나옴)
    [SerializeField] private Vector3 _doorwayPosition;

    // 자리 앞 통로 위치 (다른 책상을 피해 여기까지 걸은 뒤 자리로 들어감)
    [SerializeField] private Vector3 _aislePosition;

    // 걷기 끝 위치 (앉을 자리)
    [SerializeField] private Vector3 _endPosition;

    // 앉을 때 내려가는 높이 (앉는 애니메이션이 엉덩이를 내리므로 의자 높이 보정용)
    [SerializeField, Min(0f)] private float _sitDrop;

    // 앉았을 때 바라보는 방향
    [SerializeField] private Vector3 _sitDirection = Vector3.back;

    // 걷기 · 앉기 · 타자 동작
    [SerializeField] private NpcBody _body;

    private Transform _player;
    private bool _isWalking;
    private bool _isTurningToSeat;
    private bool _isStaring;
    private Vector3 _target;

    private void Update()
    {
        if (_isTurningToSeat)
        {
            TurnToSeat();
            return;
        }

        if (!_isWalking) return;

        Face(_target - transform.position);
        WalkToEnd();
    }

    // 걷기 전 시작 위치로 되돌리기
    public void ResetWalk(bool isStaring)
    {
        _isWalking = false;
        _isTurningToSeat = false;
        _isStaring = isStaring;
        _body.StandUp();
        _body.LookAt(null);
        _target = _doorwayPosition;
        transform.position = _startPosition;
        transform.rotation = Quaternion.LookRotation(_doorwayPosition - _startPosition);
    }

    public void BeginWalk(Transform player)
    {
        _player = player;
        _isWalking = true;
        _body.Walk(true);

        // 이상현상이면 걷는 내내 고개만 돌려 플레이어를 봄 (몸은 걷는 방향 그대로)
        _body.LookAt(_isStaring ? _player : null);
    }

    private void WalkToEnd()
    {
        transform.position = Vector3.MoveTowards(transform.position, _target, _walkSpeed * Time.deltaTime);
        if (transform.position != _target) return;

        // 통로에 도착하면 자리로 꺾고, 자리에 도착하면 앉을 방향으로 돎
        // 문 앞에 도착하면 통로를 따라 자리 앞까지 감
        if (_target == _endPosition)
        {
            _isWalking = false;
            _isTurningToSeat = true;
        }
        else if (_target == _aislePosition)
        {
            _target = _endPosition;
        }
        else
        {
            _target = _aislePosition;
        }
    }

    // 앉을 방향으로 다 돌면 자리에 앉음
    private void TurnToSeat()
    {
        Face(_sitDirection);
        if (Quaternion.Angle(transform.rotation, Quaternion.LookRotation(_sitDirection)) > SitFacingTolerance) return;

        _isTurningToSeat = false;
        _body.LookAt(null);
        transform.position += Vector3.down * _sitDrop;
        _body.SitAndType();
    }

    // 몸을 주어진 방향으로 조금씩 돌림
    private void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < MinFaceDistance) return;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), _turnSpeed * Time.deltaTime);
    }
}
