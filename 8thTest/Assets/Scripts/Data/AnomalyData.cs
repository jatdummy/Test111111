using UnityEngine;

// 이상현상 하나의 정보 (리포트 이름, 코드 override와의 관계)
[CreateAssetMenu(fileName = "AnomalyData", menuName = "8번 테스터/Anomaly Data")]
public class AnomalyData : ScriptableObject
{
    // 판정을 틀렸을 때 버그 리포트에 나오는 이름
    [SerializeField] private string _reportLabel;

    // 함께 나오면 안 되는 override (미끼 override와 핫픽스 모두, 이상현상 대상이 사라지거나 똑같아 보이는 경우)
    [SerializeField] private OverrideKind[] _blockedOverrides;

    // 이 이상현상이 성립하려면 코드에 반드시 적혀 있어야 하는 조명 (None이면 상관없음)
    [SerializeField] private LightOverride _requiredLight;

    // 핫픽스로 이 override가 코드에 추가되면 정상이 되는지 (책장 문 열림 → BookcaseOpen)
    [SerializeField] private bool _canBeLegitimized;
    [SerializeField] private OverrideKind _legitimizeOverride;

    public string ReportLabel => _reportLabel;
    public LightOverride RequiredLight => _requiredLight;
    public bool CanBeLegitimized => _canBeLegitimized;
    public OverrideKind LegitimizeOverride => _legitimizeOverride;

    public bool IsBlocked(OverrideKind kind) => System.Array.IndexOf(_blockedOverrides, kind) >= 0;
}
