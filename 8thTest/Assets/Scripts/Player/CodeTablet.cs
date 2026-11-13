using UnityEngine;
using UnityEngine.InputSystem;

// Tab을 누르고 있는 동안 코드를 보여 주는 태블릿을 들어 올림
public class CodeTablet : MonoBehaviour
{
    // 들어 올리고 내리는 속도 (초당 진행도, 1이면 1초)
    [SerializeField] private float _raiseSpeed = 6f;

    // 태블릿 본체 (카메라 자식)
    [SerializeField] private Transform _tablet;

    // 내렸을 때 카메라 기준 위치와 각도 (화면 아래로 숨김)
    [SerializeField] private Vector3 _loweredPosition = new(0f, -0.4f, 0.3f);
    [SerializeField] private Vector3 _loweredEuler = new(60f, 0f, 0f);

    // 들었을 때 카메라 기준 위치와 각도
    [SerializeField] private Vector3 _raisedPosition = new(0f, -0.05f, 0.34f);
    [SerializeField] private Vector3 _raisedEuler = new(8f, 0f, 0f);

    private InputAction _viewAction;
    private float _progress;

    // 조금이라도 들려 있으면 태블릿을 보는 중
    public bool IsRaised => _progress > 0f;

    private void Awake() => _viewAction = InputSystem.actions.FindAction("Player/CodeView");

    private void Start() => ApplyPose();

    private void Update()
    {
        float target = _viewAction.IsPressed() ? 1f : 0f;
        _progress = Mathf.MoveTowards(_progress, target, _raiseSpeed * Time.deltaTime);
        ApplyPose();
    }

    // 진행도에 맞춰 내린 자세와 든 자세 사이를 부드럽게 이동
    private void ApplyPose()
    {
        _tablet.gameObject.SetActive(IsRaised);

        float t = Mathf.SmoothStep(0f, 1f, _progress);
        _tablet.localPosition = Vector3.Lerp(_loweredPosition, _raisedPosition, t);
        _tablet.localRotation = Quaternion.Slerp(Quaternion.Euler(_loweredEuler), Quaternion.Euler(_raisedEuler), t);
    }
}
