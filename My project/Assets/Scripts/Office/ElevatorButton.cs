using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 범위 안 Interact 입력 시 엘리베이터 제출 알림
public class ElevatorButton : MonoBehaviour
{
    [SerializeField] private InputActionReference _interactAction;

    [Header("표시")]
    // 활성 상태에 따라 머티리얼을 바꿀 버튼 렌더러
    [SerializeField] private Renderer _buttonRenderer;
    [SerializeField] private Material _activeMaterial;
    [SerializeField] private Material _inactiveMaterial;

    private bool _isInteractable;
    private bool _isPlayerInRange;

    public event Action OnPressed;

    private void Update()
    {
        if (!_isInteractable || !_isPlayerInRange) return;

        if (_interactAction.action.WasPressedThisFrame())
            OnPressed?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _isPlayerInRange = false;
    }

    // 상호작용 가능 여부와 버튼 표시 갱신
    public void SetInteractable(bool isInteractable)
    {
        _isInteractable = isInteractable;
        // 텔레포트로 콜라이더가 꺼질 때 OnTriggerExit 누락 대비
        _isPlayerInRange = false;
        _buttonRenderer.sharedMaterial = isInteractable ? _activeMaterial : _inactiveMaterial;
    }
}
