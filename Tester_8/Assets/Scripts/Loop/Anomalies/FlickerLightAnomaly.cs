using UnityEngine;

// 형광등 하나가 불규칙하게 깜빡임 (색은 코드대로, 켜졌다 꺼졌다만 함)
public class FlickerLightAnomaly : Anomaly
{
    // 천장 형광등
    [SerializeField] private OfficeLights _lights;

    // 깜빡일 형광등 번호 (0부터, 조명 명세 불일치 이상현상과 다른 번호)
    [SerializeField, Min(0)] private int _lightIndex = 4;

    // 꺼져 있는 시간 범위(초)
    [SerializeField] private Vector2 _offSeconds = new(0.04f, 0.18f);

    // 켜져 있는 시간 범위(초)
    [SerializeField] private Vector2 _onSeconds = new(0.08f, 1.4f);

    private bool _isFlickering;
    private bool _isLit = true;
    private float _timer;

    private void Update()
    {
        if (!_isFlickering) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _isLit = !_isLit;
        _lights.SetLit(_lightIndex, _isLit);
        Vector2 range = _isLit ? _onSeconds : _offSeconds;
        _timer = Random.Range(range.x, range.y);
    }

    public override void Apply(int hour)
    {
        _isFlickering = true;
        _isLit = true;
        _timer = Random.Range(_onSeconds.x, _onSeconds.y);
    }

    public override void Revert()
    {
        _isFlickering = false;
        _isLit = true;
        _lights.SetLit(_lightIndex, true);
    }
}
