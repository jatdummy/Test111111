// override 종류별 글자 (버그 리포트에 쓰는 이름, 모니터 코드에 쓰는 한 줄)
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
                return "override LightColor => Red;";
            case OverrideKind.BlueLight:
                return "override LightColor => Blue;";
            default:
                return "override BookcaseOpen => true;";
        }
    }
}
