using UnityEngine;

// 가까이 가면 열리는 자동문
public class AutoDoor : MonoBehaviour
{
    // 문을 열 대상 (플레이어)
    [SerializeField] private Transform _player;

    // 닫혔을 때 길을 막는 문짝
    [SerializeField] private GameObject _doorPanel;

    // 문이 열리는 거리 (0이면 항상 닫힘)
    [SerializeField] private float _openDistance = 2.5f;

    private void Update() => UpdateDoor();

    private void UpdateDoor()
    {
        float distance = Vector3.Distance(_player.position, transform.position);
        _doorPanel.SetActive(distance > _openDistance);
    }
}
