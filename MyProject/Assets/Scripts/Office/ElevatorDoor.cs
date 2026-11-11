using UnityEngine;

// 양쪽으로 열리는 엘리베이터 문
public class ElevatorDoor : MonoBehaviour
{
    // 왼쪽 문짝
    [SerializeField] private Transform _leftPanel;

    // 오른쪽 문짝
    [SerializeField] private Transform _rightPanel;

    // 완전히 열렸을 때 문짝이 옆으로 이동한 거리
    [SerializeField] private float _openDistance = 0.95f;

    // 문이 움직이는 속도(초당 거리)
    [SerializeField] private float _moveSpeed = 0.7f;

    private Vector3 _leftClosedPosition;
    private Vector3 _rightClosedPosition;
    private float _currentDistance;
    private float _targetDistance;

    public bool IsMoving => !Mathf.Approximately(_currentDistance, _targetDistance);

    private void Awake() => CacheClosedPositions();

    private void Update() => MovePanels();

    public void Open() => _targetDistance = _openDistance;

    public void Close() => _targetDistance = 0f;

    // 연출 없이 바로 열거나 닫기
    public void SetOpenImmediate(bool isOpen)
    {
        _targetDistance = isOpen ? _openDistance : 0f;
        _currentDistance = _targetDistance;
        ApplyPanelPositions();
    }

    private void CacheClosedPositions()
    {
        _leftClosedPosition = _leftPanel.localPosition;
        _rightClosedPosition = _rightPanel.localPosition;
    }

    private void MovePanels()
    {
        if (!IsMoving) return;

        _currentDistance = Mathf.MoveTowards(_currentDistance, _targetDistance, _moveSpeed * Time.deltaTime);
        ApplyPanelPositions();
    }

    private void ApplyPanelPositions()
    {
        _leftPanel.localPosition = _leftClosedPosition + Vector3.left * _currentDistance;
        _rightPanel.localPosition = _rightClosedPosition + Vector3.right * _currentDistance;
    }
}
