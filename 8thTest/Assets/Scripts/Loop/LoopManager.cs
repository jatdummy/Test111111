using System.Text;
using UnityEngine;

// 루프 진행, 빌드 출제, 판정 관리
public class LoopManager : MonoBehaviour
{
    private const int ClearCount = 8;
    private const int StartHour = 22;
    private const int HoursPerDay = 24;
    private const int AnomalyCount = 10;
    private const int MaxNormalStreak = 2;
    private const int MaxBugStreak = 4;
    private const int MismatchLightIndex = 2;
    private const float NormalBuildChance = 1f / 3f;
    private const float OverrideChance = 0.5f;
    // 미끼 override 종류 수 (빨간 조명, 파란 조명, 동료 없음, 화분 없음, 캐비닛 열림)
    private const int OverrideKindCount = 5;
    private const float EmissionIntensity = 2f;

    // 모니터 글자 색 (리치 텍스트)
    private const string WaitingColor = "#F2C14E";
    private const string PassColor = "#5FD07A";
    private const string FailColor = "#FF6B6B";
    private const string CommentColor = "#6A9955";
    private const string AddedColor = "#5FD07A";

    // 로비에서만 보이는 접속 정보 (플레이어는 8번째 테스터)
    private const string SessionLine = "// 야간 검수 세션: tester_08 · 세션 종료: 릴리스 이후";

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

    // 천장에 붙을 때 벽에서 떨어뜨리는 거리 (벽에 박히지 않고 천장 면에 보이게)
    [SerializeField] private Vector3 _extinguisherCeilingInset = new(0f, 0f, -0.3f);

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

    // 복도 화분 (override PlantCount => 0 이면 모두 숨김)
    [SerializeField] private GameObject[] _plants;

    // 시드는 이상현상이 나올 화분의 잎
    [SerializeField] private Renderer _wiltPlantLeaves;

    // 시든 잎 재질
    [SerializeField] private Material _deadPlantMaterial;

    // 시든 잎 크기 (높이를 줄이고 옆으로 퍼뜨려 주저앉은 모양)
    [SerializeField] private Vector3 _wiltLeafScale = new(1.15f, 0.55f, 1.15f);

    // 시든 잎이 복도 쪽으로 기우는 각도
    [SerializeField] private float _wiltLeafTilt = 18f;

    // 시든 화분 앞 바닥에 떨어진 마른 잎
    [SerializeField] private GameObject _fallenLeaves;

    // 서류 캐비닛 문짝들의 경첩 축 (문짝이 자식, 양쪽으로 열리는 여닫이문)
    [SerializeField] private Transform[] _cabinetDoorHinges;

    // 문짝마다 열리는 각도 (경첩이 양 끝에 있어 서로 부호가 반대)
    [SerializeField] private float[] _cabinetDoorOpenAngles = { -100f, 100f };

    // 문과 함께 앞으로 빠지는 서랍
    [SerializeField] private Transform _cabinetDrawer;

    // 서랍이 빠지는 방향과 거리 (월드 기준, 캐비닛 앞쪽)
    [SerializeField] private Vector3 _cabinetDrawerPull = new(-0.3f, 0f, 0f);

    // 쓰레기통 위와 바닥에 넘친 종이 뭉치
    [SerializeField] private GameObject _trashOverflow;

    // 화이트보드 회의 메모의 마지막 줄 (이른 시간 이상현상에서 바뀜)
    [SerializeField] private TextMesh _whiteboardLastLine;

    // 이른 시간 이상현상에서 바뀐 마지막 줄
    [SerializeField] private string _whiteboardChangedLine = "· 퇴근 없음";

    // 화이트보드에 피로 쓴 글 묶음 (글자와 흘러내린 자국)
    [SerializeField] private GameObject _bloodWriting;

    // 피로 쓴 글자
    [SerializeField] private TextMesh _bloodMessage;

    // 피로 쓰는 문구 (나올 때마다 하나를 고름)
    [SerializeField] private string[] _bloodPhrases =
    {
        "제발\n탈출시켜줘",
        "코드를\n믿지마",
        "06시는\n오지 않아",
        "뒤에\n있어",
        "나도\n승인했었어",
    };

    // 문구마다 붙는 서명 (_bloodPhrases와 같은 순서, 빈 칸이면 서명 없음)
    [SerializeField] private string[] _bloodSignatures = { "", "", "- 01", "", "- 07" };

