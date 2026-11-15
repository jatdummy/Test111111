using UnityEngine;

// 걷기 애니메이션이 없어서 코드로 만든 걷는 동작 (재생 중인 대기 애니메이션 위에 다리, 팔, 몸 높이를 덮어씀)
[RequireComponent(typeof(Animator))]
public class WalkPose : MonoBehaviour
{
    private const float FullCircle = Mathf.PI * 2f;

    // 1m 걸을 때 다리가 왕복하는 횟수 (보폭)
    [SerializeField] private float _cyclesPerMeter = 0.8f;

    // 걷는 속도(초당 거리), 걷는 쪽에서 넣어줌
    [SerializeField] private float _speed = 1.1f;

    // 허벅지가 앞뒤로 흔들리는 각도
    [SerializeField] private float _legSwingAngle = 25f;

    // 다리를 들 때 무릎이 굽는 최대 각도
    [SerializeField] private float _kneeBendAngle = 35f;

    // 팔이 앞뒤로 흔들리는 각도 (다리와 반대로)
    [SerializeField] private float _armSwingAngle = 18f;

    // 한 걸음마다 몸이 오르내리는 높이
    [SerializeField] private float _bobHeight = 0.025f;

    private Transform _hips;
    private Transform[] _upperLegs;
    private Transform[] _lowerLegs;
    private Transform[] _upperArms;
    private float _phase;

    public float Speed
    {
        set => _speed = value;
    }

    private void Awake() => CacheBones();

    // 켤 때마다 같은 발부터 시작
    private void OnEnable() => _phase = 0f;

    // 애니메이터가 자세를 정한 뒤에 덮어써야 해서 LateUpdate에서 처리
    private void LateUpdate()
    {
        _phase = (_phase + _speed * _cyclesPerMeter * FullCircle * Time.deltaTime) % FullCircle;
        ApplyPose();
    }

    private void CacheBones()
    {
        var animator = GetComponent<Animator>();
        _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        _upperLegs = new[] { animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg), animator.GetBoneTransform(HumanBodyBones.RightUpperLeg) };
        _lowerLegs = new[] { animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg), animator.GetBoneTransform(HumanBodyBones.RightLowerLeg) };
        _upperArms = new[] { animator.GetBoneTransform(HumanBodyBones.LeftUpperArm), animator.GetBoneTransform(HumanBodyBones.RightUpperArm) };
    }

    private void ApplyPose()
    {
        Vector3 axis = transform.right;

        // 왼발과 오른발은 반 바퀴 차이
        for (int i = 0; i < _upperLegs.Length; i++)
        {
            float legPhase = _phase + i * Mathf.PI;
            float swing = Mathf.Sin(legPhase) * _legSwingAngle;

            // 다리가 뒤에서 앞으로 넘어오는 동안만 무릎을 굽혀 발을 들어 올림
            float knee = Mathf.Max(0f, Mathf.Cos(legPhase)) * _kneeBendAngle;

            _upperLegs[i].rotation = Quaternion.AngleAxis(-swing, axis) * _upperLegs[i].rotation;
            _lowerLegs[i].rotation = Quaternion.AngleAxis(knee, axis) * _lowerLegs[i].rotation;
        }

        // 팔은 같은 쪽 다리와 반대로 흔듦
        for (int i = 0; i < _upperArms.Length; i++)
        {
            float armSwing = Mathf.Sin(_phase + i * Mathf.PI) * _armSwingAngle;
            _upperArms[i].rotation = Quaternion.AngleAxis(armSwing, axis) * _upperArms[i].rotation;
        }

        // 발이 땅에 닿는 순간(한 바퀴에 두 번) 가장 낮아짐
        _hips.position += Vector3.up * (Mathf.Abs(Mathf.Cos(_phase)) * _bobHeight);
    }
}
