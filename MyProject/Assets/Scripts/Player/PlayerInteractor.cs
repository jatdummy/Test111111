using UnityEngine;
using UnityEngine.InputSystem;

// 시선 앞 상호작용 대상 감지와 E 입력 처리
public class PlayerInteractor : MonoBehaviour
{
    // 상호작용 입력
    [SerializeField] private InputActionReference _interactAction;

    // 시선 판정 시작점(카메라)
    [SerializeField] private Transform _rayOrigin;

    // 상호작용 가능 거리
    [SerializeField] private float _interactDistance = 2.5f;

    // 조작 허용 여부를 읽을 이동 컨트롤러
    [SerializeField] private FirstPersonController _controller;

    // 안내 문구를 띄울 화면 UI
    [SerializeField] private ScreenOverlayView _overlay;

    private IInteractable _currentTarget;

    private void OnEnable() => _interactAction.action.performed += HandleInteractPerformed;

    private void Update() => FindTarget();

    private void OnDisable() => _interactAction.action.performed -= HandleInteractPerformed;

    // 시선 앞 상호작용 대상 갱신과 안내 문구 표시
    private void FindTarget()
    {
        _currentTarget = null;

        if (_controller.IsInputEnabled
            && Physics.Raycast(_rayOrigin.position, _rayOrigin.forward, out RaycastHit hit, _interactDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            IInteractable target = hit.collider.GetComponentInParent<IInteractable>();
            if (!IsMissing(target) && target.CanInteract)
                _currentTarget = target;
        }

        if (IsMissing(_currentTarget))
        {
            _overlay.HidePrompt();
        }
        else
        {
            _overlay.ShowPrompt(_currentTarget.PromptText);
        }
    }

    private void HandleInteractPerformed(InputAction.CallbackContext context)
    {
        if (!_controller.IsInputEnabled) return;
        if (IsMissing(_currentTarget)) return;

        _currentTarget.Interact();
    }

    // 파괴된 Unity 오브젝트까지 포함한 null 판정
    private static bool IsMissing(IInteractable target) => target == null || (target is Object unityObject && unityObject == null);
}
