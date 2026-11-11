// 게임 진행 단계
public enum GameState
{
    None = 0,
    Title = 1,
    Intro = 2,
    Playing = 3,
    Transition = 4,
    Clear = 5,
    Ending = 6,
}

// 사무실에 적용하는 변화 종류
public enum OfficeEffectType
{
    None = 0,
    LightsOff = 1,
    LightsRed = 2,
    ChairMissing = 3,
    ChairSunken = 4,
    MonitorOff = 5,
    MonitorTextureMissing = 6,
    SeniorTPose = 7,
    SeniorHeadTurned = 8,
    SeniorAbsent = 9,
    CoworkerTPose = 10,
    CoworkerStaring = 11,
    CoworkerClipping = 12,
    CupCount = 13,
    PlantMissing = 14,
    PosterFlipped = 15,
}

// 이상현상 유형
public enum AnomalyCategory
{
    None = 0,
    OriginalDeviation = 1,
    SpecMismatch = 2,
    CodingKnowledge = 3,
}
