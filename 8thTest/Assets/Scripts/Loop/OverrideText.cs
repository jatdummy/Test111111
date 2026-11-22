// override 종류별 글자 (버그 리포트에 쓰는 이름, 모니터 코드에 쓰는 한 줄)
// 코드 한 줄은 '대상.속성 = 값;' 대입문 (ApplyBuild 메서드 안에서 사무실 값을 바꾸는 모양)
public static class OverrideText
{
    public static string GetLabel(OverrideKind kind)
    {
        switch (kind)
        {
            case OverrideKind.RedLight:
                return "조명 Red";
            case OverrideKind.BlueLight:
                return "조명 Blue";
            default:
                return "책장 열림";
        }
    }

    public static string GetCode(OverrideKind kind)
    {
        switch (kind)
        {
            case OverrideKind.RedLight:
                return "Lights.Color = Color.red;";
            case OverrideKind.BlueLight:
                return "Lights.Color = Color.blue;";
            default:
                return "Bookcase.IsOpen = true;";
        }
    }
}
