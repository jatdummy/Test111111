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
    [SerializeField, Min(0f)] private float _walkSpeed = 2.2f;

    // 달리기 속도(초당 거리)
    [SerializeField, Min(0f)] private float _runSpeed = 3.6f;

    // 태블릿을 보는 동안 걷기 속도 배율 (0이면 멈춰서 봄, 1이면 감속 없음)
    [SerializeField, Range(0f, 1f)] private float _readingSpeedMultiplier = 0.5f;

    // 코드 태블릿 (보는 동안 달리기 불가, 감속)
    [SerializeField] private CodeTablet _codeTablet;

    [Header("시점")]
    // 마우스 감도(픽셀당 회전 각도)
    [SerializeField, Min(0f)] private float _lookSensitivity = 0.1f;

    // 위아래로 회전하는 카메라 축
    [SerializeField] private Transform _cameraPivot;

    [Header("입력")]
    // 이동 (Player/Move)
    [SerializeField] private InputActionReference _moveAction;

    // 시점 (Player/Look)
    [SerializeField] private InputActionReference _lookAction;

    // 달리기 (Player/Sprint)
    [SerializeField] private InputActionReference _sprintAction;

    // 커서를 다시 잠그는 클릭 (UI/Click)
    [SerializeField] private InputActionReference _clickAction;

    private CharacterController _controller;
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

    // 지정한 위치로 순간이동하며 좌우 방향을 yawDelta만큼 돌림 (반대로 놓인 계단실 사이를 이어 붙일 때)
    public void TeleportTo(Vector3 position, float yawDelta)
    {
        _controller.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDelta, 0f) * transform.rotation);
        _controller.enabled = true;
    }

    private void CacheComponents()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Look()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = _lookAction.action.ReadValue<Vector2>() * _lookSensitivity;
        transform.Rotate(0f, delta.x, 0f);
        _pitch = Mathf.Clamp(_pitch - delta.y, -MaxPitch, MaxPitch);
        _cameraPivot.localEulerAngles = new Vector3(_pitch, 0f, 0f);
    }

    private void Move()
    {
        // 순간이동이나 연출 중 컨트롤러가 꺼져 있으면 이동하지 않음 (꺼진 컨트롤러의 Move는 에러)
        if (!_controller.enabled) return;

        Vector2 input = _moveAction.action.ReadValue<Vector2>();
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

        return _sprintAction.action.IsPressed() ? _runSpeed : _walkSpeed;
    }

    // 클릭으로 커서를 다시 잠금
    private void UpdateCursorLock()
    {
        // (Esc는 이제 일시정지 메뉴가 맡아서 커서를 풀고 잠금)
        if (_clickAction.action.WasPressedThisFrame())
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
