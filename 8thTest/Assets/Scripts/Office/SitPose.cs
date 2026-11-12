using UnityEngine;

// 휴머노이드 캐릭터를 의자에 앉은 자세로 만들기 (재생 중인 애니메이션 위에 다리 각도만 덮어씀)
[RequireComponent(typeof(Animator))]
public class SitPose : MonoBehaviour
{
    // 허벅지를 앞으로 드는 각도
    [SerializeField] private float _hipAngle = 90f;

    // 무릎을 굽히는 각도
    [SerializeField] private float _kneeAngle = 90f;

    private Transform[] _upperLegs;
    private Transform[] _lowerLegs;

    private void Awake() => CacheBones();

    // 애니메이터가 자세를 정한 뒤에 덮어써야 해서 LateUpdate에서 처리
    private void LateUpdate() => ApplyPose();

    private void CacheBones()
    {
        var animator = GetComponent<Animator>();
        _upperLegs = new[] { animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg), animator.GetBoneTransform(HumanBodyBones.RightUpperLeg) };
        _lowerLegs = new[] { animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg), animator.GetBoneTransform(HumanBodyBones.RightLowerLeg) };
    }

    private void ApplyPose()
    {
        Vector3 axis = transform.right;
        foreach (Transform upperLeg in _upperLegs)
        {
            upperLeg.rotation = Quaternion.AngleAxis(-_hipAngle, axis) * upperLeg.rotation;
        }

        foreach (Transform lowerLeg in _lowerLegs)
        {
            lowerLeg.rotation = Quaternion.AngleAxis(_kneeAngle, axis) * lowerLeg.rotation;
        }
    }
}
