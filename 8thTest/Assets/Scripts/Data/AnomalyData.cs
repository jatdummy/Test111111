using UnityEngine;

// 이상현상 하나의 정보 (리포트 이름, 코드 override와의 관계, 나오는 시간, 코드로 적을 수 있는 줄)
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

    // 이 시간부터 나옴 (22:00 기준 경과 시간, 섬뜩한 이상현상은 밤이 깊어진 뒤에 나오게)
    [SerializeField, Min(0)] private int _minHour;

    // 코드에 이 줄이 적혀 있으면 같은 모습이 정상 (비워 두면 코드로 적을 수 없는 이상현상)
    // 다른 빌드에서 미끼로 코드에 적히고 사무실도 같은 모습이 되어, 코드를 읽어야 버그인지 알 수 있음
    [SerializeField] private string _codeLine;

    public string ReportLabel => _reportLabel;
    public int MinHour => _minHour;
    public string CodeLine => _codeLine;
    public bool HasCodeLine => !string.IsNullOrEmpty(_codeLine);
    public LightOverride RequiredLight => _requiredLight;
    public bool CanBeLegitimized => _canBeLegitimized;
    public OverrideKind LegitimizeOverride => _legitimizeOverride;

    public bool IsBlocked(OverrideKind kind) => System.Array.IndexOf(_blockedOverrides, kind) >= 0;
}
