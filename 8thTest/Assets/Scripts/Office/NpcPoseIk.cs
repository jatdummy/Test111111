using UnityEngine;

// 이 캐릭터(Synty) 몸에 맞춘 자세 보정 (Mixamo 동작을 그대로 쓰면 어긋나는 곳을 IK로 맞춤)
// 고개(대상 · 폰 보기), 앉은 높이(엉덩이가 좌석에 묻히지 않게, 발은 바닥에), 손(키보드 위), 턱(입 다물기)
// OnAnimatorIK는 Animator 레이어의 IK Pass가 켜져 있어야 불림
[RequireComponent(typeof(Animator), typeof(NpcBody))]
public class NpcPoseIk : MonoBehaviour
{
    // 고개를 돌릴 눈높이 (대상 발밑이 아니라 얼굴 쪽을 보게)
    private const float LookHeight = 1.6f;

    // 폰을 볼 때 상체가 따라 숙여지는 비율 (고개만 꺾이면 목만 접혀 보이고, 크면 허리를 굽혀 보임)
    private const float PhoneLookBodyWeight = 0.15f;

    // 앉은 높이 보정을 켜고 끄는 빠르기 (초당 세기 변화)
    private const float SeatRaiseBlendSpeed = 3f;

    // 앉는 동작에서 높이 보정을 시작하는 지점 (엉덩이가 좌석 가까이 내려온 후반)
    private const float SeatRaiseStartTime = 0.5f;

    // 키보드에 손을 올리고 내리는 빠르기 (초당 세기 변화)
    private const float HandIkBlendSpeed = 2f;

    // 애니메이션 손 위치의 평균을 따라가는 빠르기 (타자 손놀림은 남기고 평균 자리만 키보드로 옮기기 위함)
    private const float HandAverageRate = 1f;

    // 키보드 쪽을 보고 있다고 칠 정면 각도 기준 (회전의자가 도는 중엔 손을 올리지 않게)
    private const float FacingKeyboardDot = 0.5f;

    // 팔꿈치를 힌트 쪽으로 따르는 정도와 힌트 위치 (손에서 몸 쪽 · 바깥 · 아래로, 팔꿈치가 몸통에 박히지 않게)
    private const float ElbowHintWeight = 0.5f;
    private const float ElbowHintBack = 0.3f;
    private const float ElbowHintOut = 0.2f;
    private const float ElbowHintDown = 0.15f;

    [Header("고개")]
    // 고개를 돌리는 세기 (0이면 안 돌림)
    [SerializeField, Range(0f, 1f)] private float _lookWeight = 1f;

    // 고개를 돌릴 때 상체가 따라 도는 비율
    [SerializeField, Range(0f, 1f)] private float _lookBodyWeight = 0.2f;

    // 고개가 돌아가는 한계 (1이면 뒤까지 돌아감)
    [SerializeField, Range(0f, 1f)] private float _lookClamp = 0.6f;

    // 폰을 내려다보는 세기 (걸으며 폰을 볼 때 고개를 숙이는 정도)
    [SerializeField, Range(0f, 1f)] private float _phoneLookWeight = 0.45f;

    // 폰을 보기 시작하고 그만둘 때 고개가 움직이는 빠르기 (초당 세기 변화)
    [SerializeField, Min(0f)] private float _phoneLookBlendSpeed = 3f;

    [Header("앉은 높이")]
    // 앉아 있는 동안 몸을 올리는 높이 (Mixamo 앉기 자세가 이 캐릭터엔 낮아 엉덩이가 좌석에 파묻히는 만큼)
    [SerializeField, Min(0f)] private float _seatRaise = 0.12f;

    [Header("손")]
    // 타자 칠 자리의 키보드 (손을 키보드 위로 맞춤, 없으면 애니메이션 손 위치 그대로)
    [SerializeField] private Transform _keyboard;

    // 키보드 중앙에서 좌우 손목까지 거리
    [SerializeField, Min(0f)] private float _handSpread = 0.12f;

    // 키보드 중앙에서 몸 쪽으로 손목까지 거리 (손가락이 자판 위에 오게)
    [SerializeField] private float _wristBack = 0.1f;

    // 키보드 바닥에서 손목까지 높이
    [SerializeField] private float _wristHeight = 0.02f;

    private Animator _animator;
    private NpcBody _body;
    private float _phoneLook;
    private float _seated;
    private float _handIk;
    private Vector3 _leftHandAverage;
    private Vector3 _rightHandAverage;

    // 턱 뼈와 입을 다문 원래 회전 (Mixamo 동작에는 턱 값이 없어서 그대로 두면 입이 벌어짐)
    private Transform _jaw;
    private Quaternion _jawClosedRotation;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _body = GetComponent<NpcBody>();

