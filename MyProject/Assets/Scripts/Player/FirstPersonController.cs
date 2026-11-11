using UnityEngine;
using UnityEngine.InputSystem;

// 1인칭 이동과 시점(달리기 없음)
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    private const float MaxPitch = 85f;
    private const float GroundedVerticalSpeed = -1f;

    [Header("입력")]
    // 이동 입력
    [SerializeField] private InputActionReference _moveAction;

    // 시점 입력
    [SerializeField] private InputActionReference _lookAction;

    [Header("이동")]
    // 걷는 속도(초당 거리)
    [SerializeField] private float _walkSpeed = 2.2f;

    // 중력 가속도
    [SerializeField] private float _gravity = -9.81f;

    [Header("시점")]
    // 위아래로 회전하는 카메라 축
    [SerializeField] private Transform _cameraPivot;

    // 마우스 감도(이동량 1당 회전 각도)
    [SerializeField] private float _lookSensitivity = 0.1f;

    private CharacterController _characterController;
    private float _pitch;
    private float _verticalSpeed;
    private bool _isInputEnabled;

    public bool IsInputEnabled => _isInputEnabled;

    private void Awake() => _characterController = GetComponent<CharacterController>();

    private void Update()
    {
        if (_isInputEnabled)
            Look();

        Move();
    }

    // 조작 허용 여부와 커서 잠금 전환
    public void SetInputEnabled(bool isEnabled)
    {
        _isInputEnabled = isEnabled;
        Cursor.lockState = isEnabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isEnabled;
    }

    // 지정 위치와 방향으로 순간 이동
    public void Teleport(Vector3 position, float yaw)
    {
        // CharacterController가 켜져 있으면 위치 변경이 무시됨
        _characterController.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        _characterController.enabled = true;

        _pitch = 0f;
        _verticalSpeed = 0f;
        _cameraPivot.localRotation = Quaternion.identity;
    }

    // 마우스 입력으로 좌우는 몸, 위아래는 카메라 회전
    private void Look()
    {
        Vector2 delta = _lookAction.action.ReadValue<Vector2>() * _lookSensitivity;
        transform.Rotate(0f, delta.x, 0f);
        _pitch = Mathf.Clamp(_pitch - delta.y, -MaxPitch, MaxPitch);
        _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // 걷기 이동과 중력 적용
    private void Move()
    {
        Vector2 input = _isInputEnabled ? Vector2.ClampMagnitude(_moveAction.action.ReadValue<Vector2>(), 1f) : Vector2.zero;
        Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * _walkSpeed;

        _verticalSpeed = _characterController.isGrounded ? GroundedVerticalSpeed : _verticalSpeed + _gravity * Time.deltaTime;
        velocity.y = _verticalSpeed;

        _characterController.Move(velocity * Time.deltaTime);
    }
}
