using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 06:00 이후 퇴근용 엘리베이터: 타고 E를 누르면 문이 닫힌 뒤 퇴근 처리 (밤에는 잠김)
[RequireComponent(typeof(BoxCollider))]
public class Elevator : MonoBehaviour
{
    private const string IdleText = "▼ 퇴근";
    private const string RidingText = "▼ 내려가는 중…";

    // 밤 동안 (06:00 전) 표시 — 엘리베이터는 퇴근 전용이라 잠겨 있음
    private const string LockedText = "운행 중지";

    // 문이 닫혀 있는 시간(초)
    [SerializeField, Min(0f)] private float _closedSeconds = 1.5f;

    // 닫혔을 때 보이는 문짝
    [SerializeField] private GameObject _doorPanel;

    // 퇴근 가능 여부를 묻고 퇴근을 알릴 루프 관리자
    [SerializeField] private LoopManager _loopManager;

    // 엘리베이터 표시창 (퇴근 / 내려가는 중 / 운행 중지)
    [SerializeField] private TextMesh _display;

    // 플레이어 (문 앞에 왔는지 확인용)
    [SerializeField] private Transform _player;

    // 문 앞에서 이 거리 안에 들어오면 문이 열림
    [SerializeField, Min(0f)] private float _callDistance = 2.2f;

    // 타기 (Player/Interact)
    [SerializeField] private InputActionReference _interactAction;

    private FirstPersonController _rider;
    private bool _isRiding;
    private SlidingDoor _door;
    private bool _isHeldOpen;
    private bool _isHeldClosed;

    // 문이 완전히 닫혔는지 (퇴근 연출에서 기다릴 때 사용)
    public bool IsDoorClosed => _door.IsClosed;

    private void Awake()
    {
        _door = _doorPanel.GetComponent<SlidingDoor>();
    }

    private void Start() => SetDisplay(LockedText);

    private void Update()
    {
        TryRide();
        UpdateDoor();

        // 타는 중이 아니면 퇴근 가능 여부에 맞춰 표시를 바꿈
        if (!_isRiding)
        {
            SetDisplay(_loopManager.CanUseElevator ? IdleText : LockedText);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out FirstPersonController player))
        {
            _rider = player;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out FirstPersonController _))
        {
            _rider = null;
        }
    }

    // 퇴근 연출에서 동료들이 타는 동안 문을 열어 둠
    public void HoldOpen(bool isHeld) => _isHeldOpen = isHeld;

    // 퇴근 연출에서 동료들을 태우고 문을 닫아 둠
    public void HoldClosed(bool isHeld) => _isHeldClosed = isHeld;

    private void TryRide()
    {
        if (_rider == null || _isRiding) return;
        if (!_loopManager.CanUseElevator) return;
        if (!_interactAction.action.WasPressedThisFrame()) return;

        StartCoroutine(RideRoutine());
    }

    private IEnumerator RideRoutine()
    {
        _isRiding = true;
        _door.Close();
        SetDisplay(RidingText);

        // 문이 다 닫힌 뒤부터 퇴근 시간을 셈 (닫히는 도중에 사무실이 바뀌어 보이지 않게)
        // 닫히는 도중에 내리면 퇴근을 취소함 (밖에서 사무실이 바뀌는 모습이 보이지 않게, 문은 UpdateDoor가 다시 엶)
        while (!_door.IsClosed)
        {
            if (_rider == null)
            {
                SetDisplay(IdleText);
                _isRiding = false;
                yield break;
            }

            yield return null;
        }
        yield return new WaitForSeconds(_closedSeconds);

        _loopManager.HandleTrigger(LoopTriggerType.Back, _rider);
        SetDisplay(IdleText);

        // 타는 중이 끝나면 UpdateDoor가 안에 있는 플레이어를 보고 문을 다시 엶
        _isRiding = false;
    }

    // 탈 수 있을 때 문 앞에 오거나 안에 있으면 열리고, 그 밖에는 닫혀 있음
    private void UpdateDoor()
    {
        if (_isRiding) return;

        bool isNear = Vector3.Distance(_player.position, _doorPanel.transform.position) <= _callDistance;
        bool shouldOpen = !_isHeldClosed && (_isHeldOpen || _rider != null || (isNear && _loopManager.CanUseElevator));
        if (shouldOpen)
        {
            _door.Open();
        }
        else
        {
            _door.Close();
        }
    }

    private void SetDisplay(string text)
    {
        if (_display != null)
        {
            _display.text = text;
        }
    }
}
