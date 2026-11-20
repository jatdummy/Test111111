using UnityEngine;

// 사무실을 빌드대로 맞춤: 매 루프 원본으로 되돌린 뒤 조명 색, 이상현상, 소품 override를 적용
public class OfficeState : MonoBehaviour
{
    [Header("사무실 오브젝트")]
    // 천장 형광등 (코드의 조명 색 override)
    [SerializeField] private OfficeLights _lights;

    // 걸어오는 동료
    [SerializeField] private ColleagueWalker _colleague;

    // 책장 (override BookcaseOpen, 핫픽스)
    [SerializeField] private Cabinet _cabinet;

    [Header("이상현상")]
    // 나올 수 있는 이상현상 (각 물건에 붙은 컴포넌트, 루프마다 하나를 골라 켬)
    [SerializeField] private Anomaly[] _anomalies;

    [Header("조명 색")]
    // 원본 조명 색
    [SerializeField] private Color _defaultLightColor = Color.white;

    // override Red 조명 색
    [SerializeField] private Color _redLightColor = new(1f, 0.25f, 0.2f);

    // override Blue 조명 색
    [SerializeField] private Color _blueLightColor = new(0.35f, 0.5f, 1f);

    public Anomaly[] Anomalies => _anomalies;

    // 매 루프 원본으로 되돌린 뒤 이번 빌드를 적용
    public void Apply(OfficeBuild build, int hoursPassed)
    {
        ResetOffice();
        _lights.SetAll(GetLightColor(build.LightOverride));
        if (build.Anomaly != null)
        {
            build.Anomaly.Apply(hoursPassed);
        }

        _colleague.gameObject.SetActive(!build.HasNoColleague);
        ApplyPropOverrides(build);
    }

    // 핫픽스 내용을 사무실에 바로 반영 (조명 색, 동료, 책장)
    public void ApplyHotfix(OfficeBuild build)
    {
        bool isAdded = build.Hotfix == HotfixKind.AddLine;
        switch (build.HotfixTarget)
        {
            case OverrideKind.RedLight:
            case OverrideKind.BlueLight:
                LightOverride lightOverride = !isAdded ? LightOverride.None : build.HotfixTarget == OverrideKind.RedLight ? LightOverride.Red : LightOverride.Blue;
                _lights.SetAll(GetLightColor(lightOverride));
                break;
            case OverrideKind.NoColleague:
                _colleague.gameObject.SetActive(!isAdded);
                break;
            default:
                if (isAdded)
                {
                    _cabinet.Open();
                }
                else
                {
                    _cabinet.Close();
                }

                break;
        }
    }

    // 통로에 들어오면 동료가 걷기 시작 (동료 없음 빌드면 그대로)
    public void BeginColleagueWalk(Transform player)
    {
        if (_colleague.gameObject.activeSelf)
        {
            _colleague.BeginWalk(player);
        }
    }

    private void ResetOffice()
    {
        // 지난 루프의 이상현상을 모두 되돌림 (각자 원래 상태를 기억함)
        foreach (Anomaly anomaly in _anomalies)
        {
            anomaly.Revert();
        }

        _colleague.gameObject.SetActive(true);
        _colleague.ResetWalk(false);

        _cabinet.Close();
    }

    // 코드에 적힌 소품 override 적용 (책장 열림)
    private void ApplyPropOverrides(OfficeBuild build)
    {
        if (build.IsCabinetOpen)
        {
            _cabinet.Open();
        }
    }

    private Color GetLightColor(LightOverride lightOverride)
    {
        switch (lightOverride)
        {
            case LightOverride.Red:
                return _redLightColor;
            case LightOverride.Blue:
                return _blueLightColor;
            default:
                return _defaultLightColor;
        }
    }
}
