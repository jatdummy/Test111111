using System;
using UnityEngine;

// 사무실 변화 하나(종류와 값)
[Serializable]
public struct OfficeEffect
{
    // 변화 종류
    [SerializeField] private OfficeEffectType _type;

    // 변화 값(종류에 따라 개수 또는 번호)
    [SerializeField] private int _value;

    public OfficeEffectType Type => _type;
    public int Value => _value;

    public OfficeEffect(OfficeEffectType type, int value)
    {
        _type = type;
        _value = value;
    }
}
