using System.Text;
using UnityEngine;

// 루프 진행, 빌드 출제, 판정 관리
public class LoopManager : MonoBehaviour
{
    private const int ClearCount = 8;
    private const int StartHour = 22;
    private const int HoursPerDay = 24;
    private const int AnomalyCount = 4;
    private const int MaxNormalStreak = 2;
    private const int MaxBugStreak = 4;
    private const int MismatchLightIndex = 2;
    private const float NormalBuildChance = 1f / 3f;
    private const float OverrideChance = 0.5f;
    private const float EmissionIntensity = 2f;

    [Header("루프")]
    // 앞으로 통과했을 때 플레이어를 옮길 거리
    [SerializeField] private Vector3 _forwardTeleportOffset = new(-27.2f, -3.2f, -5.7f);

    // 플레이어 (모니터 켜짐 거리 계산용)
    [SerializeField] private Transform _player;

    // 모니터가 켜지는 거리 (멀리서는 코드가 바뀌는 순간이 보이지 않게)
    [SerializeField] private float _monitorWakeDistance = 2.5f;

    [Header("화면")]
    // 코드를 보여 줄 모니터 글자 (시작 구역, 끝 구역)
    [SerializeField] private TextMesh[] _monitorTexts;

    // 통로 벽시계 글자
    [SerializeField] private TextMesh _clockText;

    [Header("사무실 오브젝트")]
    // 천장 형광등 빛
    [SerializeField] private Light[] _ceilingLights;

    // 천장 형광등 몸체
    [SerializeField] private Renderer[] _ceilingLightRenderers;

    // 소화기
    [SerializeField] private Transform _extinguisher;

    // 소화기가 천장에 붙을 때의 높이
    [SerializeField] private float _extinguisherCeilingHeight = 2.7f;

    // 텍스처 누락 이상현상이 나올 포스터
    [SerializeField] private Renderer _bugPoster;

    // 텍스처 누락 표시용 재질
    [SerializeField] private Material _missingTextureMaterial;

    // 걸어오는 동료
    [SerializeField] private ColleagueWalker _colleague;

    [Header("조명 색")]
    // 원본 조명 색
    [SerializeField] private Color _defaultLightColor = Color.white;

    // override Red 조명 색
    [SerializeField] private Color _redLightColor = new(1f, 0.25f, 0.2f);

    // override Blue 조명 색
    [SerializeField] private Color _blueLightColor = new(0.35f, 0.5f, 1f);

    private OfficeBuild _currentBuild;
    private AnomalyType _lastAnomaly;
    private int _count;
    private int _normalStreak;
    private int _bugStreak;
    private bool _isArmed;
    private bool _isCleared;
    private string _report;
    private Vector3 _extinguisherPosition;
    private Quaternion _extinguisherRotation;
    private Material[] _posterMaterials;

    private void Start()
    {
        CacheOriginals();
        ApplyBuild(CreateOriginalBuild());
    }

    private void Update() => UpdateMonitorWake();

    // 트리거 통과 처리 (통로 진입, 앞으로, 뒤로)
    public void HandleTrigger(LoopTriggerType type, FirstPersonController player)
    {
        switch (type)
        {
            case LoopTriggerType.CorridorEnter:
                EnterCorridor(player);
                break;
            case LoopTriggerType.Forward:
                if (!_isArmed) return;
                Judge(true);
                player.Teleport(_forwardTeleportOffset);
                break;
            case LoopTriggerType.Back:
                if (!_isArmed) return;
                Judge(false);
                break;
        }
    }

    private void CacheOriginals()
    {
        _extinguisherPosition = _extinguisher.position;
        _extinguisherRotation = _extinguisher.rotation;
        _posterMaterials = _bugPoster.sharedMaterials;
    }

    // 통로에 들어오면 판정 대기 상태로 전환
    private void EnterCorridor(FirstPersonController player)
    {
        if (_isArmed) return;

        _isArmed = true;
        if (_colleague.gameObject.activeSelf) _colleague.BeginWalk(player.transform);
    }

