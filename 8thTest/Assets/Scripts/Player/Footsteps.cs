using UnityEngine;

// 걸음마다 발소리 (계단실 바닥에서는 계단 발소리)
// 헤드밥과 같은 걸음 박자로, 헤드밥이 가장 낮게 내려가는 순간에 발을 디딤 (설정에서 헤드밥을 꺼도 발소리는 남)
[RequireComponent(typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
    // 이 속도보다 느리면 멈춘 것으로 봄 (헤드밥과 같은 기준)
    private const float MinMoveSpeed = 0.1f;

    // 몸 아래 끝에서 더 내려가 바닥을 찾는 거리
    private const float GroundProbe = 0.3f;

    // 바닥에서 잠깐 떠도 디딘 것으로 보는 시간(초, 계단을 내려갈 때 칸마다 잠깐 뜸)
    private const float GroundedGraceSeconds = 0.2f;

    // 움직임 속도와 발 위치를 읽을 플레이어
    [SerializeField] private CharacterController _controller;

    // 계단실 묶음 (이 아래 바닥을 밟으면 계단 발소리)
    [SerializeField] private Transform _stairwells;

    // 평소 발소리 (사무실 · 엘리베이터)
    [SerializeField] private AudioClip _floorStep;

    // 계단을 오르내리는 발소리
    [SerializeField] private AudioClip _stairStep;

    // 평소 발소리 크기
    [SerializeField, Range(0f, 1f)] private float _floorVolume = 0.35f;

    // 계단 발소리 크기
    [SerializeField, Range(0f, 1f)] private float _stairVolume = 0.45f;

    // 이동 거리 1m당 흔들림 횟수 (헤드밥과 같은 값, 한 번 흔들릴 때 두 걸음)
    [SerializeField, Min(0.01f)] private float _stepsPerMeter = 0.42f;

    // 같은 소리가 기계처럼 반복되지 않게 걸음마다 바꾸는 높낮이 · 크기 폭
    [SerializeField, Range(0f, 0.3f)] private float _pitchJitter = 0.06f;
    [SerializeField, Range(0f, 0.5f)] private float _volumeJitter = 0.15f;

    private AudioSource _audio;
    private float _distance;
    private float _lastGroundedTime = float.NegativeInfinity;

    // 공중에서 걸음 박자가 와서 착지할 때 내기로 미뤄 둔 걸음
    private bool _isStepPending;

    private void Awake() => _audio = GetComponent<AudioSource>();

    private void Update()
    {
        Vector3 velocity = _controller.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        if (_controller.isGrounded)
        {
            _lastGroundedTime = Time.time;
        }

        // 걸은 거리가 반 박자를 채울 때마다 한 걸음
        float stepLength = 0.5f / _stepsPerMeter;
        _distance += speed * Time.deltaTime;
        if (_distance >= stepLength)
        {
            _distance -= stepLength;
            _isStepPending |= speed > MinMoveSpeed;
        }

        // 계단을 내려가며 잠깐 떠 있는 순간에 걸음이 오면 건너뛰지 않고 착지할 때 냄
        bool isGrounded = Time.time - _lastGroundedTime <= GroundedGraceSeconds;
        if (_isStepPending && isGrounded)
        {
            _isStepPending = false;
            PlayStep();
        }
    }

    private void PlayStep()
    {
        bool isOnStairs = IsOnStairs();
        float volume = (isOnStairs ? _stairVolume : _floorVolume) * (1f - Random.Range(0f, _volumeJitter));
        _audio.pitch = 1f + Random.Range(-_pitchJitter, _pitchJitter);
        _audio.PlayOneShot(isOnStairs ? _stairStep : _floorStep, volume);
    }

    // 발밑 바닥이 계단실에 속하는지
    private bool IsOnStairs()
    {
        Vector3 center = _controller.transform.TransformPoint(_controller.center);
        float length = _controller.height * 0.5f + GroundProbe;
        if (!Physics.Raycast(center, Vector3.down, out RaycastHit hit, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;

        return hit.collider.transform.IsChildOf(_stairwells);
    }
}
