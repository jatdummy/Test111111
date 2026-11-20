using UnityEngine;

// 동료 NPC가 탕비실에서 통로를 따라 자기 자리로 걸어와 회전의자에 앉는 흐름과 시선 처리
// 빈자리 의자는 통로 쪽을 보고 있고, 의자 앞에서 뒤돌아 앉은 뒤 의자째 책상 쪽으로 돎
public class ColleagueWalker : MonoBehaviour
{
    // 앉을 방향과 이 각도 안으로 맞으면 앉기 시작
    private const float SitFacingTolerance = 2f;

    // 걷기 속도(초당 거리)
    [SerializeField, Min(0f)] private float _walkSpeed = 0.85f;

    // 걸으며 방향을 트는 속도(초당 각도, 꺾이는 지점에서 한 번에 휙 돌지 않게)
    [SerializeField, Min(0f)] private float _turnSpeed = 360f;

    // 걷기 시작 위치
    [SerializeField] private Vector3 _startPosition;

    // 탕비실 문 앞에서 통로로 나오는 위치 (책상 섬을 가로지르지 않게 통로로 먼저 나옴)
    [SerializeField] private Vector3 _doorwayPosition;

    // 자리 앞 통로 위치 (다른 책상을 피해 여기까지 걸은 뒤 자리로 들어감)
    [SerializeField] private Vector3 _aislePosition;

    // 앉을 회전의자 (씬에 놓인 방향이 책상을 보는 방향, 위치가 앉을 자리)
    [SerializeField] private Transform _chair;

    // 앉기 시작할 때 의자 앞에 떨어져 서는 거리 (앉는 동작이 몸을 뒤로 옮기는 거리, 다 앉으면 의자 축 위에 오게)
    [SerializeField, Min(0f)] private float _standOffDistance = 0.335f;

    // 앉은 채 의자째 책상 쪽으로 도는 시간
    [SerializeField, Min(0.01f)] private float _swivelSeconds = 1f;

    // 걷기 · 앉기 · 타자 동작
    [SerializeField] private NpcBody _body;

    private Transform _player;
    private bool _isWalking;
    private bool _isTurningToSeat;
    private bool _isWaitingToSwivel;
    private bool _isSwiveling;
    private float _swivelElapsed;
    private float _swivelAngle;
    private Quaternion _chairDeskRotation;
    private bool _hasChairDeskRotation;
    private bool _isStaring;
    private Vector3 _target;

    // 의자가 책상을 볼 때의 앞 방향 (다 앉은 뒤 바라볼 방향)
    private Vector3 DeskDirection => _chairDeskRotation * Vector3.forward;

    // 통로 쪽을 보고 앉음 (앉은 뒤 의자째 돌아 책상을 봄)
    private Vector3 SitFacing => -DeskDirection;

    // 앉기 시작할 위치 (의자에서 통로 쪽으로 떨어진 곳)
    private Vector3 SeatApproachPosition => _chair.position - DeskDirection * _standOffDistance;

    private void Awake() => TurnChairToAisle();

    private void Update()
    {
        if (_isWaitingToSwivel)
        {
            // 엉덩이가 의자에 닿고 나서 돌기 시작
            if (!_body.IsSeated) return;

            _isWaitingToSwivel = false;
            _isSwiveling = true;
            _swivelElapsed = 0f;
            _swivelAngle = 0f;
            return;
        }

        if (_isSwiveling)
        {
            Swivel();
            return;
        }

        if (_isTurningToSeat)
        {
            TurnToSeat();
            return;
        }

        if (!_isWalking) return;

        NpcMotion.TurnTowards(transform, _target - transform.position, _turnSpeed);
        WalkToEnd();
    }

    // 걷기 전 시작 위치로 되돌리기
    public void ResetWalk(bool isStaring)
    {
        _isWalking = false;
        _isTurningToSeat = false;
        _isWaitingToSwivel = false;
        _isSwiveling = false;
        _isStaring = isStaring;
        TurnChairToAisle();
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
        if (!NpcMotion.StepTowards(transform, _target, _walkSpeed)) return;

        // 문 앞에 도착하면 통로를 따라 자리 앞까지 감
        // 통로에 도착하면 의자 앞으로 꺾고, 의자 앞에 도착하면 앉을 방향으로 돎
        if (_target == SeatApproachPosition)
        {
            _isWalking = false;
            _isTurningToSeat = true;
        }
        else if (_target == _aislePosition)
        {
            _target = SeatApproachPosition;
        }
        else
        {
            _target = _aislePosition;
        }
    }

    // 앉을 방향으로 다 돌면 자리에 앉음
    private void TurnToSeat()
    {
        NpcMotion.TurnTowards(transform, SitFacing, _turnSpeed);
        if (Quaternion.Angle(transform.rotation, Quaternion.LookRotation(SitFacing)) > SitFacingTolerance) return;

        _isTurningToSeat = false;
        _body.LookAt(null);
        _body.SitAndType();
        _isWaitingToSwivel = true;
    }

    // 앉은 채 의자 축을 중심으로 사람과 의자를 같이 책상 쪽으로 돌림 (천천히 출발해 천천히 멈춤)
    private void Swivel()
    {
        _swivelElapsed += Time.deltaTime;
        float targetAngle = 180f * Mathf.SmoothStep(0f, 1f, _swivelElapsed / _swivelSeconds);
        float step = targetAngle - _swivelAngle;
        _swivelAngle = targetAngle;

        transform.RotateAround(_chair.position, Vector3.up, step);
        _chair.RotateAround(_chair.position, Vector3.up, step);

        if (_swivelElapsed < _swivelSeconds) return;

        _isSwiveling = false;
        _chair.rotation = _chairDeskRotation;
    }

    // 빈자리 의자를 통로 쪽으로 돌려 둠 (자리를 잠깐 비운 사람처럼)
    private void TurnChairToAisle()
    {
        // 처음 한 번, 씬에 놓인 의자 방향 = 책상을 보는 방향 (다 앉은 뒤 돌아갈 방향)으로 기억
        if (!_hasChairDeskRotation)
        {
            _chairDeskRotation = _chair.rotation;
            _hasChairDeskRotation = true;
        }

        _chair.rotation = Quaternion.AngleAxis(180f, Vector3.up) * _chairDeskRotation;
    }
}
