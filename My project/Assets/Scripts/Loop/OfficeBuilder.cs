using UnityEngine;

// 원본 데이터 기반 변하는 오브젝트 재생성
public class OfficeBuilder : MonoBehaviour
{
    // 매 루프 재생성되는 오브젝트의 부모
    [SerializeField] private Transform _runtimeRoot;

    // 기존 오브젝트 제거 후 책상 칸과 소품 묶음 생성
    public void Build(BaseOfficeData data)
    {
        ClearRuntimeObjects();

        foreach (BaseOfficeData.DeskSlot slot in data.DeskSlots)
        {
            GameObject desk = Instantiate(data.DeskUnitPrefab, _runtimeRoot);
            desk.transform.SetLocalPositionAndRotation(slot.Position, Quaternion.Euler(0f, slot.YRotation, 0f));
        }

        // 프리팹 루트의 원점·무회전 상태 그대로 배치
        Instantiate(data.OfficePropsPrefab, _runtimeRoot);
    }

    private void ClearRuntimeObjects()
    {
        for (int i = _runtimeRoot.childCount - 1; i >= 0; i--)
            Destroy(_runtimeRoot.GetChild(i).gameObject);
    }
}
