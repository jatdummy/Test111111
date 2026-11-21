using UnityEngine;

// 루프 진행과 판정: 트리거를 받아 빌드를 뽑고(BuildGenerator), 사무실(OfficeState)과 모니터(CodeMonitor)에 적용한 뒤 판정
public class LoopManager : MonoBehaviour
{
    [Header("루프")]
    // 앞으로 통과했을 때 플레이어를 옮길 거리
    [SerializeField] private Vector3 _forwardTeleportOffset = new(-34.25f, -3.2f, -6.2f);

    // 롤백 계단을 내려가다 통과했을 때 플레이어를 옮길 거리 (롤백 계단실 → 시작 계단실 위층)
    [SerializeField] private Vector3 _rollbackTeleportOffset = new(11f, 3.2f, -4.1f);

    // 두 경로가 함께 도착하는 시작 계단실 (들어온 경로의 계단실과 똑같이 보이게 셔터를 바꿈)
    [SerializeField] private StairwellRouteView _arrivalStairwell;

    [Header("루프 구성 요소")]
    // 빌드대로 사무실을 맞추는 쪽 (조명, 이상현상, 소품)
    [SerializeField] private OfficeState _office;

    // 빌드 코드를 보여 주는 모니터와 시계
    [SerializeField] private CodeMonitor _monitor;

    // 근무 중 긴급 핫픽스
    [SerializeField] private HotfixScheduler _hotfix;

    [Header("단말 · 태블릿")]
    // 판정 기록을 남길 검수 로그 단말 (코드판에는 결과를 띄우지 않음)
    [SerializeField] private QaLogTerminal _qaLog;

    // 긴급 핫픽스로 코드가 바뀌면 알림을 띄울 플레이어 태블릿
    [SerializeField] private CodeTablet _tablet;

    // 06:00 퇴근 연출 (햇빛, 동료 퇴근, 엘리베이터 반전)
    [SerializeField] private EndingDirector _ending;

    private BuildGenerator _generator;
    private OfficeBuild _currentBuild;
    private int _count;
    private bool _isArmed;
    private bool _isCleared;

    // 06:00 이후에는 동료들이 먼저 내려간 뒤에만 엘리베이터를 쓸 수 있음 (퇴근 연출과 문이 겹치지 않게)
    // 밤에는 엘리베이터가 잠겨 있음 (롤백은 이제 내려가는 계단이 맡음, 엘리베이터는 퇴근 전용)
    public bool CanUseElevator => _isCleared && _ending.CanLeave;

    private void Awake() => _generator = new BuildGenerator(_office.Anomalies);

    private void OnEnable() => _hotfix.OnHotfixDeployed += HandleHotfixDeployed;

    private void Start()
    {
        ApplyBuild(OfficeBuild.CreateOriginal());

        // QA 수칙은 코드판이 아니라 출근하자마자 태블릿 수칙 페이지로 보냄
        _tablet.SetRules(_monitor.BuildRulesText());
    }

