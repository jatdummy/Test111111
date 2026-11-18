using UnityEngine;

// 문짝을 옆으로 밀어 천천히 여닫는 문 (엘리베이터, 자동문 공용)
public class SlidingDoor : MonoBehaviour
{
    // 움직일 문짝들
    [SerializeField] private Transform[] _leaves;

    // 문짝마다 다 열렸을 때 옮겨 갈 거리 (부모 기준, 벽 속으로 숨는 방향)
    [SerializeField] private Vector3[] _openOffsets;

    // 다 열리는 데 걸리는 시간(초)
    [SerializeField, Min(0f)] private float _openSeconds = 1.2f;

    // 다 닫히는 데 걸리는 시간(초)
    [SerializeField, Min(0f)] private float _closeSeconds = 1.5f;

    private Vector3[] _closedPositions;

    // 0이면 다 닫힘, 1이면 다 열림
    private float _progress;
    private bool _wantsOpen;

    public bool IsClosed => _progress <= 0f;
    public bool IsOpen => _progress >= 1f;

    private void Awake()
    {
        _closedPositions = new Vector3[_leaves.Length];
        for (int i = 0; i < _leaves.Length; i++)
        {
            _closedPositions[i] = _leaves[i].localPosition;
        }
    }

    private void Update() => Slide();

    public void Open() => _wantsOpen = true;

    public void Close() => _wantsOpen = false;

    private void Slide()
    {
        float target = _wantsOpen ? 1f : 0f;
        if (_progress == target) return;

        float seconds = _wantsOpen ? _openSeconds : _closeSeconds;
        _progress = Mathf.MoveTowards(_progress, target, Time.deltaTime / seconds);

        // 출발과 멈춤을 부드럽게 (처음과 끝이 느림)
        float eased = Mathf.SmoothStep(0f, 1f, _progress);
        for (int i = 0; i < _leaves.Length; i++)
        {
            _leaves[i].localPosition = _closedPositions[i] + _openOffsets[i] * eased;
        }
    }
}
