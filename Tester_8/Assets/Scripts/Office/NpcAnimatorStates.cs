using UnityEngine;

// AC_OfficeNpc의 파라미터 · 상태 이름 (컨트롤러의 이름과 같아야 함, NpcBody와 NpcPoseIk가 같이 씀)
public static class NpcAnimatorStates
{
    public static readonly int IsWalking = Animator.StringToHash("IsWalking");
    public static readonly int Sit = Animator.StringToHash("Sit");
    public static readonly int Type = Animator.StringToHash("Type");
    public static readonly int Stand = Animator.StringToHash("Stand");

    public static readonly int StandToSitState = Animator.StringToHash("StandToSit");
    public static readonly int SitToTypeState = Animator.StringToHash("SitToType");
    public static readonly int TypingState = Animator.StringToHash("Typing");

    // 의자에 다 앉음 (손을 올리는 중이거나 타자 중)
    public static bool IsSeated(int stateHash) => stateHash == SitToTypeState || stateHash == TypingState;
}
