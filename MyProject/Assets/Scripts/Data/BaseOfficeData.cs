using System;
using UnityEngine;

// 원본 사무실 구성 데이터
[CreateAssetMenu(fileName = "BaseOffice", menuName = "8번 코딩/Base Office Data")]
public class BaseOfficeData : ScriptableObject
{
    // 책상 칸 배치 정보
    [Serializable]
    public struct DeskSlot
    {
        // 사무실 기준 배치 위치
        [SerializeField] private Vector3 _position;

        // Y축 회전 각도
        [SerializeField] private float _yRotation;

        public Vector3 Position => _position;
        public float YRotation => _yRotation;
    }

    // 책상 칸 프리팹
    [SerializeField] private GameObject _deskUnitPrefab;

    // 책상 칸 배치 목록
    [SerializeField] private DeskSlot[] _deskSlots;

    // 책상 칸을 뺀 소품·조명·포스터 묶음 프리팹
    [SerializeField] private OfficeProps _officePropsPrefab;

    // 사무실 통로 길이(m)
    [SerializeField] private float _officeLength = 20f;

    public GameObject DeskUnitPrefab => _deskUnitPrefab;
    public OfficeProps OfficePropsPrefab => _officePropsPrefab;
    public DeskSlot[] DeskSlots => _deskSlots;
    public float OfficeLength => _officeLength;
}
