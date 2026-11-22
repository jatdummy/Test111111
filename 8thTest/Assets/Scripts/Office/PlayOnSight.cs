using UnityEngine;

// 켜진 뒤 플레이어가 가까이서 처음 바라보는 순간 소리를 한 번 냄 (섬뜩한 이상현상을 발견한 순간에 맞춰 소리가 차오르게)
[RequireComponent(typeof(AudioSource))]
public class PlayOnSight : MonoBehaviour
{
    // 바라보는지 확인할 플레이어 카메라
    [SerializeField] private Transform _viewer;

    // 이 거리 안에서 바라봐야 소리가 남
    [SerializeField, Min(0f)] private float _distance = 6f;

    // 시선 가운데에서 이 각도 안에 들어오면 바라본 것으로 봄
    [SerializeField, Range(0f, 90f)] private float _viewAngle = 25f;

    private AudioSource _audio;
    private bool _hasPlayed;

    private void Awake() => _audio = GetComponent<AudioSource>();

    // 이상현상이 다시 켜질 때마다 새로 한 번 (꺼지면 소리도 함께 멈춤)
    private void OnEnable() => _hasPlayed = false;

    private void Update()
    {
        if (_hasPlayed || !IsSeen()) return;

        _hasPlayed = true;
        _audio.Play();
    }

    private bool IsSeen()
    {
        Vector3 toThis = transform.position - _viewer.position;
        if (toThis.sqrMagnitude > _distance * _distance) return false;

        return Vector3.Angle(_viewer.forward, toThis) <= _viewAngle;
    }
}