        // 애니메이션이 한 번도 적용되기 전의 턱 회전 = 모델의 입 다문 자세
        _jaw = _animator.isHuman ? _animator.GetBoneTransform(HumanBodyBones.Jaw) : null;
        if (_jaw != null)
        {
            _jawClosedRotation = _jaw.localRotation;
        }
    }

    private void OnEnable() => _body.SatInstantly += SnapSeated;

    private void OnDisable() => _body.SatInstantly -= SnapSeated;

    // Animator가 자세를 다 만든 뒤에 턱만 입 다문 회전으로 되돌림
    private void LateUpdate()
    {
        if (_jaw != null)
        {
            _jaw.localRotation = _jawClosedRotation;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // 상태는 프레임마다 한 번만 읽어 세 보정이 같이 씀
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

        UpdateSeatRaise(state);
        UpdateHandIk(state);
        UpdateLook();
    }

    // 앉는 과정 없이 바로 앉은 자세이므로 높이 보정도 바로 적용
    private void SnapSeated() => _seated = 1f;

    // 앉는 동작 후반 · 손 올리는 중 · 타자 중엔 몸을 올림 (발은 애니메이션 위치에 고정해 바닥에서 뜨지 않게)
    private void UpdateSeatRaise(AnimatorStateInfo state)
    {
        _seated = Mathf.MoveTowards(_seated, SeatedAmount(state), SeatRaiseBlendSpeed * Time.deltaTime);

        float footWeight = _seated > 0f ? 1f : 0f;
        _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, footWeight);
        _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, footWeight);
        _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, footWeight);
        _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, footWeight);
        if (_seated <= 0f) return;

        // 몸을 올리기 전의 발 위치 · 방향을 그대로 목표로 둠
        _animator.SetIKPosition(AvatarIKGoal.LeftFoot, _animator.GetIKPosition(AvatarIKGoal.LeftFoot));
        _animator.SetIKPosition(AvatarIKGoal.RightFoot, _animator.GetIKPosition(AvatarIKGoal.RightFoot));
        _animator.SetIKRotation(AvatarIKGoal.LeftFoot, _animator.GetIKRotation(AvatarIKGoal.LeftFoot));
        _animator.SetIKRotation(AvatarIKGoal.RightFoot, _animator.GetIKRotation(AvatarIKGoal.RightFoot));

        _animator.bodyPosition += Vector3.up * (_seatRaise * _seated);
    }

    // 지금 얼마나 앉아 있는지 (0 = 서 있음, 1 = 다 앉음)
    private static float SeatedAmount(AnimatorStateInfo state)
    {
        if (NpcAnimatorStates.IsSeated(state.shortNameHash)) return 1f;
        if (state.shortNameHash != NpcAnimatorStates.StandToSitState) return 0f;

        return Mathf.InverseLerp(SeatRaiseStartTime, 1f, state.normalizedTime);
    }

    // 손 올리는 중 · 타자 중이고 키보드 쪽을 보고 있으면 손을 키보드 위로 옮김 (서서히 켜고 끔)
    private void UpdateHandIk(AnimatorStateInfo state)
    {
        bool isAtKeyboard = _keyboard != null && NpcAnimatorStates.IsSeated(state.shortNameHash) && IsFacingKeyboard();

        // 손을 처음 올릴 때 평균 위치를 지금 애니메이션 손 위치로 시작
        if (isAtKeyboard && _handIk <= 0f)
        {
            _leftHandAverage = _animator.GetIKPosition(AvatarIKGoal.LeftHand);
            _rightHandAverage = _animator.GetIKPosition(AvatarIKGoal.RightHand);
        }

        _handIk = Mathf.MoveTowards(_handIk, isAtKeyboard ? 1f : 0f, HandIkBlendSpeed * Time.deltaTime);
        if (_handIk <= 0f)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            _animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
            _animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
            return;
        }

        PlaceHand(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, -1f, ref _leftHandAverage);
        PlaceHand(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, 1f, ref _rightHandAverage);
    }

    // 애니메이션 손 위치에 "평균 손 위치 → 키보드 위 목표" 차이만 더함 (손을 톡톡 움직이는 타자 동작은 그대로)
    private void PlaceHand(AvatarIKGoal hand, AvatarIKHint elbow, float side, ref Vector3 average)
    {
        Vector3 animated = _animator.GetIKPosition(hand);
        average = Vector3.Lerp(average, animated, HandAverageRate * Time.deltaTime);

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 home = _keyboard.position - forward * _wristBack + right * (side * _handSpread) + Vector3.up * _wristHeight;

        _animator.SetIKPositionWeight(hand, _handIk);
        _animator.SetIKPosition(hand, animated + (home - average));

        _animator.SetIKHintPositionWeight(elbow, _handIk * ElbowHintWeight);
        _animator.SetIKHintPosition(elbow, home - forward * ElbowHintBack + right * (side * ElbowHintOut) + Vector3.down * ElbowHintDown);
    }

    private bool IsFacingKeyboard()
    {
        Vector3 toKeyboard = _keyboard.position - transform.position;
        toKeyboard.y = 0f;
        return Vector3.Dot(transform.forward, toKeyboard.normalized) > FacingKeyboardDot;
    }

    // 대상이 있으면 대상을 보고, 없으면 걷는 중(폰이 보일 때)엔 폰 쪽으로 서서히 고개를 숙이고 아니면 서서히 원래대로
    private void UpdateLook()
    {
        _phoneLook = Mathf.MoveTowards(_phoneLook, _body.IsOnPhone ? 1f : 0f, _phoneLookBlendSpeed * Time.deltaTime);

        Transform lookTarget = _body.LookTarget;
        if (lookTarget != null)
        {
            _animator.SetLookAtWeight(_lookWeight, _lookBodyWeight, 1f, 1f, _lookClamp);
            _animator.SetLookAtPosition(lookTarget.position + Vector3.up * LookHeight);
            return;
        }

        Transform phone = _body.HandPhone;
        if (phone == null || _phoneLook <= 0f)
        {
            _animator.SetLookAtWeight(0f);
            return;
        }

        _animator.SetLookAtWeight(_phoneLookWeight * _phoneLook, PhoneLookBodyWeight, 1f, 1f, _lookClamp);
        _animator.SetLookAtPosition(phone.position);
    }
}
