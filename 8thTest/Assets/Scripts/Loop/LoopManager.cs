using System.Collections.Generic;
using System.Collections;
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
    // 미끼 override 종류 수 (빨간 조명, 파란 조명, 동료 없음, 화분 없음, 책장 열림)
    private const int OverrideKindCount = 5;
    private const float EmissionIntensity = 2f;

    // 모니터 글자 색 (리치 텍스트)
    private const string WaitingColor = "#F2C14E";
    private const string PassColor = "#5FD07A";
    private const string FailColor = "#FF6B6B";
    private const string CommentColor = "#6A9955";
    private const string AddedColor = "#5FD07A";
    private const string HotfixColor = "#FFA94D";

    // QA 수칙 
    private static readonly string[] RuleLines =
    {
        "1. 코드는 틀리지 않습니다. 틀리는 것은 사무실입니다.",
        "2. 코드와 다르다면, 계단을 내려가십시오.",
        "3. 코드와 같다면, 계단을 올라가십시오.",
        "4. 코드는 긴급 수정될 수 있습니다. 1번을 기억하십시오.",
        "5. 버그 리포트는 컴퓨터에서. 세 번까지만.",
        "6. 06:00이 되면 퇴근하셔도 좋습니다.",
    };

    [Header("루프")]
    // 앞으로 통과했을 때 플레이어를 옮길 거리
    [SerializeField] private Vector3 _forwardTeleportOffset = new(-27.2f, -3.2f, -6.2f);

    // 롤백 계단을 내려가다 통과했을 때 플레이어를 옮길 거리 (롤백 계단실 → 시작 계단실 위층)
    [SerializeField] private Vector3 _rollbackTeleportOffset = new(-7.4f, 3.2f, 0f);

    // 플레이어 (모니터 켜짐 거리 계산용)
    [SerializeField] private Transform _player;

    // 모니터가 켜지는 거리 (멀리서는 코드가 바뀌는 순간이 보이지 않게)
    [SerializeField, Min(0f)] private float _monitorWakeDistance = 2.5f;

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
    [SerializeField, Min(0f)] private float _extinguisherCeilingHeight = 2.7f;

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
    [SerializeField, Range(0f, 90f)] private float _eyeDoorOpenAngle = 20f;

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
    [SerializeField, Range(0f, 90f)] private float _wiltLeafTilt = 18f;

    // 시든 화분 앞 바닥에 떨어진 마른 잎
    [SerializeField] private GameObject _fallenLeaves;

    // 책장 문짝들의 경첩 축 (문짝이 자식, 양쪽으로 열리는 여닫이문)
    [SerializeField] private Transform[] _cabinetDoorHinges;

    // 문짝마다 열리는 각도 (경첩이 양 끝에 있어 서로 부호가 반대)
    [SerializeField] private float[] _cabinetDoorOpenAngles = { -100f, 100f };

    // 문과 함께 앞으로 빠지는 서랍
    [SerializeField] private Transform _cabinetDrawer;

    // 서랍이 빠지는 방향과 거리 (월드 기준, 책장 앞쪽)
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
    [SerializeField, Range(0, ClearCount - 1)] private int _bloodWritingStartHour = 3;

    [Header("단말 · 태블릿")]
    // 판정 기록을 남길 검수 로그 단말 (코드판에는 결과를 띄우지 않음)
    [SerializeField] private QaLogTerminal _qaLog;

    // 긴급 핫픽스로 코드가 바뀌면 알림을 띄울 플레이어 태블릿
    [SerializeField] private CodeTablet _tablet;

    [Header("긴급 핫픽스")]
    // 판정 대기 중 긴급 핫픽스로 코드가 바뀔 확률 (원본 빌드 제외)
    [SerializeField, Range(0f, 1f)] private float _hotfixChance = 0.3f;

    // 통로에 들어온 뒤 핫픽스가 배포되기까지 걸리는 시간(초, 최소 ~ 최대)
    [SerializeField] private Vector2 _hotfixDelay = new(6f, 14f);

    // 핫픽스가 나오기 시작하는 경과 시간 (22:00 기준, 1이면 23:00부터)
    [SerializeField, Range(0, ClearCount - 1)] private int _hotfixStartHour = 1;

    // 핫픽스가 사무실에도 바로 반영될 확률 (반영되면 정상, 코드만 바뀌면 버그)
    [SerializeField, Range(0f, 1f)] private float _hotfixApplyChance = 0.5f;

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
    private Coroutine _hotfixRoutine;
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

    // 06:00 이후에는 동료들이 먼저 내려간 뒤에만 엘리베이터를 쓸 수 있음 (퇴근 연출과 문이 겹치지 않게)
    // 밤에는 엘리베이터가 잠겨 있음 (롤백은 이제 내려가는 계단이 맡음, 엘리베이터는 퇴근 전용)
    public bool CanUseElevator => _isCleared && _ending.CanLeave;

    private void Start()
    {
        CacheOriginals();
        ApplyBuild(CreateOriginalBuild());

        // QA 수칙은 코드판이 아니라 출근하자마자 태블릿 수칙 페이지로 보냄
        var rules = new StringBuilder();
        AppendRules(rules);
        _tablet.SetRules(rules.ToString());
    }

    private void Update() => UpdateMonitorWake();

    // 트리거 통과 처리 (통로 진입, 앞으로, 뒤로)
    public void HandleTrigger(LoopTriggerType type, FirstPersonController player)
    {
        // 06:00 이후에는 엘리베이터(퇴근)만 받음, 동료들이 먼저 내려간 뒤에만 탈 수 있음
        if (_isCleared)
        {
            if (type == LoopTriggerType.Back && _ending.CanLeave)
            {
                ReturnToNight();
            }

            // 계단으로 가도 갇히지 않게 이동만 시킴 (판정 없음)
            if (type == LoopTriggerType.Forward)
            {
                player.Teleport(_forwardTeleportOffset);
            }

            if (type == LoopTriggerType.Rollback)
            {
                player.Teleport(_rollbackTeleportOffset);
            }

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
            case LoopTriggerType.Rollback:
                // 판정 대기 중일 때만 판정하고, 이동은 항상 함 (계단 끝 가짜 문 앞에 갇히지 않게)
                if (_isArmed)
                {
                    Judge(false);
                }

                player.Teleport(_rollbackTeleportOffset);
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
        if (_colleague.gameObject.activeSelf)
        {
            _colleague.BeginWalk(player.transform);
        }

        ScheduleHotfix();
    }

    // 이번 루프에 긴급 핫픽스를 낼지 정하고, 내면 잠시 뒤 코드만 바꿈
    private void ScheduleHotfix()
    {
        if (_currentBuild.IsOriginal || _count < _hotfixStartHour || Random.value >= _hotfixChance) return;

        _hotfixRoutine = StartCoroutine(DeployHotfixLaterRoutine());
    }

    private IEnumerator DeployHotfixLaterRoutine()
    {
        yield return new WaitForSeconds(Random.Range(_hotfixDelay.x, _hotfixDelay.y));
        _hotfixRoutine = null;
        if (_isArmed)
        {
            DeployHotfix();
        }
    }

    private void CancelHotfix()
    {
        if (_hotfixRoutine != null)
        {
            StopCoroutine(_hotfixRoutine);
        }

        _hotfixRoutine = null;
    }

    // 사무실은 그대로 두고 코드만 바꿈 → 플레이어가 바뀐 코드로 다시 판단해야 함
    private void DeployHotfix()
    {
        var candidates = new List<(HotfixKind Kind, OverrideKind Target)>();
        OfficeBuild build = _currentBuild;

        // 책장 문이 열린 버그를 코드에 반영해 정상으로 만듦
        if (build.Anomaly == AnomalyType.CabinetDoorOpen)
        {
            candidates.Add((HotfixKind.Legitimize, OverrideKind.Bookcase));
        }

        // 조명 명세 불일치는 Blue 줄이 있어야 성립하므로 조명은 건드리지 않음
        if (build.Anomaly != AnomalyType.LightMismatch)
        {
            if (build.LightOverride == LightOverride.None)
            {
                candidates.Add((HotfixKind.AddLine, OverrideKind.RedLight));
                candidates.Add((HotfixKind.AddLine, OverrideKind.BlueLight));
            }
            else
            {
                candidates.Add((HotfixKind.RemoveLine, build.LightOverride == LightOverride.Red ? OverrideKind.RedLight : OverrideKind.BlueLight));
            }
        }

        candidates.Add((build.HasNoColleague ? HotfixKind.RemoveLine : HotfixKind.AddLine, OverrideKind.NoColleague));
        candidates.Add((build.HasNoPlants ? HotfixKind.RemoveLine : HotfixKind.AddLine, OverrideKind.NoPlants));
        if (build.IsCabinetOpen)
        {
            candidates.Add((HotfixKind.RemoveLine, OverrideKind.Bookcase));
        }
        else if (build.Anomaly != AnomalyType.CabinetDoorOpen)
        {
            candidates.Add((HotfixKind.AddLine, OverrideKind.Bookcase));
        }

        (HotfixKind kind, OverrideKind target) = candidates[Random.Range(0, candidates.Count)];
        _currentBuild.Hotfix = kind;
        _currentBuild.HotfixTarget = target;

        // 절반쯤은 사무실도 눈앞에서 같이 바뀜 (코드와 같아져 정상), 나머지는 코드만 바뀜 (버그)
        _currentBuild.IsHotfixApplied = kind != HotfixKind.Legitimize && Random.value < _hotfixApplyChance;
        if (_currentBuild.IsHotfixApplied)
        {
            ApplyHotfixToOffice(kind, target);
        }

        UpdateTexts();
        _qaLog.AddLine(Colorize($"[{FormatTime(_count)}] Office_{GetBuildNumber():00}  긴급 핫픽스 배포", HotfixColor));
        _tablet.NotifyCodeChanged();
    }

    // 핫픽스 내용을 사무실에 바로 반영 (조명 색, 동료, 화분, 책장)
    private void ApplyHotfixToOffice(HotfixKind kind, OverrideKind target)
    {
        bool isAdded = kind == HotfixKind.AddLine;
        switch (target)
        {
            case OverrideKind.RedLight:
            case OverrideKind.BlueLight:
                OfficeBuild lightBuild = _currentBuild;
                lightBuild.LightOverride = !isAdded ? LightOverride.None : target == OverrideKind.RedLight ? LightOverride.Red : LightOverride.Blue;
                ApplyLightColor(lightBuild);
                break;
            case OverrideKind.NoColleague:
                _colleague.gameObject.SetActive(!isAdded);
                break;
            case OverrideKind.NoPlants:
                foreach (GameObject plant in _plants)
                {
                    plant.SetActive(!isAdded);
                }

                break;
            default:
                if (isAdded)
                {
                    OpenCabinetDoor();
                }
                else
                {
                    CloseCabinetDoor();
                }

                break;
        }
    }

    // 사무실이 코드와 다른지 (핫픽스가 있으면 바뀐 코드 기준)
    private static bool HasBug(OfficeBuild build)
    {
        switch (build.Hotfix)
        {
            case HotfixKind.Legitimize:
                return false;
            case HotfixKind.AddLine:
            case HotfixKind.RemoveLine:
                // 사무실에도 반영됐으면 핫픽스 줄은 맞고, 원래 이상현상만 따짐
                return !build.IsHotfixApplied || build.Anomaly != AnomalyType.None;
            default:
                return build.Anomaly != AnomalyType.None;
        }
    }

    private void Judge(bool isForward)
    {
        _isArmed = false;
        CancelHotfix();

        // 원본 확인 구간은 판정 없이 검수 시작
        if (_currentBuild.IsOriginal)
        {
            if (isForward)
            {
                _qaLog.AddLine(Colorize($"[{FormatTime(0)}] 검수 시작", WaitingColor));
            }

            ApplyBuild(isForward ? CreateJudgedBuild() : CreateOriginalBuild());
            return;
        }

        // 핫픽스가 배포됐으면 바뀐 코드 기준으로 판정
        bool hasBug = HasBug(_currentBuild);
        bool isCorrect = isForward != hasBug;
        // 판정 결과는 코드판이 아니라 검수 로그 단말에 남김
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
        CancelHotfix();
        _isCleared = false;
        _count = 0;
        ApplyBuild(CreateOriginalBuild());

        // 엘리베이터 문이 열리면 밤 사무실, 잠시 뒤 타이틀로
        _ending.PlayReturn();
    }

    // 검수 로그 단말에 남길 판정 기록 (오답이면 틀린 이유)
    private void LogJudgement(bool isForward, bool hasBug, bool isCorrect)
    {
        string action = isForward ? "승인" : "롤백";
        string result = isCorrect ? Colorize("PASS", PassColor) : Colorize("FAIL", FailColor);
        _qaLog.AddLine($"[{FormatTime(_count)}] Office_{GetBuildNumber():00}  {action}  {result}");

        if (!isCorrect)
        {
            string reason = hasBug ? $"놓친 버그: {GetMissedLabel()}" : GetNormalLabel();
            _qaLog.AddLine(Colorize($"   └ {reason}", FailColor));

            // 오답이면 시간이 처음으로 돌아감 (롤백과 헷갈리지 않게 '초기화'로 표기)
            _qaLog.AddLine(Colorize($"── {FormatTime(0)} 세션 초기화 ──", CommentColor));
            _qaLog.MarkFailure();
            return;
        }
    }

    // 놓친 버그 이름 (핫픽스를 반영하지 않은 경우 우선)
    private string GetMissedLabel()
    {
        bool isHotfixBug = _currentBuild.Hotfix is HotfixKind.AddLine or HotfixKind.RemoveLine && !_currentBuild.IsHotfixApplied;
        return isHotfixBug ? $"핫픽스 미반영 ({GetOverrideLabel(_currentBuild.HotfixTarget)})" : GetAnomalyLabel(_currentBuild.Anomaly);
    }

    // 정상이었는데 롤백한 경우의 설명
    private string GetNormalLabel()
    {
        if (_currentBuild.Hotfix == HotfixKind.Legitimize) return $"정상 빌드였음 (핫픽스로 {GetOverrideLabel(_currentBuild.HotfixTarget)} 반영)";
        if (_currentBuild.IsHotfixApplied) return $"정상 빌드였음 (핫픽스 {GetOverrideLabel(_currentBuild.HotfixTarget)} 사무실 반영 완료)";

        return "정상 빌드였음";
    }

    private static string GetOverrideLabel(OverrideKind kind)
    {
        switch (kind)
        {
            case OverrideKind.RedLight:
                return "조명 Red";
            case OverrideKind.BlueLight:
                return "조명 Blue";
            case OverrideKind.NoColleague:
                return "동료 없음";
            case OverrideKind.NoPlants:
                return "화분 없음";
            default:
                return "책장 열림";
        }
    }

    private static string GetOverrideCode(OverrideKind kind)
    {
        switch (kind)
        {
            case OverrideKind.RedLight:
                return "override LightColor => Red;";
            case OverrideKind.BlueLight:
                return "override LightColor => Blue;";
            case OverrideKind.NoColleague:
                return "override NpcCount => 0;";
            case OverrideKind.NoPlants:
                return "override PlantCount => 0;";
            default:
                return "override BookcaseOpen => true;";
        }
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
                return "책장 문";
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
            if (Random.value < OverrideChance)
            {
                AddRandomOverride(ref build);
            }

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
    // 화분·책장도 같은 소품의 이상현상과 그 소품을 바꾸는 override는 함께 내지 않음
    private void AddRandomOverride(ref OfficeBuild build)
    {
        // 이번 이상현상과 겹치는 종류가 뽑히면 다시 뽑음
        while (true)
        {
            switch ((OverrideKind)Random.Range(0, OverrideKindCount))
            {
                case OverrideKind.RedLight:
                    build.LightOverride = LightOverride.Red;
                    return;
                case OverrideKind.BlueLight:
                    build.LightOverride = LightOverride.Blue;
                    return;
                case OverrideKind.NoColleague:
                    if (build.Anomaly == AnomalyType.ColleagueStare) continue;
                    build.HasNoColleague = true;
                    return;
                case OverrideKind.NoPlants:
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

    // 코드에 적힌 소품 override 적용 (화분 없음, 책장 열림)
    private void ApplyPropOverrides(OfficeBuild build)
    {
        if (build.HasNoPlants)
        {
            foreach (GameObject plant in _plants)
            {
                plant.SetActive(false);
            }
        }

        if (build.IsCabinetOpen)
        {
            OpenCabinetDoor();
        }
    }

    // 책장 문짝을 양쪽으로 활짝 열고 서랍을 앞으로 빼냄
    private void OpenCabinetDoor()
    {
        for (int i = 0; i < _cabinetDoorHinges.Length; i++)
        {
            _cabinetDoorHinges[i].localRotation = _cabinetDoorRotations[i] * Quaternion.Euler(0f, _cabinetDoorOpenAngles[i], 0f);
        }

        _cabinetDrawer.position = _cabinetDrawerPosition + _cabinetDrawerPull;
    }

    // 책장 문을 닫고 서랍을 원래대로 (핫픽스로 BookcaseOpen 줄이 지워지고 사무실에도 반영될 때)
    private void CloseCabinetDoor()
    {
        for (int i = 0; i < _cabinetDoorHinges.Length; i++)
        {
            _cabinetDoorHinges[i].localRotation = _cabinetDoorRotations[i];
        }

        _cabinetDrawer.position = _cabinetDrawerPosition;
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
                // 코드에 BookcaseOpen이 없는데 책장 문이 열려 있음
                OpenCabinetDoor();
                break;
            case AnomalyType.TrashOverflow:
                _trashOverflow.SetActive(true);
                break;
            case AnomalyType.WhiteboardMessage:
                // 01:00 전에는 메모 한 줄만 슬쩍 바뀌고, 01:00부터는 피로 쓴 글
                if (_count < _bloodWritingStartHour)
                {
                    _whiteboardLastLine.text = _whiteboardChangedLine;
                }
                else
                {
                    ShowBloodWriting();
                }

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
        if (_currentBuild.Hotfix != HotfixKind.None)
        {
            builder.AppendLine(Colorize("● 긴급 핫픽스 적용됨 — 다시 확인하세요", HotfixColor));
        }

        builder.AppendLine();

        // 바뀐 값이 없으면 원본과 같은 한 줄로 표시
        bool hasOverride = _currentBuild.LightOverride != LightOverride.None || _currentBuild.HasNoColleague
            || _currentBuild.HasNoPlants || _currentBuild.IsCabinetOpen || _currentBuild.Hotfix != HotfixKind.None;
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
        if (_currentBuild.LightOverride != LightOverride.None)
        {
            AppendOverrideLine(builder, _currentBuild.LightOverride == LightOverride.Red ? OverrideKind.RedLight : OverrideKind.BlueLight);
        }

        if (_currentBuild.HasNoColleague)
        {
            AppendOverrideLine(builder, OverrideKind.NoColleague);
        }

        if (_currentBuild.HasNoPlants)
        {
            AppendOverrideLine(builder, OverrideKind.NoPlants);
        }

        if (_currentBuild.IsCabinetOpen)
        {
            AppendOverrideLine(builder, OverrideKind.Bookcase);
        }

        // 핫픽스로 새로 들어온 줄은 주황색
        bool isHotfixAdded = _currentBuild.Hotfix is HotfixKind.AddLine or HotfixKind.Legitimize;
        if (isHotfixAdded)
        {
            builder.AppendLine(Colorize($"<b>+   {GetOverrideCode(_currentBuild.HotfixTarget)}</b>   // hotfix", HotfixColor));
        }
    }

    // 원래 있던 override 한 줄 (핫픽스로 지워졌으면 - 표시와 빨간색)
    private void AppendOverrideLine(StringBuilder builder, OverrideKind kind)
    {
        bool isRemoved = _currentBuild.Hotfix == HotfixKind.RemoveLine && _currentBuild.HotfixTarget == kind;
        // 방송 화면에서도 잘 보이게 바뀐 줄은 굵게
        builder.AppendLine(isRemoved ? Colorize($"<b>-   {GetOverrideCode(kind)}</b>   // hotfix", FailColor) : Colorize($"<b>+   {GetOverrideCode(kind)}</b>", AddedColor));
    }

    private void AppendRules(StringBuilder builder)
    {
        foreach (string line in RuleLines)
        {
            // 번호만 강조색, 나머지는 공지문처럼 한 색
            int dot = line.IndexOf(". ");
            bool isNumbered = dot > 0 && char.IsDigit(line[0]);
            builder.AppendLine(isNumbered ? Colorize(line.Substring(0, dot + 1), WaitingColor) + line.Substring(dot + 1) : line);
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

        // 근무 중 배포된 긴급 핫픽스 (사무실은 그대로, 코드만 바뀜)
        public HotfixKind Hotfix;
        public OverrideKind HotfixTarget;
        public bool IsHotfixApplied;
    }
}
