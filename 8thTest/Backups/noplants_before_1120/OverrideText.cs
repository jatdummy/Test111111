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
            case OverrideKind.NoColleague:
                return "동료 없음";
            case OverrideKind.NoPlants:
                return "화분 없음";
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
            case OverrideKind.NoColleague:
                return "override NpcCount => 0;";
            case OverrideKind.NoPlants:
                return "override PlantCount => 0;";
            default:
                return "override BookcaseOpen => true;";
        }
    }
}