    private void Judge(bool isForward)
    {
        _isArmed = false;

        // 클리어 뒤 퇴근하려 해도 다시 밤 10시
        if (_isCleared)
        {
            _isCleared = false;
            _count = 0;
            _report = null;
            ApplyBuild(CreateOriginalBuild());
            return;
        }

        // 원본 확인 구간은 판정 없이 검수 시작
        if (_currentBuild.IsOriginal)
        {
            _report = null;
            ApplyBuild(isForward ? CreateJudgedBuild() : CreateOriginalBuild());
            return;
        }

        bool hasBug = _currentBuild.Anomaly != AnomalyType.None;
        bool isCorrect = isForward != hasBug;
        _report = CreateReport(hasBug, isCorrect);

        if (!isCorrect)
        {
            _count = 0;
            ApplyBuild(CreateOriginalBuild());
            return;
        }

        _count++;
        if (_count >= ClearCount)
        {
            _isCleared = true;
            ApplyBuild(CreateOriginalBuild());
            return;
        }

        ApplyBuild(CreateJudgedBuild());
    }

    private string CreateReport(bool hasBug, bool isCorrect)
    {
        string label = GetAnomalyLabel(_currentBuild.Anomaly);
        if (isCorrect) return hasBug ? $"FIXED  {label}" : "MERGED  build matched spec";

        return hasBug ? $"MISSED BUG  {label}" : "NO BUG  build matched spec";
    }

    private string GetAnomalyLabel(AnomalyType anomaly)
    {
        switch (anomaly)
        {
            case AnomalyType.ExtinguisherOnCeiling:
                return "Extinguisher: position";
            case AnomalyType.LightMismatch:
                return $"Light[{MismatchLightIndex}]: not Blue";
            case AnomalyType.MissingTexturePoster:
                return "Poster B: missing texture";
            case AnomalyType.ColleagueStare:
                return "Colleague: lookAt Player";
            default:
                return string.Empty;
        }
    }

    private OfficeBuild CreateOriginalBuild() => new OfficeBuild { IsOriginal = true };

    private OfficeBuild CreateJudgedBuild()
    {
        var build = new OfficeBuild();
        if (ShouldBeNormal())
        {
            _normalStreak++;
            _bugStreak = 0;
            if (Random.value < OverrideChance) AddRandomOverride(ref build);
            return build;
        }

        _bugStreak++;
        _normalStreak = 0;
        build.Anomaly = PickAnomaly();

        // 조명 명세 불일치는 코드에 Blue가 적혀 있어야 성립
        if (build.Anomaly == AnomalyType.LightMismatch)
        {
            build.LightOverride = LightOverride.Blue;
        }
        else if (Random.value < OverrideChance)
        {
            AddRandomOverride(ref build);
        }

        return build;
    }

    private bool ShouldBeNormal()
    {
        if (_normalStreak >= MaxNormalStreak) return false;
        if (_bugStreak >= MaxBugStreak) return true;

        return Random.value < NormalBuildChance;
    }

    // 직전과 같은 이상현상은 연속으로 내지 않음
    private AnomalyType PickAnomaly()
    {
        AnomalyType anomaly;
        do
        {
            anomaly = (AnomalyType)Random.Range(1, AnomalyCount + 1);
        }
        while (anomaly == _lastAnomaly);

        _lastAnomaly = anomaly;
        return anomaly;
    }

    // 미끼 override 하나 추가 (동료 이상현상과 동료 없음은 함께 내지 않음)
    private void AddRandomOverride(ref OfficeBuild build)
    {
        bool canHideColleague = build.Anomaly != AnomalyType.ColleagueStare;
        int pick = Random.Range(0, canHideColleague ? 3 : 2);
        switch (pick)
        {
            case 0:
                build.LightOverride = LightOverride.Red;
                break;
            case 1:
                build.LightOverride = LightOverride.Blue;
                break;
            default:
                build.HasNoColleague = true;
                break;
        }
    }

    // 매 루프 원본으로 되돌린 뒤 이번 빌드를 적용
    private void ApplyBuild(OfficeBuild build)
    {
        _currentBuild = build;
        ResetOffice();
        ApplyLightColor(build);
        ApplyAnomaly(build.Anomaly);
        _colleague.gameObject.SetActive(!build.HasNoColleague);
        UpdateTexts();
    }

    private void ResetOffice()
    {
        _extinguisher.SetPositionAndRotation(_extinguisherPosition, _extinguisherRotation);
        _bugPoster.sharedMaterials = _posterMaterials;
        _colleague.gameObject.SetActive(true);
        _colleague.ResetWalk(false);
    }

