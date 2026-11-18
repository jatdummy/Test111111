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
    [SerializeField] private float _runSpeed = 3.6f;

    // 태블릿을 보는 동안 걷기 속도 배율 (0이면 멈춰서 봄, 1이면 감속 없음)
    [SerializeField] private float _readingSpeedMultiplier = 0.5f;

    // 코드 태블릿 (보는 동안 달리기 불가, 감속)
    [SerializeField] private CodeTablet _codeTablet;

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

    // 일시정지 메뉴가 열린 동안 이동 · 시점 · 커서 처리를 멈춤
    public bool IsInputBlocked { get; set; }

    private void Awake() => CacheComponents();

    private void Start() => SetCursorLocked(true);

    private void Update()
    {
        if (IsInputBlocked) return;

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
        float speed = GetSpeed();
        Vector3 direction = transform.right * input.x + transform.forward * input.y;

        // 바닥에 닿아 있으면 낙하 속도 초기화
        if (_controller.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = GroundedVelocity;
        }

        _verticalVelocity += Gravity * Time.deltaTime;

        Vector3 velocity = direction * speed + Vector3.up * _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }

    // 태블릿을 보는 중이면 달리지 못하고 천천히 걸음
    private float GetSpeed()
    {
        if (_codeTablet != null && _codeTablet.IsRaised) return _walkSpeed * _readingSpeedMultiplier;

        return _sprintAction.IsPressed() ? _runSpeed : _walkSpeed;
    }

    // Esc로 커서 해제, 클릭으로 다시 잠금
    private void UpdateCursorLock()
    {
        // (Esc는 이제 일시정지 메뉴가 맡아서 커서를 풀고 잠금)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetCursorLocked(true);
        }
    }

    private void SetCursorLocked(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }
}
