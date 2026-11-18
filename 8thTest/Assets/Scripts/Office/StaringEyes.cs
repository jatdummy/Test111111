using UnityEngine;

// 어둠 속 눈: 플레이어를 따라 보고 가끔 깜빡임
public class StaringEyes : MonoBehaviour
{
    // 감았을 때 세로 크기 배율
    private const float ClosedScale = 0.08f;

    // 바라볼 대상 (플레이어 카메라)
    [SerializeField] private Transform _target;

    // 눈알 (흰자, 홍채, 눈동자를 묶은 부모)
    [SerializeField] private Transform[] _eyeballs;

    // 깜빡임 간격 범위(초)
    [SerializeField] private Vector2 _blinkInterval = new(2.5f, 6f);

    // 감고 있는 시간(초)
    [SerializeField] private float _blinkSeconds = 0.12f;

    private Vector3[] _openScales;
    private float _nextBlinkTime;

    private void Awake() => CacheScales();

    private void OnEnable() => ScheduleBlink();

    private void LateUpdate()
    {
        bool isClosed = Time.time >= _nextBlinkTime;
        if (Time.time >= _nextBlinkTime + _blinkSeconds)
        {
            ScheduleBlink();
        }

        for (int i = 0; i < _eyeballs.Length; i++)
        {
            _eyeballs[i].LookAt(_target);
            _eyeballs[i].localScale = isClosed ? Vector3.Scale(_openScales[i], new Vector3(1f, ClosedScale, 1f)) : _openScales[i];
        }
    }

    private void CacheScales()
    {
        _openScales = new Vector3[_eyeballs.Length];
        for (int i = 0; i < _eyeballs.Length; i++)
        {
            _openScales[i] = _eyeballs[i].localScale;
        }
    }

    private void ScheduleBlink() => _nextBlinkTime = Time.time + Random.Range(_blinkInterval.x, _blinkInterval.y);
}
