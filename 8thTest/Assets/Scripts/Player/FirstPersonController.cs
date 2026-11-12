using UnityEngine;
using UnityEngine.InputSystem;

// 1인칭 이동, 달리기, 시점 처리
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    private const float Gravity = -9.81f;
    private const float MaxPitch = 80f;
    private const float GroundedVelocity = -1f;

    [Header("이동")]
    // 걷기 속도(초당 거리)
    [SerializeField] private float _walkSpeed = 2.2f;

    // 달리기 속도(초당 거리)
    [SerializeField] private float _runSpeed = 4f;

    [Header("시점")]
    // 마우스 감도(픽셀당 회전 각도)
    [SerializeField] private float _lookSensitivity = 0.1f;

    // 위아래로 회전하는 카메라 축
    [SerializeField] private Transform _cameraPivot;

    private CharacterController _controller;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _sprintAction;
    private float _pitch;
    private float _verticalVelocity;

    private void Awake() => CacheComponents();

    private void Start() => SetCursorLocked(true);

    private void Update()
    {
        UpdateCursorLock();
        Look();
        Move();
    }

    // 위치와 방향을 유지한 채 순간이동
    public void Teleport(Vector3 offset)
    {
        _controller.enabled = false;
        transform.position += offset;
        _controller.enabled = true;
    }

    private void CacheComponents()
    {
        _controller = GetComponent<CharacterController>();
        _moveAction = InputSystem.actions.FindAction("Player/Move");
        _lookAction = InputSystem.actions.FindAction("Player/Look");
        _sprintAction = InputSystem.actions.FindAction("Player/Sprint");
    }

    private void Look()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = _lookAction.ReadValue<Vector2>() * _lookSensitivity;
        transform.Rotate(0f, delta.x, 0f);
        _pitch = Mathf.Clamp(_pitch - delta.y, -MaxPitch, MaxPitch);
        _cameraPivot.localEulerAngles = new Vector3(_pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        float speed = _sprintAction.IsPressed() ? _runSpeed : _walkSpeed;
        Vector3 direction = transform.right * input.x + transform.forward * input.y;

        // 바닥에 닿아 있으면 낙하 속도 초기화
        if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = GroundedVelocity;
        _verticalVelocity += Gravity * Time.deltaTime;

        Vector3 velocity = direction * speed + Vector3.up * _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }

    // Esc로 커서 해제, 클릭으로 다시 잠금
    private void UpdateCursorLock()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) SetCursorLocked(true);
    }

    private void SetCursorLocked(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }
}
