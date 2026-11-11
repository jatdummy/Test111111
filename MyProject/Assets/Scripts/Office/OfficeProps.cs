using UnityEngine;

// 소품 묶음의 오브젝트 참조 모음
public class OfficeProps : MonoBehaviour
{
    [Header("조명")]
    [SerializeField] private Light[] _ceilingLights;

    [Header("벽면")]
    // A, B, C 순서의 포스터
    [SerializeField] private Transform[] _posters;
    [SerializeField] private Transform _kanbanBoard;
    [SerializeField] private Transform _whiteboard;
    [SerializeField] private Transform _clock;
    [SerializeField] private Transform _exitSign;

    [Header("바닥")]
    [SerializeField] private Transform _extinguisher;

    public Light[] CeilingLights => _ceilingLights;
    public Transform[] Posters => _posters;
    public Transform KanbanBoard => _kanbanBoard;
    public Transform Whiteboard => _whiteboard;
    public Transform Clock => _clock;
    public Transform ExitSign => _exitSign;
    public Transform Extinguisher => _extinguisher;
}
