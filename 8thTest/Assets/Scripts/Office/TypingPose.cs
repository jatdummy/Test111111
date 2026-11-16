using UnityEngine;

// 앉아서 타자 치는 동작 (재생 중인 애니메이션 위에 팔과 손가락 각도만 덮어씀)
// 팔은 노트북 위에 올려 둔 채 고정하고, 손가락만 번갈아 키를 누름
[RequireComponent(typeof(Animator))]
public class TypingPose : MonoBehaviour
{
    private const float FullCircle = Mathf.PI * 2f;

    // 손가락마다 누르는 박자를 조금씩 다르게 (같은 박자면 기계처럼 보임)
    private static readonly float[] FingerRhythms = { 1f, 0.83f, 1.17f, 0.91f };

    // 위팔을 앞으로 내미는 각도
    [SerializeField] private float _reachAngle = 60f;

    // 팔꿈치를 굽히는 각도
    [SerializeField] private float _elbowAngle = 16f;

    // 위팔을 몸 안쪽(키보드 쪽)으로 모으는 각도
    [SerializeField] private float _inwardAngle = 22f;

    // 손바닥이 키보드를 향하도록 아래팔을 비트는 각도
    [SerializeField] private float _palmDownAngle = 80f;

    // 손가락을 살짝 구부린 기본 각도
    [SerializeField] private float _fingerRestAngle = 24f;

    // 키를 누를 때 손가락이 더 구부러지는 각도
    [SerializeField] private float _fingerTapAngle = 10f;

    // 손가락 하나가 1초에 키를 누르는 횟수
    [SerializeField] private float _tapsPerSecond = 3f;

    private Transform[] _upperArms;
    private Transform[] _lowerArms;
    private Transform[] _hands;
    private Transform[][] _fingers;
    private float _time;

    private void Awake() => CacheBones();

    // 애니메이터가 자세를 정한 뒤에 덮어써야 해서 LateUpdate에서 처리
    private void LateUpdate()
    {
        _time += Time.deltaTime;
        ApplyArms();
        ApplyFingers();
    }

    private void CacheBones()
    {
        var animator = GetComponent<Animator>();
        _upperArms = new[] { animator.GetBoneTransform(HumanBodyBones.LeftUpperArm), animator.GetBoneTransform(HumanBodyBones.RightUpperArm) };
        _lowerArms = new[] { animator.GetBoneTransform(HumanBodyBones.LeftLowerArm), animator.GetBoneTransform(HumanBodyBones.RightLowerArm) };
        _hands = new[] { animator.GetBoneTransform(HumanBodyBones.LeftHand), animator.GetBoneTransform(HumanBodyBones.RightHand) };
        _fingers = new[]
        {
            new[]
            {
                animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal), animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal),
                animator.GetBoneTransform(HumanBodyBones.LeftRingProximal), animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal),
            },
            new[]
            {
                animator.GetBoneTransform(HumanBodyBones.RightIndexProximal), animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal),
                animator.GetBoneTransform(HumanBodyBones.RightRingProximal), animator.GetBoneTransform(HumanBodyBones.RightLittleProximal),
            },
        };
    }

    private void ApplyArms()
    {
        Vector3 side = transform.right;
        Vector3 forward = transform.forward;
        for (int i = 0; i < _upperArms.Length; i++)
        {
            // 왼팔(0)과 오른팔(1)은 안쪽 방향이 반대
            float inward = i == 0 ? _inwardAngle : -_inwardAngle;
            _upperArms[i].rotation = Quaternion.AngleAxis(inward, forward) * Quaternion.AngleAxis(-_reachAngle, side) * _upperArms[i].rotation;
            _lowerArms[i].rotation = Quaternion.AngleAxis(-_elbowAngle, side) * _lowerArms[i].rotation;

            // 아래팔 길이 방향을 축으로 비틀어 손바닥이 아래를 보게 함 (좌우 반대)
            Vector3 forearm = (_hands[i].position - _lowerArms[i].position).normalized;
            float twist = i == 0 ? -_palmDownAngle : _palmDownAngle;
            _lowerArms[i].rotation = Quaternion.AngleAxis(twist, forearm) * _lowerArms[i].rotation;
        }
    }

    // 손가락마다 다른 박자로 짧게 눌렀다 뗌
    private void ApplyFingers()
    {
        Vector3 side = transform.right;
        for (int hand = 0; hand < _fingers.Length; hand++)
        {
            for (int i = 0; i < _fingers[hand].Length; i++)
            {
                Transform finger = _fingers[hand][i];
                if (finger == null) continue;

                float phase = _time * _tapsPerSecond * FingerRhythms[i] * FullCircle + (hand * 4 + i) * 1.7f;

                // 사인값의 양수 부분을 4제곱해서 짧게 '톡' 누르는 느낌
                float press = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(phase)), 4f);
                float curl = _fingerRestAngle + press * _fingerTapAngle;
                finger.rotation = Quaternion.AngleAxis(curl, side) * finger.rotation;
            }
        }
    }
}
