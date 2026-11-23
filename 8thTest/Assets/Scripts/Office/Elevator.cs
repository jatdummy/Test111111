using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 06:00 이후 퇴근용 엘리베이터: 타고 E를 누르면 문이 닫힌 뒤 퇴근 처리 (밤에는 잠김)
[RequireComponent(typeof(BoxCollider))]
public class Elevator : MonoBehaviour
{
    // 표시창 안내는 플레이어가 타고 내려갈 때만 띄움 (그 밖에는 비워 둬서 동료들의 인사 말풍선과 겹치지 않게)
    private const string RidingText = "▼ 내려가는 중…";
    private const string ArrivedText = "8층 도착";

    // 문이 닫혀 있는 시간(초)
    [SerializeField, Min(0f)] private float _closedSeconds = 1.5f;

    // 도착 안내를 띄워 두는 시간(초)
    [SerializeField, Min(0f)] private float _arrivedSeconds = 3f;

    // 닫혔을 때 보이는 문짝
    [SerializeField] private GameObject _doorPanel;

    // 퇴근 가능 여부를 묻고 퇴근을 알릴 루프 관리자
    [SerializeField] private LoopManager _loopManager;

    // 엘리베이터 표시창 (내려가는 중 / 도착, 그 밖에는 비어 있음)
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

    // 플레이어가 스위치를 눌러 내려가는 중인지 (퇴근 연출이 [E] 안내를 숨길 때 사용)
    public bool IsRiding => _isRiding;

    // 플레이어가 엘리베이터 안에 있는지 (퇴근 연출이 [E] 안내를 엘리베이터 안에서만 보일 때 사용)
    public bool HasRider => _rider != null;

    private void Awake()
    {
        _door = _doorPanel.GetComponent<SlidingDoor>();
    }

    private void Start() => SetDisplay(string.Empty);

    private void Update()
    {
        TryRide();
        UpdateDoor();
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
                SetDisplay(string.Empty);
                _isRiding = false;
                yield break;
            }

            yield return null;
        }
        yield return new WaitForSeconds(_closedSeconds);

        _loopManager.HandleTrigger(LoopTriggerType.Back, _rider);
        SetDisplay(ArrivedText);

        // 타는 중이 끝나면 UpdateDoor가 안에 있는 플레이어를 보고 문을 다시 엶
        _isRiding = false;

        // 잠시 도착 안내를 보여 준 뒤 비움 (그사이 다시 탔으면 내려가는 중 안내를 그대로 둠)
        yield return new WaitForSeconds(_arrivedSeconds);
        if (!_isRiding)
        {
            SetDisplay(string.Empty);
        }
    }

    // 탈 수 있을 때 문 앞에 오거나 안에 있으면 열리고, 그 밖에는 닫혀 있음
    private void UpdateDoor()
    {
        if (_isRiding) return;

        bool isNear = Vector3.Distance(_player.position, _doorPanel.transform.position) <= _callDistance;
        bool isOpenNeeded = !_isHeldClosed && (_isHeldOpen || _rider != null || (isNear && _loopManager.CanUseElevator));
        if (isOpenNeeded)
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