    private void ApplyLightColor(OfficeBuild build)
    {
        Color color = GetOverrideColor(build.LightOverride);
        for (int i = 0; i < _ceilingLights.Length; i++)
        {
            // 명세 불일치면 한 개만 원래 색으로 남김
            bool isMismatch = build.Anomaly == AnomalyType.LightMismatch && i == MismatchLightIndex;
            SetLightColor(i, isMismatch ? _defaultLightColor : color);
        }
    }

    private Color GetOverrideColor(LightOverride lightOverride)
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

    // 빛과 함께 형광등의 발광 재질만 색 변경 (몸체 재질은 그대로)
    private void SetLightColor(int index, Color color)
    {
        _ceilingLights[index].color = color;
        foreach (Material material in _ceilingLightRenderers[index].materials)
        {
            if (!material.IsKeywordEnabled("_EMISSION")) continue;

            material.SetColor("_BaseColor", color);
            material.SetColor("_EmissionColor", color * EmissionIntensity);
        }
    }

    // 텍스처 누락처럼 모든 재질을 분홍색으로
    private void SetMissingTexture(Renderer target)
    {
        var materials = new Material[target.sharedMaterials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = _missingTextureMaterial;
        }

        target.sharedMaterials = materials;
    }

    private void ApplyAnomaly(AnomalyType anomaly)
    {
        switch (anomaly)
        {
            case AnomalyType.ExtinguisherOnCeiling:
                Vector3 ceilingPosition = new(_extinguisherPosition.x, _extinguisherCeilingHeight, _extinguisherPosition.z);
                _extinguisher.SetPositionAndRotation(ceilingPosition, _extinguisherRotation * Quaternion.Euler(180f, 0f, 0f));
                break;
            case AnomalyType.MissingTexturePoster:
                SetMissingTexture(_bugPoster);
                break;
            case AnomalyType.ColleagueStare:
                _colleague.ResetWalk(true);
                break;
        }
    }

    // 가까이 가야 모니터 글자가 켜짐
    private void UpdateMonitorWake()
    {
        foreach (TextMesh monitorText in _monitorTexts)
        {
            float distance = Vector3.Distance(_player.position, monitorText.transform.position);
            monitorText.gameObject.SetActive(distance <= _monitorWakeDistance);
        }
    }

    private void UpdateTexts()
    {
        string code = BuildCodeText();
        foreach (TextMesh monitorText in _monitorTexts)
        {
            monitorText.text = code;
        }

        _clockText.text = FormatTime(_count);
    }

    private string BuildCodeText()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"// {FormatTime(_count)}   BUILD {_count} / {ClearCount}");
        if (!string.IsNullOrEmpty(_report)) builder.AppendLine($"// {_report}");

        if (_isCleared)
        {
            builder.AppendLine("// RELEASE COMPLETE. Time to go home.");
            return builder.ToString();
        }

        // 바뀐 값이 없으면 원본과 같은 한 줄로 표시
        bool hasOverride = _currentBuild.LightOverride != LightOverride.None || _currentBuild.HasNoColleague;
        if (_currentBuild.IsOriginal || !hasOverride)
        {
            builder.AppendLine($"class Office_{GetBuildNumber():00} : BaseOffice {{ }}");
            return builder.ToString();
        }

        builder.AppendLine($"class Office_{GetBuildNumber():00} : BaseOffice");
        builder.AppendLine("{");
        AppendOverrides(builder);
        builder.AppendLine("}");
        return builder.ToString();
    }

    private void AppendOverrides(StringBuilder builder)
    {
        if (_currentBuild.LightOverride != LightOverride.None) builder.AppendLine($"    override LightColor => {_currentBuild.LightOverride};");
        if (_currentBuild.HasNoColleague) builder.AppendLine("    override NpcCount => 0;");
    }

    private int GetBuildNumber() => _currentBuild.IsOriginal ? 0 : _count + 1;

    private string FormatTime(int hoursPassed) => $"{(StartHour + hoursPassed) % HoursPerDay:00}:00";

    // 한 루프의 빌드 명세
    private struct OfficeBuild
    {
        public bool IsOriginal;
        public LightOverride LightOverride;
        public bool HasNoColleague;
        public AnomalyType Anomaly;
    }
}
