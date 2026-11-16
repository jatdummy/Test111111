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

    private InputAction _interactAction;
    private FirstPersonController _rider;
    private bool _isRiding;

    private void Awake() => _interactAction = InputSystem.actions.FindAction("Player/Interact");

    private void Start() => SetDisplay(IdleText);

    private void Update() => TryRide();

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
        _doorPanel.SetActive(true);
        SetDisplay(RidingText);
        yield return new WaitForSeconds(_closedSeconds);

        _loopManager.HandleTrigger(LoopTriggerType.Back, _rider);
        _doorPanel.SetActive(false);
        SetDisplay(IdleText);
        _isRiding = false;
    }

    private void SetDisplay(string text)
    {
        if (_display != null) _display.text = text;
    }
}
