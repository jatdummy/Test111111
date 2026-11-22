using System;
using UnityEngine;
using UnityEngine.Serialization;

// NPC 몸 동작 (Animator 파라미터로 걷기 · 앉아서 타자 · 서기를 바꾸고, 걸을 때는 폰을 듦)
// 위치와 몸 방향은 부르는 쪽(ColleagueWalker, EndingDirector)이 정함, 꺼진 동안 받은 명령은 무시함 (다시 켜면 처음 상태)
// 이 캐릭터 몸에 맞춘 자세 보정(고개 · 손 · 앉은 높이 · 턱)은 NpcPoseIk가 맡음
[RequireComponent(typeof(Animator))]
public class NpcBody : MonoBehaviour
{
    // 씬이 시작되자마자 자리에서 타자 (처음부터 앉아 있는 NPC)
    [SerializeField, FormerlySerializedAs("_startsTyping")] private bool _isTypingOnStart;

    // 걸을 때만 손에 드는 휴대폰 (폰 보며 걷기 동작용, 없으면 비워 둠)
    [SerializeField] private GameObject _handPhone;

    private Animator _animator;

    // 고개를 돌릴 대상 (null이면 정면)
    public Transform LookTarget { get; private set; }

    // 손에 드는 폰 (보이는지와 상관없이, 없으면 null)
    public Transform HandPhone => _handPhone != null ? _handPhone.transform : null;

    // 폰을 들고 걷는 중
    public bool IsOnPhone => _handPhone != null && _handPhone.activeSelf;

    // 손을 키보드에 올리고 타자 치는 중
    public bool IsTyping => _animator.isActiveAndEnabled && CurrentState == NpcAnimatorStates.TypingState;

    // 의자에 다 앉음 (손을 올리는 중이거나 타자 중)
    public bool IsSeated => _animator.isActiveAndEnabled && NpcAnimatorStates.IsSeated(CurrentState);

    private int CurrentState => _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;

    // 앉는 과정 없이 바로 앉은 자세가 됨 (자세 보정이 서서히가 아니라 바로 맞추게)
    public event Action OnSatInstantly;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        ShowPhone(false);
    }

    private void Start()
    {
        if (_isTypingOnStart)
        {
            StartTyping();
        }
    }

    // 앉는 동작이 엉덩이를 뒤로 옮기는 만큼만 실제 위치를 옮김 (다 앉은 뒤 몸이 앞으로 튀지 않게)
    // 걷기 · 타자 중의 위치는 부르는 쪽이 정하므로 그대로 둠
    private void OnAnimatorMove()
    {
        bool isSitting = CurrentState == NpcAnimatorStates.StandToSitState
            || (_animator.IsInTransition(0) && _animator.GetNextAnimatorStateInfo(0).shortNameHash == NpcAnimatorStates.StandToSitState);
        if (!isSitting) return;

        Vector3 delta = _animator.deltaPosition;
        delta.y = 0f;
        transform.position += delta;
    }

    public void Walk(bool isWalking)
    {
        if (!_animator.isActiveAndEnabled) return;

        _animator.SetBool(NpcAnimatorStates.IsWalking, isWalking);
        ShowPhone(isWalking);
    }

    // 걷다가 의자에 앉아 손을 키보드에 올림 (이후 타자까지 자동으로 이어짐)
    // 걷기를 같은 프레임에 끄면 걷기 → 대기 전환이 먼저 걸려 앉지 못하므로 걷기 값은 그대로 둠
    public void SitAndType()
    {
        if (!_animator.isActiveAndEnabled) return;

        _animator.SetTrigger(NpcAnimatorStates.Sit);
        ShowPhone(false);
    }

    // 앉는 과정 없이 바로 타자 (처음부터 앉아 있거나, 다시 밤이 되어 자리로 돌아갈 때)
    public void StartTyping()
    {
        if (!_animator.isActiveAndEnabled) return;

        ClearTriggers();
        _animator.SetBool(NpcAnimatorStates.IsWalking, false);
        _animator.SetTrigger(NpcAnimatorStates.Type);
        ShowPhone(false);
        OnSatInstantly?.Invoke();
    }

    // 어떤 상태에서든 선 자세로 (엔딩 출발, 루프 처음으로 되돌릴 때)
    public void StandUp()
    {
        if (!_animator.isActiveAndEnabled) return;

        ClearTriggers();
        _animator.SetBool(NpcAnimatorStates.IsWalking, false);
        _animator.SetTrigger(NpcAnimatorStates.Stand);
        ShowPhone(false);
    }

    // 고개를 대상 쪽으로 (null이면 정면)
    public void LookAt(Transform target) => LookTarget = target;

    // 앉거나 멈추면 폰을 넣음 (앉는 동작 중에 손에 폰이 남아 있지 않게)
    private void ShowPhone(bool isShown)
    {
        if (_handPhone != null)
        {
            _handPhone.SetActive(isShown);
        }
    }

    // 처리되지 않고 남은 트리거가 다음 동작에 끼어들지 않게
    private void ClearTriggers()
    {
        _animator.ResetTrigger(NpcAnimatorStates.Sit);
        _animator.ResetTrigger(NpcAnimatorStates.Type);
        _animator.ResetTrigger(NpcAnimatorStates.Stand);
    }
}
