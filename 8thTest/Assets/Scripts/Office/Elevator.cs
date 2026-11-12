using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 엘리베이터 호출 (뒤로 = 수정 요청), 문이 닫힌 동안 사무실을 다시 빌드
[RequireComponent(typeof(BoxCollider))]
public class Elevator : MonoBehaviour
{
    // 문이 닫혀 있는 시간(초)
    [SerializeField] private float _closedSeconds = 1.5f;

    // 닫혔을 때 보이는 문짝
    [SerializeField] private GameObject _doorPanel;

    // 판정을 알릴 루프 관리자
    [SerializeField] private LoopManager _loopManager;

    private InputAction _interactAction;
    private FirstPersonController _rider;
    private bool _isRiding;

    private void Awake() => _interactAction = InputSystem.actions.FindAction("Player/Interact");

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
        if (!_interactAction.WasPressedThisFrame()) return;

        StartCoroutine(Ride());
    }

    private IEnumerator Ride()
    {
        _isRiding = true;
        _doorPanel.SetActive(true);
        yield return new WaitForSeconds(_closedSeconds);

        _loopManager.HandleTrigger(LoopTriggerType.Back, _rider);
        _doorPanel.SetActive(false);
        _isRiding = false;
    }
}
