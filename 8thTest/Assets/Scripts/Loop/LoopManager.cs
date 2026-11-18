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
    private const int MaxNormalStreak = 2;
    private const int MaxBugStreak = 4;
    private const float NormalBuildChance = 1f / 3f;
    private const float OverrideChance = 0.5f;
    // 미끼 override 종류 수 (빨간 조명, 파란 조명, 동료 없음, 화분 없음, 책장 열림)
    private const int OverrideKindCount = 5;

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
    // 천장 형광등 (코드의 조명 색 override)
    [SerializeField] private OfficeLights _lights;

    // 걸어오는 동료
    [SerializeField] private ColleagueWalker _colleague;

    // 복도 화분 (override PlantCount => 0 이면 모두 숨김)
    [SerializeField] private GameObject[] _plants;

    // 책장 (override BookcaseOpen, 핫픽스)
    [SerializeField] private Cabinet _cabinet;

    [Header("이상현상")]
    // 나올 수 있는 이상현상 (각 물건에 붙은 컴포넌트, 루프마다 하나를 골라 켬)
    [SerializeField] private Anomaly[] _anomalies;

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
    private Anomaly _lastAnomaly;
    private int _count;
    private int _normalStreak;
    private int _bugStreak;
    private bool _isArmed;
    private bool _isCleared;
    private Coroutine _hotfixRoutine;

    // 06:00 이후에는 동료들이 먼저 내려간 뒤에만 엘리베이터를 쓸 수 있음 (퇴근 연출과 문이 겹치지 않게)
    // 밤에는 엘리베이터가 잠겨 있음 (롤백은 이제 내려가는 계단이 맡음, 엘리베이터는 퇴근 전용)
    public bool CanUseElevator => _isCleared && _ending.CanLeave;

    private void Start()
    {
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

        // 이상현상을 코드에 반영해 정상으로 만듦 (책장 문이 열린 버그 → BookcaseOpen 추가)
        if (build.Anomaly != null && build.Anomaly.Data.CanBeLegitimized)
        {
            candidates.Add((HotfixKind.Legitimize, build.Anomaly.Data.LegitimizeOverride));
        }

        // 이상현상과 함께 나오면 안 되는 override는 핫픽스로도 바꾸지 않음 (조명 명세 불일치는 Blue 줄이 있어야 성립 등)
        if (build.LightOverride == LightOverride.None)
        {
            AddHotfixCandidate(candidates, build, HotfixKind.AddLine, OverrideKind.RedLight);
            AddHotfixCandidate(candidates, build, HotfixKind.AddLine, OverrideKind.BlueLight);
        }
        else
        {
            AddHotfixCandidate(candidates, build, HotfixKind.RemoveLine, build.LightOverride == LightOverride.Red ? OverrideKind.RedLight : OverrideKind.BlueLight);
        }

        AddHotfixCandidate(candidates, build, build.HasNoColleague ? HotfixKind.RemoveLine : HotfixKind.AddLine, OverrideKind.NoColleague);
        AddHotfixCandidate(candidates, build, build.HasNoPlants ? HotfixKind.RemoveLine : HotfixKind.AddLine, OverrideKind.NoPlants);
        AddHotfixCandidate(candidates, build, build.IsCabinetOpen ? HotfixKind.RemoveLine : HotfixKind.AddLine, OverrideKind.Bookcase);

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

    private static void AddHotfixCandidate(List<(HotfixKind Kind, OverrideKind Target)> candidates, OfficeBuild build, HotfixKind kind, OverrideKind target)
    {
        if (IsBlocked(build, target)) return;

        candidates.Add((kind, target));
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
                    _cabinet.Open();
                }
                else
                {
                    _cabinet.Close();
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
                return !build.IsHotfixApplied || build.Anomaly != null;
            default:
                return build.Anomaly != null;
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
        return isHotfixBug ? $"핫픽스 미반영 ({GetOverrideLabel(_currentBuild.HotfixTarget)})" : GetAnomalyLabel();
    }

    private string GetAnomalyLabel() => _currentBuild.Anomaly == null ? string.Empty : _currentBuild.Anomaly.Data.ReportLabel;

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

        // 코드에 특정 조명이 적혀 있어야 성립하는 이상현상 (조명 명세 불일치 → Blue)
        if (build.Anomaly.Data.RequiredLight != LightOverride.None)
        {
            build.LightOverride = build.Anomaly.Data.RequiredLight;
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
    private Anomaly PickAnomaly()
    {
        Anomaly anomaly;
        do
        {
            anomaly = _anomalies[Random.Range(0, _anomalies.Length)];
        }
        while (anomaly == _lastAnomaly && _anomalies.Length > 1);

        _lastAnomaly = anomaly;
        return anomaly;
    }

    // 이번 이상현상과 함께 나오면 안 되는 override인지 (이상현상 데이터에 적힌 목록)
    private static bool IsBlocked(OfficeBuild build, OverrideKind kind) => build.Anomaly != null && build.Anomaly.Data.IsBlocked(kind);

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
                    if (IsBlocked(build, OverrideKind.RedLight)) continue;
                    build.LightOverride = LightOverride.Red;
                    return;
                case OverrideKind.BlueLight:
                    if (IsBlocked(build, OverrideKind.BlueLight)) continue;
                    build.LightOverride = LightOverride.Blue;
                    return;
                case OverrideKind.NoColleague:
                    if (IsBlocked(build, OverrideKind.NoColleague)) continue;
                    build.HasNoColleague = true;
                    return;
                case OverrideKind.NoPlants:
                    if (IsBlocked(build, OverrideKind.NoPlants)) continue;
                    build.HasNoPlants = true;
                    return;
                default:
                    if (IsBlocked(build, OverrideKind.Bookcase)) continue;
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
        if (build.Anomaly != null)
        {
            build.Anomaly.Apply(_count);
        }

        _colleague.gameObject.SetActive(!build.HasNoColleague);
        ApplyPropOverrides(build);
        UpdateTexts();

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
        foreach (GameObject plant in _plants)
        {
            plant.SetActive(true);
        }

        _cabinet.Close();
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
            _cabinet.Open();
        }
    }

    private void ApplyLightColor(OfficeBuild build) => _lights.SetAll(GetOverrideColor(build.LightOverride));

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

    // 가까이 가야 모니터 글자가 켜짐
    private void UpdateMonitorWake()
    {
        foreach (TextMesh monitorText in _monitorTexts)
        {
            // 높이는 빼고 바닥 위 거리만 잼 (벽 위쪽 모니터와 발 위치의 높이 차 때문에 바로 밑에서도 꺼지지 않게)
            Vector3 offset = monitorText.transform.position - _player.position;
            offset.y = 0f;
            monitorText.gameObject.SetActive(offset.magnitude <= _monitorWakeDistance);
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
        public Anomaly Anomaly;

        // 근무 중 배포된 긴급 핫픽스 (사무실은 그대로, 코드만 바뀜)
        public HotfixKind Hotfix;
        public OverrideKind HotfixTarget;
        public bool IsHotfixApplied;
    }
}
