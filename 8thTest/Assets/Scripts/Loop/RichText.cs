// 모니터, 검수 로그에 쓰는 리치 텍스트 색
public static class RichText
{
    public const string WaitingColor = "#F2C14E";
    public const string PassColor = "#5FD07A";
    public const string FailColor = "#FF6B6B";
    public const string CommentColor = "#6A9955";
    public const string AddedColor = "#5FD07A";
    public const string HotfixColor = "#FFA94D";

    public static string Colorize(string text, string color) => $"<color={color}>{text}</color>";
}
