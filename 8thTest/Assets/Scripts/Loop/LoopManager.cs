using System.Text;
using UnityEngine;

// 루프 진행, 빌드 출제, 판정 관리
public class LoopManager : MonoBehaviour
{
    private const int ClearCount = 8;
    private const int StartHour = 22;
    private const int HoursPerDay = 24;
    private const int AnomalyCount = 6;
    private const int MaxNormalStreak = 2;
    private const int MaxBugStreak = 4;
    private const int MismatchLightIndex = 2;
    private const float NormalBuildChance = 1f / 3f;
    private const float OverrideChance = 0.5f;
    private const float EmissionIntensity = 2f;

    // 모니터 글자 색 (리치 텍스트)
    private const string WaitingColor = "#F2C14E";
    private const string PassColor = "#5FD07A";
    private const string FailColor = "#FF6B6B";
    private const string CommentColor = "#6A9955";
    private const string AddedColor = "#5FD07A";

    // 로비(원본) 빌드에서만 코드 맨 위에 보여 줄 QA 수칙
    private static readonly string[] RuleLines =
    {
        "// ── QA 수칙 ──",
        "// 코드는 틀리지 않습니다.",
        "// 사무실이 코드와 다르다면, 즉시 되돌아가세요.",
        "// 사무실이 코드와 같다면, 승인하세요.",
        "// 06:00 전에는 퇴근할 수 없습니다.",
    };

    [Header("루프")]
    // 앞으로 통과했을 때 플레이어를 옮길 거리
    [SerializeField] private Vector3 _forwardTeleportOffset = new(-27.2f, -3.2f, -6.2f);

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

    // 눈 이상현상이 나올 문짝의 경첩 축 (문짝과 손잡이가 자식)
    [SerializeField] private Transform _eyeDoorHinge;

    // 눈 이상현상일 때 문이 안쪽으로 열리는 각도
    [SerializeField] private float _eyeDoorOpenAngle = 20f;

    // 문 틈 안쪽에서 보이는 눈
    [SerializeField] private GameObject _doorEyes;

    // 정수기에서 흘러나오는 핏물 (줄기, 방울, 웅덩이)
    [SerializeField] private GameObject _bloodLeak;

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
    private Quaternion _eyeDoorRotation;

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
        _eyeDoorRotation = _eyeDoorHinge.localRotation;
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

    // 다음 루프 모니터 맨 위에 보여 줄 직전 판정 결과
    private string CreateReport(bool hasBug, bool isCorrect)
    {
        string label = GetAnomalyLabel(_currentBuild.Anomaly);
        if (isCorrect) return Colorize(hasBug ? $"● 롤백 완료 — 버그 확인됨 ({label})" : "● 이전 빌드 승인됨", PassColor);

        // 오답이면 시간이 처음으로 돌아감 (롤백과 헷갈리지 않게 '초기화'로 표기)
        string reset = $" — {FormatTime(0)}로 초기화";
        return Colorize(hasBug ? $"● 버그 누락 ({label}){reset}" : $"● 정상 빌드 롤백{reset}", FailColor);
    }

    private string GetAnomalyLabel(AnomalyType anomaly)
    {
        switch (anomaly)
        {
            case AnomalyType.ExtinguisherOnCeiling:
                return "소화기 위치";
            case AnomalyType.LightMismatch:
                return $"{MismatchLightIndex + 1}번 조명 색";
            case AnomalyType.MissingTexturePoster:
                return "포스터 B 텍스처";
            case AnomalyType.ColleagueStare:
                return "동료 시선";
            case AnomalyType.EyesInDoor:
                return "회의실 문 틈";
            case AnomalyType.BloodWater:
                return "정수기 물";
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
        _eyeDoorHinge.localRotation = _eyeDoorRotation;
        _doorEyes.SetActive(false);
        _bloodLeak.SetActive(false);
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
            case AnomalyType.EyesInDoor:
                // 문이 살짝 열리고 틈 안쪽에서 눈이 보임
                _eyeDoorHinge.localRotation = _eyeDoorRotation * Quaternion.Euler(0f, _eyeDoorOpenAngle, 0f);
                _doorEyes.SetActive(true);
                break;
            case AnomalyType.BloodWater:
                _bloodLeak.SetActive(true);
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

        // 클리어하면 리뷰 대기 대신 릴리스 완료
        if (_isCleared)
        {
            builder.AppendLine(Colorize($"● 릴리스 완료   {FormatTime(_count)}", PassColor));
            if (!string.IsNullOrEmpty(_report)) builder.AppendLine(_report);
            builder.AppendLine();
            builder.AppendLine(Colorize("// 퇴근하셔도 됩니다.", CommentColor));
            return builder.ToString();
        }

        builder.AppendLine(Colorize($"● 리뷰 대기   Office_{GetBuildNumber():00}   {FormatTime(_count)}", WaitingColor));
        if (!string.IsNullOrEmpty(_report)) builder.AppendLine(_report);
        builder.AppendLine();

        // 로비(원본)에서만 코드 맨 위에 수칙 주석 표시
        if (_currentBuild.IsOriginal) AppendRules(builder);

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
        // 바뀐 줄은 + 표시와 초록색으로 한눈에 보이게
        if (_currentBuild.LightOverride != LightOverride.None) builder.AppendLine(Colorize($"+   override LightColor => {_currentBuild.LightOverride};", AddedColor));
        if (_currentBuild.HasNoColleague) builder.AppendLine(Colorize("+   override NpcCount => 0;", AddedColor));
    }

    private void AppendRules(StringBuilder builder)
    {
        foreach (string line in RuleLines)
        {
            builder.AppendLine(Colorize(line, CommentColor));
        }
    }

    private static string Colorize(string text, string color) => $"<color={color}>{text}</color>";

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
