// 한 루프의 빌드 명세
public struct OfficeBuild
{
    public bool IsOriginal { get; set; }
    public LightOverride LightOverride { get; set; }
    public bool IsCabinetOpen { get; set; }
    public Anomaly Anomaly { get; set; }

    // 코드에 적혀 있어 정상인 이상현상 모습 (미끼, 사무실도 같은 모습이 됨)
    public Anomaly CodedAnomaly { get; set; }

    // 근무 중 배포된 긴급 핫픽스 (사무실은 그대로, 코드만 바뀜)
    public HotfixKind Hotfix { get; set; }
    public OverrideKind HotfixTarget { get; set; }
    public bool IsHotfixApplied { get; set; }

    // 사무실이 코드와 다른지 (핫픽스가 있으면 바뀐 코드 기준)
    public bool HasBug
    {
        get
        {
            switch (Hotfix)
            {
                case HotfixKind.Legitimize:
                    return false;
                case HotfixKind.AddLine:
                case HotfixKind.RemoveLine:
                    // 사무실에도 반영됐으면 핫픽스 줄은 맞고, 원래 이상현상만 따짐
                    return !IsHotfixApplied || Anomaly != null;
                default:
                    return Anomaly != null;
            }
        }
    }

    // 원본 확인 구간(검수 시작 전)의 빌드
    public static OfficeBuild CreateOriginal() => new() { IsOriginal = true };

    // 모니터, 로그에 나오는 빌드 번호 (원본은 0)
    public int GetNumber(int hoursPassed) => IsOriginal ? 0 : hoursPassed + 1;
}
