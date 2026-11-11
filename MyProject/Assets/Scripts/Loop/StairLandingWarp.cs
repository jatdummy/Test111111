using UnityEngine;

// 계단참 A 진입 시 계단참 B의 같은 상대 위치로 플레이어 이동
public class StairLandingWarp : MonoBehaviour
{
    [SerializeField] private FirstPersonController _player;

    [Header("기준점")]
    // 출발 계단참 기준점
    [SerializeField] private Transform _sourceAnchor;

    // 도착 계단참 기준점
    [SerializeField] private Transform _destinationAnchor;

    private int _lastWarpFrame;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        // 같은 프레임 중복 이동 방지
        if (_lastWarpFrame == Time.frameCount) return;

        _lastWarpFrame = Time.frameCount;
        _player.Warp(_destinationAnchor.position - _sourceAnchor.position);
    }
}
