using UnityEngine;

// 이상현상 하나: 대상 물건에 붙어서 켜기(Apply)와 되돌리기(Revert)를 짝으로 가짐
// 원래 상태는 각자 Awake에서 저장 (LoopManager는 고르고 부르기만 함)
public abstract class Anomaly : MonoBehaviour
{
    // 우클릭 메뉴로 확인할 때 쓰는 늦은 시간 (22:00 기준 경과 시간, 시간에 따라 모습이 바뀌는 이상현상용)
    private const int LateTestHour = 7;

    // 리포트 이름과 override 관계
    [SerializeField] private AnomalyData _data;

    public AnomalyData Data => _data;

    // hour : 22:00부터 지난 시간 (시간에 따라 모습이 바뀌는 이상현상용)
    public abstract void Apply(int hour);

    public abstract void Revert();

    // 개발용: Play 중 Inspector에서 컴포넌트를 우클릭해 바로 켜고 끄기 (루프를 돌며 나오길 기다리지 않게)
    // 원래 상태는 Awake에서 저장하므로 Play 중에만 동작
    [ContextMenu("이상현상 켜기 (22:00)")]
    private void ApplyEarlyForTest()
    {
        if (!Application.isPlaying) return;

        Apply(0);
    }

    [ContextMenu("이상현상 켜기 (05:00)")]
    private void ApplyLateForTest()
    {
        if (!Application.isPlaying) return;

        Apply(LateTestHour);
    }

    [ContextMenu("이상현상 되돌리기")]
    private void RevertForTest()
    {
        if (!Application.isPlaying) return;

        Revert();
    }
}