    // 피로 쓴 서명 글자 (서명이 없는 문구면 숨김)
    [SerializeField] private TextMesh _bloodSignature;

    // 피로 쓴 글이 나오기 시작하는 경과 시간 (22:00 기준, 3이면 01:00)
    [SerializeField] private int _bloodWritingStartHour = 3;

    [Header("단서")]
    // 판정 결과 아래에 붙는 검수 기록 (경과 시간별, 빈 칸이면 없음)
    // 사무실이 아니라 화면 글자만 바뀌어서 이상현상 판정에 영향이 없음
    [SerializeField] private string[] _reportHints =
    {
        "",
        "",
        "[SYS] 이전 세션 검수자 tester_07 · 로그아웃 기록 없음",
        "",
        "[SYS] 이전 세션 검수자 tester_01 · 로그아웃 기록 없음",
        "",
        "[SYS] 다음 세션 검수자 tester_09 · 좌석 배정 완료",
        "",
    };

    // 판정 기록과 단서를 남길 검수 로그 단말 (코드판에는 결과를 띄우지 않음)
    [SerializeField] private QaLogTerminal _qaLog;

    // 코드가 바뀔 때마다 알림을 띄울 플레이어 태블릿
    [SerializeField] private CodeTablet _tablet;

    // 06:00 퇴근 연출 (햇빛, 동료 퇴근, 엘리베이터 반전)
    [SerializeField] private EndingDirector _ending;

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
    private bool _isReady;
    private Vector3 _extinguisherPosition;
    private Quaternion _extinguisherRotation;
    private Material[] _posterMaterials;
    private Quaternion _eyeDoorRotation;
    private Material _plantLeafMaterial;
    private Vector3 _plantLeafScale;
    private Quaternion _plantLeafRotation;
    private Quaternion[] _cabinetDoorRotations;
    private Vector3 _cabinetDrawerPosition;
    private string _whiteboardLine;

    private void Start()
    {
        CacheOriginals();
        ApplyBuild(CreateOriginalBuild());
        _isReady = true;
    }

    // 06:00 이후에는 동료들이 먼저 내려간 뒤에만 엘리베이터를 쓸 수 있음 (퇴근 연출과 문이 겹치지 않게)
    // 밤에는 통로에 들어와 판정 대기 중일 때만 롤백할 수 있음 (로비에서 타면 아무 일도 안 일어나는 것 방지)
    public bool CanUseElevator => _isCleared ? _ending.CanLeave : _isArmed;

    private void Update() => UpdateMonitorWake();

    // 트리거 통과 처리 (통로 진입, 앞으로, 뒤로)
    public void HandleTrigger(LoopTriggerType type, FirstPersonController player)
    {
        // 06:00 이후에는 엘리베이터(퇴근)만 받음, 동료들이 먼저 내려간 뒤에만 탈 수 있음
        if (_isCleared)
        {
            if (type == LoopTriggerType.Back && _ending.CanLeave) ReturnToNight();
            return;
        }

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
        _plantLeafMaterial = _wiltPlantLeaves.sharedMaterial;
        _plantLeafScale = _wiltPlantLeaves.transform.localScale;
        _plantLeafRotation = _wiltPlantLeaves.transform.localRotation;
        _cabinetDoorRotations = new Quaternion[_cabinetDoorHinges.Length];
        for (int i = 0; i < _cabinetDoorHinges.Length; i++)
        {
            _cabinetDoorRotations[i] = _cabinetDoorHinges[i].localRotation;
        }

        _cabinetDrawerPosition = _cabinetDrawer.position;
        _whiteboardLine = _whiteboardLastLine.text;
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

        // 원본 확인 구간은 판정 없이 검수 시작
        if (_currentBuild.IsOriginal)
        {
            if (isForward) _qaLog.AddLine(Colorize($"[{FormatTime(0)}] 검수 시작", WaitingColor));
            ApplyBuild(isForward ? CreateJudgedBuild() : CreateOriginalBuild());
            return;
        }

        bool hasBug = _currentBuild.Anomaly != AnomalyType.None;
        bool isCorrect = isForward != hasBug;
        // 검수 기록 단서는 맞혔을 때만 붙음 (시간이 진행될수록 드러나는 보상)
        // 판정 결과와 단서는 코드판이 아니라 검수 로그 단말에 남김
        LogJudgement(isForward, hasBug, isCorrect);

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

            // 창으로 햇빛이 들고 동료들이 먼저 퇴근
            _ending.BeginMorning();
            return;
        }

