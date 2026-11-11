using UnityEngine;

// 플레이어 접근 시 열리고 벗어나면 닫히는 문, 일방통행이면 통과 후 닫고 잠금
public class ProximityDoor : MonoBehaviour
{
    [Header("문짝")]
    // 열고 닫을 문짝
    [SerializeField] private Transform _doorLeaf;

    // 닫힘 위치에서 열림 위치까지의 문짝 로컬 이동량
    [SerializeField] private Vector3 _openOffset = Vector3.left;

    // 문짝 이동 속도(초당 거리)
    [SerializeField] private float _moveSpeed = 2f;

    [Header("일방통행")]
    // 문 앞쪽(forward)으로 통과하면 닫고 잠그는지 여부
    [SerializeField] private bool _isOneWay;

    // 문 평면에서 앞쪽으로 이 거리를 넘으면 통과로 보는 기준(m)
    [SerializeField] private float _passDistance = 0.6f;

    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private Transform _playerInRange;
    private bool _isLocked;

    // 일방통행 통과 후 잠긴 상태 여부
    public bool IsLocked => _isLocked;

    private void Awake()
    {
        _closedPosition = _doorLeaf.localPosition;
        _openPosition = _closedPosition + _openOffset;
    }

    private void Update()
    {
        if (_isOneWay && !_isLocked && _playerInRange != null && HasPassed())
            _isLocked = true;

        bool isOpen = _playerInRange != null && !_isLocked;
        Vector3 target = isOpen ? _openPosition : _closedPosition;
        _doorLeaf.localPosition = Vector3.MoveTowards(_doorLeaf.localPosition, target, _moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = other.transform;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _playerInRange = null;
    }

    // 잠금 해제와 즉시 닫힘 상태 복귀
    public void ResetDoor()
    {
        _isLocked = false;
        _doorLeaf.localPosition = _closedPosition;
    }

    // 플레이어가 문 앞쪽으로 통과 기준 거리를 넘었는지 여부
    private bool HasPassed() => Vector3.Dot(_playerInRange.position - transform.position, transform.forward) > _passDistance;
}
