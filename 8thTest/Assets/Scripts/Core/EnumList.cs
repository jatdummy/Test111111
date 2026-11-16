// 루프 구간 트리거 종류
public enum LoopTriggerType
{
    None = 0,
    CorridorEnter = 1,
    Forward = 2,
    Back = 3,
}

// 이상현상 종류
public enum AnomalyType
{
    None = 0,
    ExtinguisherOnCeiling = 1,
    LightMismatch = 2,
    MissingTexturePoster = 3,
    ColleagueStare = 4,
    EyesInDoor = 5,
    BloodWater = 6,
    WiltedPlant = 7,
    CabinetDoorOpen = 8,
    TrashOverflow = 9,
    WhiteboardMessage = 10,
}

// 코드에 적히는 조명 색 변경
public enum LightOverride
{
    None = 0,
    Red = 1,
    Blue = 2,
}
