using UnityEngine;

// 걸을 때 카메라가 위아래 · 좌우로 살짝 흔들리는 효과 (헤드밥)
// 다른 효과(폭발 흔들림 등)와 겹쳐도 되도록 이번 프레임에 바뀐 만큼만 더함
public class HeadBob : MonoBehaviour
{
    // 흔들 카메라 축
    [SerializeField] private Transform _cameraPivot;

    // 움직임 속도를 읽을 플레이어
    [SerializeField] private CharacterController _controller;

    // 걸을 때 위아래로 흔들리는 높이
    [SerializeField, Min(0f)] private float _verticalAmount = 0.015f;

    // 걸을 때 좌우로 흔들리는 폭
    [SerializeField, Min(0f)] private float _horizontalAmount = 0.008f;

    // 이동 거리 1m당 흔들림 횟수 (걸음 박자)
    [SerializeField, Min(0f)] private float _stepsPerMeter = 0.42f;

    // 멈추면 원래 높이로 돌아오는 빠르기
    [SerializeField, Min(0f)] private float _returnSpeed = 6f;

    private Vector3 _appliedOffset;
    private float _phase;
    private float _strength;

    private void LateUpdate() => Bob();

    // 꺼지면 더해 둔 흔들림을 빼서 원래 자리로
    private void OnDisable()
    {
        _cameraPivot.localPosition -= _appliedOffset;
        _appliedOffset = Vector3.zero;
        _strength = 0f;
    }

    private void Bob()
    {
        Vector3 velocity = _controller.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        // 걷는 동안만 세지고, 멈추면 부드럽게 잦아듦
        bool isMoving = _controller.isGrounded && speed > 0.1f;
        _strength = Mathf.MoveTowards(_strength, isMoving ? 1f : 0f, _returnSpeed * Time.deltaTime);
        _phase += speed * _stepsPerMeter * Mathf.PI * 2f * Time.deltaTime;

        // 위아래는 한 걸음에 두 번, 좌우는 한 번 (발을 번갈아 딛는 느낌)
        var offset = new Vector3(Mathf.Sin(_phase) * _horizontalAmount, Mathf.Abs(Mathf.Sin(_phase)) * _verticalAmount, 0f) * _strength;
        _cameraPivot.localPosition += offset - _appliedOffset;
        _appliedOffset = offset;
    }
}