        ApplyBuild(CreateJudgedBuild());
    }

    // 클리어 뒤 퇴근하려 해도 다시 밤 10시
    private void ReturnToNight()
    {
        _isArmed = false;
        _isCleared = false;
        _count = 0;
        ApplyBuild(CreateOriginalBuild());

        // 엘리베이터 문이 열리면 밤 사무실, 잠시 뒤 타이틀로
        _ending.PlayReturn();
    }

    // 검수 로그 단말에 남길 판정 기록 (오답이면 틀린 이유, 정답이면 이번 시간대 단서)
    private void LogJudgement(bool isForward, bool hasBug, bool isCorrect)
    {
        string action = isForward ? "승인" : "롤백";
        string result = isCorrect ? Colorize("PASS", PassColor) : Colorize("FAIL", FailColor);
        _qaLog.AddLine($"[{FormatTime(_count)}] Office_{GetBuildNumber():00}  {action}  {result}");

        if (!isCorrect)
        {
            string reason = hasBug ? $"놓친 버그: {GetAnomalyLabel(_currentBuild.Anomaly)}" : "정상 빌드였음";
            _qaLog.AddLine(Colorize($"   └ {reason}", FailColor));

            // 오답이면 시간이 처음으로 돌아감 (롤백과 헷갈리지 않게 '초기화'로 표기)
            _qaLog.AddLine(Colorize($"── {FormatTime(0)} 세션 초기화 ──", CommentColor));
            return;
        }

        string hint = GetReportHint();
        if (!string.IsNullOrEmpty(hint)) _qaLog.AddLine(hint);
    }

    // 이번 시간대의 검수 기록 한 줄 (없으면 빈 문자열)
    private string GetReportHint()
    {
        if (_count >= _reportHints.Length || string.IsNullOrEmpty(_reportHints[_count])) return string.Empty;

        return Colorize(_reportHints[_count], CommentColor);
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
            case AnomalyType.WiltedPlant:
                return "화분 상태";
            case AnomalyType.CabinetDoorOpen:
                return "서류 캐비닛 문";
            case AnomalyType.TrashOverflow:
                return "쓰레기통";
            case AnomalyType.WhiteboardMessage:
                return "화이트보드";
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
    // 화분·캐비닛도 같은 소품의 이상현상과 그 소품을 바꾸는 override는 함께 내지 않음
    private void AddRandomOverride(ref OfficeBuild build)
    {
        // 이번 이상현상과 겹치는 종류가 뽑히면 다시 뽑음
        while (true)
        {
            switch (Random.Range(0, OverrideKindCount))
            {
                case 0:
                    build.LightOverride = LightOverride.Red;
                    return;
                case 1:
                    build.LightOverride = LightOverride.Blue;
                    return;
                case 2:
                    if (build.Anomaly == AnomalyType.ColleagueStare) continue;
                    build.HasNoColleague = true;
                    return;
                case 3:
                    if (build.Anomaly == AnomalyType.WiltedPlant) continue;
                    build.HasNoPlants = true;
                    return;
                default:
                    if (build.Anomaly == AnomalyType.CabinetDoorOpen) continue;
                    build.IsCabinetOpen = true;
                    return;
            }
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
        ApplyPropOverrides(build);
        UpdateTexts();

        // 게임 시작 때 말고, 루프마다 코드가 바뀌면 태블릿 알림
        if (_isReady) _tablet.NotifyCodeChanged();
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
        foreach (GameObject plant in _plants)
        {
            plant.SetActive(true);
        }

        _wiltPlantLeaves.sharedMaterial = _plantLeafMaterial;
        _wiltPlantLeaves.transform.localScale = _plantLeafScale;
        _wiltPlantLeaves.transform.localRotation = _plantLeafRotation;
        _fallenLeaves.SetActive(false);
        for (int i = 0; i < _cabinetDoorHinges.Length; i++)
        {
            _cabinetDoorHinges[i].localRotation = _cabinetDoorRotations[i];
        }

        _cabinetDrawer.position = _cabinetDrawerPosition;
        _trashOverflow.SetActive(false);
        _whiteboardLastLine.text = _whiteboardLine;
        _bloodWriting.SetActive(false);
    }

    // 코드에 적힌 소품 override 적용 (화분 없음, 캐비닛 열림)
    private void ApplyPropOverrides(OfficeBuild build)
    {
        if (build.HasNoPlants)
        {
            foreach (GameObject plant in _plants)
            {
                plant.SetActive(false);
            }
        }

        if (build.IsCabinetOpen) OpenCabinetDoor();
    }

    // 캐비닛 문짝을 양쪽으로 활짝 열고 서랍을 앞으로 빼냄
    private void OpenCabinetDoor()
    {
        for (int i = 0; i < _cabinetDoorHinges.Length; i++)
        {
            _cabinetDoorHinges[i].localRotation = _cabinetDoorRotations[i] * Quaternion.Euler(0f, _cabinetDoorOpenAngles[i], 0f);
        }

        _cabinetDrawer.position = _cabinetDrawerPosition + _cabinetDrawerPull;
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
                // 앞뒤(Z축) 기준으로 뒤집어야 벽 쪽으로 넘어가지 않음
                _extinguisher.SetPositionAndRotation(ceilingPosition + _extinguisherCeilingInset, _extinguisherRotation * Quaternion.Euler(0f, 0f, 180f));
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
            case AnomalyType.WiltedPlant:
                // 화분 하나만 잎이 갈색으로 시듦
                _wiltPlantLeaves.sharedMaterial = _deadPlantMaterial;
                // 잎이 주저앉고 복도 쪽으로 기울며, 바닥에 마른 잎이 떨어져 있음
                _wiltPlantLeaves.transform.localScale = Vector3.Scale(_plantLeafScale, _wiltLeafScale);
                _wiltPlantLeaves.transform.localRotation = _plantLeafRotation * Quaternion.Euler(_wiltLeafTilt, 0f, 0f);
                _fallenLeaves.SetActive(true);
                break;
            case AnomalyType.CabinetDoorOpen:
                // 코드에 CabinetOpen이 없는데 캐비닛 문이 열려 있음
                OpenCabinetDoor();
                break;
            case AnomalyType.TrashOverflow:
                _trashOverflow.SetActive(true);
                break;
            case AnomalyType.WhiteboardMessage:
                // 01:00 전에는 메모 한 줄만 슬쩍 바뀌고, 01:00부터는 피로 쓴 글
                if (_count < _bloodWritingStartHour) _whiteboardLastLine.text = _whiteboardChangedLine;
                else ShowBloodWriting();
                break;
        }
    }

    // 화이트보드에 피로 쓴 글 (매번 다른 문구)
    private void ShowBloodWriting()
    {
        int index = Random.Range(0, _bloodPhrases.Length);
        _bloodMessage.text = _bloodPhrases[index];

        // 서명은 문구를 쓴 사람마다 다르고, 없는 문구도 있음
        string signature = index < _bloodSignatures.Length ? _bloodSignatures[index] : string.Empty;
        _bloodSignature.text = signature;
        _bloodSignature.gameObject.SetActive(!string.IsNullOrEmpty(signature));
        _bloodWriting.SetActive(true);
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
            builder.AppendLine();
            builder.AppendLine(Colorize("// 퇴근하셔도 됩니다.", CommentColor));
            return builder.ToString();
        }

        builder.AppendLine(Colorize($"● 리뷰 대기   Office_{GetBuildNumber():00}   {FormatTime(_count)}", WaitingColor));
        builder.AppendLine();

        // 로비(원본)에서만 코드 맨 위에 수칙 주석 표시
        if (_currentBuild.IsOriginal) AppendRules(builder);

        // 바뀐 값이 없으면 원본과 같은 한 줄로 표시
        bool hasOverride = _currentBuild.LightOverride != LightOverride.None || _currentBuild.HasNoColleague
            || _currentBuild.HasNoPlants || _currentBuild.IsCabinetOpen;
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
        if (_currentBuild.HasNoPlants) builder.AppendLine(Colorize("+   override PlantCount => 0;", AddedColor));
        if (_currentBuild.IsCabinetOpen) builder.AppendLine(Colorize("+   override FileCabinetOpen => true;", AddedColor));
    }

    private void AppendRules(StringBuilder builder)
    {
        builder.AppendLine(Colorize(SessionLine, CommentColor));
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
        public bool HasNoPlants;
        public bool IsCabinetOpen;
        public AnomalyType Anomaly;
    }
}
