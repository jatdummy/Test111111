using UnityEngine;

// NPC 몸 동작 (Animator 파라미터로 걷기 · 앉아서 타자 · 서기를 바꾸고, 고개만 대상 쪽으로 돌림)
// 위치와 몸 방향은 부르는 쪽(ColleagueWalker, EndingDirector)이 정함, 꺼진 동안 받은 명령은 무시함 (다시 켜면 처음 상태)
[RequireComponent(typeof(Animator))]
public class NpcBody : MonoBehaviour
{
    // 고개를 돌릴 눈높이 (대상 발밑이 아니라 얼굴 쪽을 보게)
    private const float LookHeight = 1.6f;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int SitHash = Animator.StringToHash("Sit");
    private static readonly int TypeHash = Animator.StringToHash("Type");
    private static readonly int StandHash = Animator.StringToHash("Stand");

    // 타자 상태 이름 (AC_OfficeNpc의 상태 이름과 같아야 함)
    private static readonly int TypingStateHash = Animator.StringToHash("Typing");

    // 씬이 시작되자마자 자리에서 타자 (처음부터 앉아 있는 NPC)
    [SerializeField] private bool _startsTyping;

    // 고개를 돌리는 세기 (0이면 안 돌림)
    [SerializeField, Range(0f, 1f)] private float _lookWeight = 1f;

    // 고개를 돌릴 때 상체가 따라 도는 비율
    [SerializeField, Range(0f, 1f)] private float _lookBodyWeight = 0.2f;

    // 고개가 돌아가는 한계 (1이면 뒤까지 돌아감)
    [SerializeField, Range(0f, 1f)] private float _lookClamp = 0.6f;

    private Animator _animator;
    private Transform _lookTarget;

    // 손을 키보드에 올리고 타자 치는 중
    public bool IsTyping => _animator.isActiveAndEnabled && _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == TypingStateHash;

    private void Awake() => _animator = GetComponent<Animator>();

    private void Start()
    {
        if (_startsTyping)
        {
            StartTyping();
        }
    }

    // 고개 돌리기 (Animator 레이어의 IK Pass가 켜져 있어야 불림)
    private void OnAnimatorIK(int layerIndex)
    {
        if (_lookTarget == null)
        {
            _animator.SetLookAtWeight(0f);
            return;
        }

        _animator.SetLookAtWeight(_lookWeight, _lookBodyWeight, 1f, 1f, _lookClamp);
        _animator.SetLookAtPosition(_lookTarget.position + Vector3.up * LookHeight);
    }

    public void Walk(bool isWalking)
    {
        if (!_animator.isActiveAndEnabled) return;

        _animator.SetBool(IsWalkingHash, isWalking);
    }

    // 걷다가 의자에 앉아 손을 키보드에 올림 (이후 타자까지 자동으로 이어짐)
    // 걷기를 같은 프레임에 끄면 걷기 → 대기 전환이 먼저 걸려 앉지 못하므로 걷기 값은 그대로 둠
    public void SitAndType()
    {
        if (!_animator.isActiveAndEnabled) return;

        _animator.SetTrigger(SitHash);
    }

    // 앉는 과정 없이 바로 타자 (처음부터 앉아 있거나, 다시 밤이 되어 자리로 돌아갈 때)
    public void StartTyping()
    {
        if (!_animator.isActiveAndEnabled) return;

        ClearTriggers();
        _animator.SetBool(IsWalkingHash, false);
        _animator.SetTrigger(TypeHash);
    }

    // 어떤 상태에서든 선 자세로 (엔딩 출발, 루프 처음으로 되돌릴 때)
    public void StandUp()
    {
        if (!_animator.isActiveAndEnabled) return;

        ClearTriggers();
        _animator.SetBool(IsWalkingHash, false);
        _animator.SetTrigger(StandHash);
    }

    // 고개를 대상 쪽으로 (null이면 정면)
    public void LookAt(Transform target) => _lookTarget = target;

    // 처리되지 않고 남은 트리거가 다음 동작에 끼어들지 않게
    private void ClearTriggers()
    {
        _animator.ResetTrigger(SitHash);
        _animator.ResetTrigger(TypeHash);
        _animator.ResetTrigger(StandHash);
    }
}
