// 근무 시간 규칙 (22:00 출근, 8시간 연속 정답이면 06:00 퇴근)
public static class LoopTime
{
    public const int ClearCount = 8;
    public const int StartHour = 22;
    public const int HoursPerDay = 24;

    // hoursPassed : 22:00부터 지난 시간 → "23:00" 같은 시각 글자
    public static string Format(int hoursPassed) => $"{(StartHour + hoursPassed) % HoursPerDay:00}:00";
}
