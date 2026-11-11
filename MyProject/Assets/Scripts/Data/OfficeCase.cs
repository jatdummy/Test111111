using System;
using System.Collections.Generic;
using UnityEngine;

// 사무실 한 판의 문제(화면에 표시할 코드와 실제로 적용할 변화)
[Serializable]
public class OfficeCase
{
    // 클래스 본문에 표시할 코드
    [TextArea(2, 8)] [SerializeField] private string _code;

    // 실제 사무실에 적용할 변화 목록
    [SerializeField] private List<OfficeEffect> _effects = new();

    // 사무실이 "원본 + 코드에 적힌 변경"과 다른지 여부
    [SerializeField] private bool _hasBug;

    // 버그 유형
    [SerializeField] private AnomalyCategory _category;

    // 버그 리포트에 표시할 설명
    [TextArea(1, 4)] [SerializeField] private string _explanation;

    public string Code => _code;
    public IReadOnlyList<OfficeEffect> Effects => _effects;
    public bool HasBug => _hasBug;
    public AnomalyCategory Category => _category;
    public string Explanation => _explanation;

    public OfficeCase(string code, bool hasBug, AnomalyCategory category, string explanation, params OfficeEffect[] effects)
    {
        _code = code;
        _hasBug = hasBug;
        _category = category;
        _explanation = explanation;
        _effects = new List<OfficeEffect>(effects);
    }
}
