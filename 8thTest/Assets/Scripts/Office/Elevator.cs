using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 엘리베이터 호출 (뒤로 = 롤백), 문이 닫힌 동안 사무실을 다시 빌드
[RequireComponent(typeof(BoxCollider))]
public class Elevator : MonoBehaviour
{
    private const string IdleText = "◀ 롤백";
    private const string RidingText = "◀ 롤백 중…";

    // 문이 닫혀 있는 시간(초)
    [SerializeField] private float _closedSeconds = 1.5f;

    // 닫혔을 때 보이는 문짝
    [SerializeField] private GameObject _doorPanel;

    // 판정을 알릴 루프 관리자
    [SerializeField] private LoopManager _loopManager;

    // 엘리베이터 안 표시창 (타는 동안 롤백 진행 표시)
    [SerializeField] private TextMesh _display;

    // 플레이어 (문 앞에 왔는지 확인용)
    [SerializeField] private Transform _player;

    // 문 앞에서 이 거리 안에 들어오면 문이 열림
    [SerializeField] private float _callDistance = 2.2f;

    private InputAction _interactAction;
    private FirstPersonController _rider;
    private bool _isRiding;
    private SlidingDoor _door;
    private bool _isHeldOpen;
    private bool _isHeldClosed;

    // 문이 완전히 닫혔는지 (퇴근 연출에서 기다릴 때 사용)
    public bool IsDoorClosed => _door.IsClosed;

    private void Awake()
    {
        _interactAction = InputSystem.actions.FindAction("Player/Interact");
        _door = _doorPanel.GetComponent<SlidingDoor>();
    }

    private void Start() => SetDisplay(IdleText);

    private void Update()
    {
        TryRide();
        UpdateDoor();
    }

    // 퇴근 연출에서 동료들이 타는 동안 문을 열어 둠
    public void HoldOpen(bool isHeld) => _isHeldOpen = isHeld;

    // 퇴근 연출에서 동료들을 태우고 문을 닫아 둠
    public void HoldClosed(bool isHeld) => _isHeldClosed = isHeld;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out FirstPersonController player)) _rider = player;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out FirstPersonController _)) _rider = null;
    }

    private void TryRide()
    {
        if (_rider == null || _isRiding) return;
        if (!_loopManager.CanUseElevator) return;
        if (!_interactAction.WasPressedThisFrame()) return;

        StartCoroutine(Ride());
    }

    private IEnumerator Ride()
    {
        _isRiding = true;
        _door.Close();
        SetDisplay(RidingText);

        // 문이 다 닫힌 뒤부터 롤백 시간을 셈 (닫히는 도중에 사무실이 바뀌어 보이지 않게)
        // 닫히는 도중에 내리면 롤백을 취소함 (밖에서 사무실이 바뀌는 모습이 보이지 않게, 문은 UpdateDoor가 다시 엶)
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
        if (shouldOpen) _door.Open();
        else _door.Close();
    }

    private void SetDisplay(string text)
    {
        if (_display != null) _display.text = text;
    }
}
