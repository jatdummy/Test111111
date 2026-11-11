using UnityEngine;
using UnityEngine.InputSystem;

// 1인칭 이동과 시점 회전
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    private const float MaxPitch = 80f;

    [Header("입력")]
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _lookAction;

    [Header("시점")]
    // 상하 회전을 적용할 카메라 기준점
    [SerializeField] private Transform _cameraPivot;

    // 마우스 이동량 대비 회전 각도
    [SerializeField] private float _lookSensitivity = 0.1f;

    [Header("이동")]
    // 걷기 속도(초당 거리)
    [SerializeField] private float _moveSpeed = 3f;

    // 중력 가속도(초당 속도 변화)
    [SerializeField] private float _gravity = -9.81f;

    // 접지 중 바닥에 붙여 두는 하강 속도
    [SerializeField] private float _groundedFallSpeed = -2f;

    private CharacterController _characterController;
    private float _pitch;
    private float _verticalSpeed;

    private void Awake() => _characterController = GetComponent<CharacterController>();

    private void Start() => Cursor.lockState = CursorLockMode.Locked;

    private void Update()
    {
        Look();
        Move();
    }

    // 지정 위치·방향으로 즉시 이동
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        _characterController.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        _pitch = 0f;
        _verticalSpeed = 0f;
        _cameraPivot.localRotation = Quaternion.identity;
        _characterController.enabled = true;
    }

    // 회전·시점·낙하 속도를 유지한 채 지정 거리만큼의 위치 이동
    public void Warp(Vector3 offset)
    {
        _characterController.enabled = false;
        transform.position += offset;
        _characterController.enabled = true;
    }

    private void Look()
    {
        Vector2 lookInput = _lookAction.action.ReadValue<Vector2>() * _lookSensitivity;
        transform.Rotate(0f, lookInput.x, 0f);
        _pitch = Mathf.Clamp(_pitch - lookInput.y, -MaxPitch, MaxPitch);
        _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 moveInput = _moveAction.action.ReadValue<Vector2>();
        Vector3 horizontal = (transform.right * moveInput.x + transform.forward * moveInput.y) * _moveSpeed;

        if (_characterController.isGrounded && _verticalSpeed < 0f)
            _verticalSpeed = _groundedFallSpeed;

        _verticalSpeed += _gravity * Time.deltaTime;
        Vector3 velocity = horizontal + Vector3.up * _verticalSpeed;
        _characterController.Move(velocity * Time.deltaTime);
    }
}
