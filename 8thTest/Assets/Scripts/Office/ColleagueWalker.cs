using UnityEngine;

// 동료 NPC의 통로 걷기와 시선 처리
public class ColleagueWalker : MonoBehaviour
{
    private const float MinFaceDistance = 0.01f;

    // 걷기 속도(초당 거리)
    [SerializeField] private float _walkSpeed = 1.1f;

    // 걷기 시작 위치
    [SerializeField] private Vector3 _startPosition;

    // 탕비실 문 앞에서 통로로 나오는 위치 (책상 섬을 가로지르지 않게 통로로 먼저 나옴)
    [SerializeField] private Vector3 _doorwayPosition;

    // 자리 앞 통로 위치 (다른 책상을 피해 여기까지 걸은 뒤 자리로 들어감)
    [SerializeField] private Vector3 _aislePosition;

    // 걷기 끝 위치 (앉을 자리)
    [SerializeField] private Vector3 _endPosition;

    // 앉을 때 내려가는 높이
    [SerializeField] private float _sitDrop = 0.35f;

    // 앉았을 때 바라보는 방향
    [SerializeField] private Vector3 _sitDirection = Vector3.back;

    // 앉은 자세
    [SerializeField] private SitPose _sitPose;

    // 걷는 동작
    [SerializeField] private WalkPose _walkPose;

    // 앉은 뒤 타자 치는 동작 (07은 쉬지 않고 빌드를 돌림)
    [SerializeField] private TypingPose _typingPose;

    private Transform _player;
    private bool _isWalking;
    private bool _isStaring;
    private Vector3 _target;

    private void Update()
    {
        if (!_isWalking) return;

        Face();
        WalkToEnd();
    }

    // 걷기 전 시작 위치로 되돌리기
    public void ResetWalk(bool isStaring)
    {
        _isWalking = false;
        _isStaring = isStaring;
        _sitPose.enabled = false;
        _walkPose.enabled = false;
        _typingPose.enabled = false;
        _target = _doorwayPosition;
        transform.position = _startPosition;
        transform.rotation = Quaternion.LookRotation(_doorwayPosition - _startPosition);
    }

    public void BeginWalk(Transform player)
    {
        _player = player;
        _isWalking = true;
        _walkPose.Speed = _walkSpeed;
        _walkPose.enabled = true;
    }

    private void WalkToEnd()
    {
        transform.position = Vector3.MoveTowards(transform.position, _target, _walkSpeed * Time.deltaTime);
        if (transform.position != _target) return;

        // 통로에 도착하면 자리로 꺾고, 자리에 도착하면 앉음
        // 문 앞에 도착하면 통로를 따라 자리 앞까지 감
        if (_target == _endPosition)
        {
            SitDown();
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

    // 도착하면 자리에 앉음
    private void SitDown()
    {
        _isWalking = false;
        _walkPose.enabled = false;
        transform.position += Vector3.down * _sitDrop;
        transform.rotation = Quaternion.LookRotation(_sitDirection);
        _sitPose.enabled = true;
        _typingPose.enabled = true;
    }

    // 평소에는 걷는 방향, 이상현상이면 플레이어 쪽
    private void Face()
    {
        Vector3 target = _isStaring ? _player.position : _target;
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < MinFaceDistance) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
