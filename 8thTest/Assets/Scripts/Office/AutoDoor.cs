using UnityEngine;

// 가까이 가면 열리는 자동문
public class AutoDoor : MonoBehaviour
{
    // 문을 열 대상 (플레이어)
    [SerializeField] private Transform _player;

    // 닫혔을 때 길을 막는 문짝
    [SerializeField] private GameObject _doorPanel;

    // 문이 열리는 거리 (0이면 항상 닫힘)
    [SerializeField, Min(0f)] private float _openDistance = 2.5f;

    // 문짝을 밀어 여는 장치 (비워 두면 문짝을 바로 숨기고 보이는 방식)
    [SerializeField] private SlidingDoor _slidingDoor;

    // 한 번 지나가면 다시 못 들어가는 문인지 (계단 → 사무실 출구)
    [SerializeField] private bool _isOneWay;

    // 나가는 방향 (월드 기준, 이쪽으로 다 지나가면 잠김)
    [SerializeField] private Vector3 _exitDirection = Vector3.forward;

    // 문에서 이만큼 지나가면 잠김 (문 안쪽으로 돌아오면 다시 풀림)
    [SerializeField, Min(0f)] private float _lockDistance = 0.8f;

    private bool _isLocked;

    // 열리는 거리가 0인 문은 항상 닫혀 있으므로 한 번 닫아 두고 매 프레임 거리를 재지 않음
    private void Start()
    {
        if (_openDistance > 0f) return;

        UpdateDoor();
        enabled = false;
    }

    private void Update() => UpdateDoor();

    private void UpdateDoor()
    {
        UpdateLock();

        float distance = Vector3.Distance(_player.position, transform.position);
        bool isOpen = !_isLocked && distance <= _openDistance;
        if (_slidingDoor != null)
        {
            if (isOpen)
            {
                _slidingDoor.Open();
            }
            else
            {
                _slidingDoor.Close();
            }

            return;
        }

        _doorPanel.SetActive(!isOpen);
    }

    // 출구 쪽으로 다 지나가면 잠그고, 다음 루프에 계단 안쪽으로 다시 오면 풂
    private void UpdateLock()
    {
        if (!_isOneWay) return;

        float side = Vector3.Dot(_player.position - transform.position, _exitDirection.normalized);
        if (side > _lockDistance)
        {
            _isLocked = true;
        }
        else if (side < 0f)
        {
            _isLocked = false;
        }
    }
}