    private void OnDisable() => _hotfix.OnHotfixDeployed -= HandleHotfixDeployed;

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
                TeleportForward(player);
            }

            if (type == LoopTriggerType.Rollback)
            {
                TeleportRollback(player);
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
                TeleportForward(player);
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

                TeleportRollback(player);
                break;
        }
    }

    // 통로에 들어오면 판정 대기 상태로 전환
    private void EnterCorridor(FirstPersonController player)
    {
        if (_isArmed) return;

        _isArmed = true;
        _office.BeginColleagueWalk(player.transform);
        _hotfix.Schedule(_currentBuild, _count);
    }

    // 승인 계단을 올라간 플레이어를 시작 계단실로 보냄 (도착 계단실도 승인 계단실처럼 위층 계단을 막음)
    // 텔레포트와 같은 프레임에 셔터를 바꿔야 바뀌는 순간이 보이지 않음
    private void TeleportForward(FirstPersonController player)
    {
        _arrivalStairwell.ShowArrival(true);
        player.Teleport(_forwardTeleportOffset);
    }

    // 롤백 계단을 내려간 플레이어를 시작 계단실로 보냄 (도착 계단실도 롤백 계단실처럼 아래층 계단을 막음)
    private void TeleportRollback(FirstPersonController player)
    {
        _arrivalStairwell.ShowArrival(false);
        player.Teleport(_rollbackTeleportOffset);
    }

    // 핫픽스가 배포되면 바뀐 코드로 다시 판단해야 함 (절반쯤은 사무실도 같이 바뀜)
    private void HandleHotfixDeployed(OfficeBuild build)
    {
        if (!_isArmed) return;

        _currentBuild = build;
        if (build.IsHotfixApplied)
        {
            _office.ApplyHotfix(build);
        }

        _monitor.Show(_currentBuild, _count, _isCleared);
        _qaLog.AddLine(RichText.Colorize($"[{LoopTime.Format(_count)}] Office_{_currentBuild.GetNumber(_count):00}  긴급 핫픽스 배포", RichText.HotfixColor));
        _tablet.NotifyCodeChanged();
    }

    private void Judge(bool isForward)
    {
        _isArmed = false;
        _hotfix.Cancel();

        // 원본 확인 구간은 판정 없이 검수 시작
        if (_currentBuild.IsOriginal)
        {
            if (isForward)
            {
                _qaLog.AddLine(RichText.Colorize($"[{LoopTime.Format(0)}] 검수 시작", RichText.WaitingColor));
            }

            ApplyBuild(isForward ? _generator.CreateJudged() : OfficeBuild.CreateOriginal());
            return;
        }

        // 핫픽스가 배포됐으면 바뀐 코드 기준으로 판정
        bool hasBug = _currentBuild.HasBug;
        bool isCorrect = isForward != hasBug;
        // 판정 결과는 코드판이 아니라 검수 로그 단말에 남김
        LogJudgement(isForward, hasBug, isCorrect);

        if (!isCorrect)
        {
            _count = 0;
            ApplyBuild(OfficeBuild.CreateOriginal());
            return;
        }

        _count++;
        if (_count >= LoopTime.ClearCount)
        {
            _isCleared = true;
            ApplyBuild(OfficeBuild.CreateOriginal());

            // 창으로 햇빛이 들고 동료들이 먼저 퇴근
            _ending.BeginMorning();
            return;
        }

        ApplyBuild(_generator.CreateJudged());
    }

    // 클리어 뒤 퇴근하려 해도 다시 밤 10시
    private void ReturnToNight()
    {
        _isArmed = false;
        _hotfix.Cancel();
        _isCleared = false;
        _count = 0;
        ApplyBuild(OfficeBuild.CreateOriginal());

        // 엘리베이터 문이 열리면 밤 사무실, 잠시 뒤 타이틀로
        _ending.PlayReturn();
    }

    // 매 루프 원본으로 되돌린 뒤 이번 빌드를 적용
    private void ApplyBuild(OfficeBuild build)
    {
        _currentBuild = build;
        _office.Apply(build, _count);
        _monitor.Show(build, _count, _isCleared);
    }

    // 검수 로그 단말에 남길 판정 기록 (오답이면 틀린 이유)
    private void LogJudgement(bool isForward, bool hasBug, bool isCorrect)
    {
        string action = isForward ? "승인" : "롤백";
        string result = isCorrect ? RichText.Colorize("PASS", RichText.PassColor) : RichText.Colorize("FAIL", RichText.FailColor);
        _qaLog.AddLine($"[{LoopTime.Format(_count)}] Office_{_currentBuild.GetNumber(_count):00}  {action}  {result}");

        if (!isCorrect)
        {
            string reason = hasBug ? $"놓친 버그: {GetMissedLabel()}" : GetNormalLabel();
            _qaLog.AddLine(RichText.Colorize($"   └ {reason}", RichText.FailColor));

            // 오답이면 시간이 처음으로 돌아감 (롤백과 헷갈리지 않게 '초기화'로 표기)
            _qaLog.AddLine(RichText.Colorize($"── {LoopTime.Format(0)} 세션 초기화 ──", RichText.CommentColor));
            _qaLog.MarkFailure();
        }
    }

    // 놓친 버그 이름 (핫픽스를 반영하지 않은 경우 우선)
    private string GetMissedLabel()
    {
        bool isHotfixBug = _currentBuild.Hotfix is HotfixKind.AddLine or HotfixKind.RemoveLine && !_currentBuild.IsHotfixApplied;
        return isHotfixBug ? $"핫픽스 미반영 ({OverrideText.GetLabel(_currentBuild.HotfixTarget)})" : GetAnomalyLabel();
    }

    private string GetAnomalyLabel() => _currentBuild.Anomaly == null ? string.Empty : _currentBuild.Anomaly.Data.ReportLabel;

    // 정상이었는데 롤백한 경우의 설명
    private string GetNormalLabel()
    {
        if (_currentBuild.Hotfix == HotfixKind.Legitimize) return $"정상 빌드였음 (핫픽스로 {OverrideText.GetLabel(_currentBuild.HotfixTarget)} 반영)";
        if (_currentBuild.IsHotfixApplied) return $"정상 빌드였음 (핫픽스 {OverrideText.GetLabel(_currentBuild.HotfixTarget)} 사무실 반영 완료)";

        return "정상 빌드였음";
    }
}
