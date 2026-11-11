using System.Collections.Generic;

// 빌드 모니터와 사무실이 함께 따르는 이번 빌드 명세
public class LoopSpec
{
    // 현재 카운트 단계
    public int StageIndex { get; }

    // 원본에 덧붙이는 override 코드 줄
    public IReadOnlyList<string> OverrideLines { get; }

    public bool HasAnomaly { get; }

    public LoopSpec(int stageIndex, IReadOnlyList<string> overrideLines, bool hasAnomaly)
    {
        StageIndex = stageIndex;
        OverrideLines = overrideLines;
        HasAnomaly = hasAnomaly;
    }
}
