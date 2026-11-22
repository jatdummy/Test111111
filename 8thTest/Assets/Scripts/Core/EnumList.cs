// 루프 구간 트리거 종류
public enum LoopTriggerType
{
    None = 0,
    CorridorEnter = 1,
    Forward = 2,
    Back = 3,
    Rollback = 4,
    Hotfix = 5,      // 자판기를 지나면 예약된 긴급 핫픽스 배포
}

// 코드에 적히는 조명 색 변경
public enum LightOverride
{
    None = 0,
    Red = 1,
    Blue = 2,
}

// 근무 중 긴급 핫픽스 종류 (사무실은 그대로 두고 코드만 바꿈)
public enum HotfixKind
{
    None = 0,
    AddLine = 1,     // 사무실에 없는 변경을 코드에 추가 → 버그
    RemoveLine = 2,  // 사무실에 있는 변경을 코드에서 지움 → 버그
    Legitimize = 3,  // 이상현상을 코드에 반영 → 정상
}

// override 종류 번호 (이상현상 데이터에 숫자로 저장되므로 번호를 바꾸지 않음, 2번은 지운 '동료 없음' 자리)
public enum OverrideKind
{
    RedLight = 0,
    BlueLight = 1,
    Bookcase = 3,
}

// 태블릿 페이지 (번호가 휠로 넘기는 순서)
public enum TabletPage
{
    Code = 0,
    Report = 1,
    Rules = 2,
}
