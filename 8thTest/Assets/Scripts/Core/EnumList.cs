// 루프 구간 트리거 종류
public enum LoopTriggerType
{
    None = 0,
    CorridorEnter = 1,
    Forward = 2,
    Back = 3,
    Rollback = 4,
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

// override 종류 번호 (AddRandomOverride의 순서와 같음)
public enum OverrideKind
{
    RedLight = 0,
    BlueLight = 1,
    NoColleague = 2,
    NoPlants = 3,
    Bookcase = 4,
}

// 태블릿 페이지 (번호가 휠로 넘기는 순서)
public enum TabletPage
{
    Code = 0,
    Report = 1,
    Rules = 2,
}
