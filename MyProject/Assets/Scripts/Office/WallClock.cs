using UnityEngine;
using UnityEngine.UI;

// 벽시계(아날로그 바늘과 디지털 표시)
public class WallClock : MonoBehaviour
{
    private const float DegreesPerHour = 30f;
    private const float DegreesPerMinute = 6f;
    private const int MinutesPerHour = 60;
    private const int HoursPerDay = 24;
    private const int HoursPerDial = 12;

    // 시침 회전 축
    [SerializeField] private Transform _hourHand;

    // 분침 회전 축
    [SerializeField] private Transform _minuteHand;

    // 디지털 시각 글자
    [SerializeField] private Text _digitalText;

    // 시간이 흐를 때 1분이 지나는 실제 시간(초)
    [SerializeField] private float _secondsPerMinute = 2f;

    private int _hour;
    private int _minute;
    private float _elapsed;
    private bool _isRunning;

    private void Update() => AdvanceTime();

    // 시각 지정(흐름 정지)
    public void SetTime(int hour, int minute)
    {
        _isRunning = false;
        ApplyTime(hour, minute);
    }

    // 현재 시각부터 시간 흐름 시작
    public void StartRunning()
    {
        _isRunning = true;
        _elapsed = 0f;
    }

    private void AdvanceTime()
    {
        if (!_isRunning) return;

        _elapsed += Time.deltaTime;
        if (_elapsed < _secondsPerMinute) return;

        _elapsed -= _secondsPerMinute;
        ApplyTime(_hour, _minute + 1);
    }

    // 시각을 24시간 범위로 맞춘 뒤 바늘과 글자 갱신
    private void ApplyTime(int hour, int minute)
    {
        _hour = (hour + minute / MinutesPerHour) % HoursPerDay;
        _minute = minute % MinutesPerHour;

        float hourAngle = (_hour % HoursPerDial + _minute / (float)MinutesPerHour) * DegreesPerHour;

        // 정면에서 볼 때 시계 방향이 되도록 음수 회전
        _hourHand.localRotation = Quaternion.Euler(0f, 0f, -hourAngle);
        _minuteHand.localRotation = Quaternion.Euler(0f, 0f, -_minute * DegreesPerMinute);
        _digitalText.text = $"{_hour:00}:{_minute:00}";
    }
}
