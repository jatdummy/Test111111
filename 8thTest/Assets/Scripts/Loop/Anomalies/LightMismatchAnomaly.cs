using UnityEngine;

// 코드에는 Blue 조명인데 형광등 하나만 원래 색으로 남아 있음
public class LightMismatchAnomaly : Anomaly
{
    // 천장 형광등
    [SerializeField] private OfficeLights _lights;

    // 원래 색으로 남길 형광등 번호 (0부터)
    [SerializeField, Min(0)] private int _lightIndex = 2;

    // 남겨 둘 원래 색
    [SerializeField] private Color _originalColor = Color.white;

    public override void Apply(int hour) => _lights.Set(_lightIndex, _originalColor);

    // 조명 색은 매 루프 LoopManager가 코드대로 다시 칠하므로 되돌릴 것이 없음
    public override void Revert()
    {
    }
}
