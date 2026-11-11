using System.Collections.Generic;
using UnityEngine;

// 사무실 문제 목록 데이터
[CreateAssetMenu(fileName = "OfficeCaseDatabase", menuName = "8번 코딩/Office Case Database")]
public class OfficeCaseDatabase : ScriptableObject
{
    // 출제할 문제 목록
    [SerializeField] private List<OfficeCase> _cases = new();

    public IReadOnlyList<OfficeCase> Cases => _cases;

    // 문제 목록 교체(씬 빌더에서 사용)
    public void SetCases(List<OfficeCase> cases) => _cases = cases;
}
