using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 게임 전체 흐름(타이틀, 루프 판정, 클리어, 엔딩) 관리
public class GameFlowManager : MonoBehaviour
{
    private const int TargetStreak = 8;
    private const int StartHour = 22;
    private const int OriginOfficeNumber = 0;
    private const int RecentCaseMemory = 6;
    private const float TitleBackdropAlpha = 0.75f;
    private const string MergeLabel = "Merge";
    private const string FixRequestLabel = "수정 요청";
    private const string GameTitle = "8번 코딩";
    private const string OriginCode = "class Office_00 : BaseOffice { }";
    private const string RuleHint = "코드와 같으면 앞으로 걸어가 Merge   ·   다르면 뒤쪽 버튼에서 E로 수정 요청";

    private static readonly string[] CreditLines =
    {
        "기획 · 개발\n정승원",
        "Made with Unity",
        "플레이해 주셔서 감사합니다",
    };

    [Header("데이터")]
    // 출제할 문제 목록
    [SerializeField] private OfficeCaseDatabase _caseDatabase;

    [Header("사무실")]
    [SerializeField] private OfficeController _office;
    [SerializeField] private CodeMonitorView _codeMonitor;
    [SerializeField] private WallClock _clock;
    [SerializeField] private BugReportTerminal _bugReportTerminal;

    // 뒤로 가는 판정(수정 요청) 버튼
    [SerializeField] private SubmitButton _submitButton;

    // 앞으로 가는 판정(Merge) 영역
    [SerializeField] private PlayerZoneTrigger _mergeZone;

    [Header("엘리베이터")]
    [SerializeField] private ElevatorDoor _elevatorDoor;

    // 클리어 후 플레이어가 타면 엔딩이 시작되는 영역
    [SerializeField] private PlayerZoneTrigger _elevatorZone;

    // 시니어가 자리에서 일어나 처음 걸어갈 지점
    [SerializeField] private Transform _seniorStandPoint;

    // 직원이 엘리베이터까지 걷는 경유 지점
    [SerializeField] private Transform[] _leavePathPoints;

    // 엔딩에서 플레이어가 서 있을 엘리베이터 안 위치
    [SerializeField] private Transform _elevatorInsidePoint;

    [Header("플레이어")]
    [SerializeField] private FirstPersonController _player;
    [SerializeField] private Transform _playerStartPoint;

    // 타이틀에서 시작할 때 쓰는 상호작용 입력
    [SerializeField] private InputActionReference _interactAction;

    [Header("UI")]
    [SerializeField] private ScreenOverlayView _overlay;

    [Header("연출 시간(초)")]
    // 화면이 어두워지거나 밝아지는 시간
    [SerializeField] private float _fadeDuration = 0.6f;

    // 스토리 문장 하나를 보여 주는 시간
    [SerializeField] private float _storyLineDuration = 2.6f;

    // 알림 문구를 보여 주는 시간
    [SerializeField] private float _messageDuration = 4f;

    // 클리어 후 직원이 퇴근을 시작하기까지 대기
    [SerializeField] private float _clearLeaveDelay = 4f;

    // 엘리베이터가 움직이는 동안의 암전 시간
    [SerializeField] private float _elevatorRideDuration = 2f;

    // 엔딩에서 문이 열린 사무실을 보여 주는 시간
    [SerializeField] private float _endingViewDuration = 6f;

    // 크레딧 한 장을 보여 주는 시간
    [SerializeField] private float _creditDuration = 3f;

    private readonly List<int> _recentCaseIndices = new();
    private OfficeCase _currentCase;
    private GameState _state;
    private int _streak;
    private int _currentOfficeNumber;
    private int _reportCount;
    private bool _isStartRequested;

    private void OnEnable()
    {
        _mergeZone.OnPlayerEntered += HandleMergeZoneEntered;
        _submitButton.OnPressed += HandleSubmitPressed;
        _elevatorZone.OnPlayerEntered += HandleElevatorZoneEntered;
        _interactAction.action.performed += HandleInteractPerformed;
    }

    private void Start() => StartCoroutine(TitleRoutine());

    private void OnDisable()
    {
        _mergeZone.OnPlayerEntered -= HandleMergeZoneEntered;
        _submitButton.OnPressed -= HandleSubmitPressed;
        _elevatorZone.OnPlayerEntered -= HandleElevatorZoneEntered;
        _interactAction.action.performed -= HandleInteractPerformed;
    }

    private void HandleMergeZoneEntered()
    {
        if (_state != GameState.Playing) return;

        StartCoroutine(JudgeRoutine(false));
    }

    private void HandleSubmitPressed()
    {
        if (_state != GameState.Playing) return;

        StartCoroutine(JudgeRoutine(true));
    }

    private void HandleElevatorZoneEntered()
    {
        if (_state != GameState.Clear) return;

        StartCoroutine(EndingRoutine());
    }

    private void HandleInteractPerformed(InputAction.CallbackContext context)
    {
        if (_state != GameState.Title) return;

        _isStartRequested = true;
    }

    // 타이틀: 원본 사무실을 배경으로 시작 입력 대기
    private IEnumerator TitleRoutine()
    {
        _state = GameState.Title;
        ResetRun();
        PrepareOffice(null);
        _elevatorDoor.SetOpenImmediate(false);
        _player.SetInputEnabled(false);
        _player.Teleport(_playerStartPoint.position, _playerStartPoint.eulerAngles.y);

        _overlay.SetCrosshairVisible(false);
        _overlay.HideHint();
        _overlay.HidePrompt();
        _overlay.SetFade(TitleBackdropAlpha);
        _overlay.ShowCenter(GameTitle, "E 키를 눌러 시작");

        _isStartRequested = false;
        yield return new WaitUntil(() => _isStartRequested);

        yield return IntroRoutine();
    }

    // 인트로: 출시 전날 밤 10시, 낯선 코드
    private IEnumerator IntroRoutine()
    {
        _state = GameState.Intro;
        _overlay.HideCenter();
        yield return _overlay.FadeTo(1f, _fadeDuration);

        yield return ShowStoryLine("출시 전날 밤 10시.");
        yield return ShowStoryLine("야근 중인 QA 테스터의 모니터에\n낯선 코드가 뜬다.");
        yield return ShowStoryLine(OriginCode);

        yield return _overlay.FadeTo(0f, _fadeDuration * 2f);
        _overlay.ShowMessage("알 수 없는 이유로, 이제 사무실 자체를 QA해야 한다.", _messageDuration);
        BeginPlaying();
    }

    // 판정 후 다음 사무실로 이동(Office_00은 판정 없이 통과)
    private IEnumerator JudgeRoutine(bool isFixRequested)
    {
        _state = GameState.Transition;
        _submitButton.SetAvailable(false);
        _player.SetInputEnabled(false);
        _overlay.HidePrompt();

        bool isCorrect = _currentCase == null || Evaluate(isFixRequested);

        yield return _overlay.FadeTo(1f, _fadeDuration);

        if (_streak >= TargetStreak)
        {
            yield return ClearRoutine();
            yield break;
        }

        PrepareOffice(isCorrect ? PickNextCase() : null);
        _player.Teleport(_playerStartPoint.position, _playerStartPoint.eulerAngles.y);
        yield return _overlay.FadeTo(0f, _fadeDuration);

        if (!isCorrect)
            _overlay.ShowMessage("다시 밤 10시. 무엇이 틀렸는지는 버그 리포트 컴퓨터에서 확인할 수 있다.", _messageDuration);

        BeginPlaying();
    }

    // 클리어: 아침 6시, 시계가 흐르고 동료들이 퇴근
    private IEnumerator ClearRoutine()
    {
        _state = GameState.Clear;
        PrepareOffice(null);
        _codeMonitor.ShowOfficeCode(TargetStreak, "// 출시 준비 완료");
        _player.Teleport(_playerStartPoint.position, _playerStartPoint.eulerAngles.y);

        yield return _overlay.FadeTo(0f, _fadeDuration * 2f);
        _player.SetInputEnabled(true);
        _overlay.SetCrosshairVisible(true);
        _overlay.HideHint();
        _overlay.ShowMessage("아침 6시. 출시일이다.", _messageDuration);
        _clock.StartRunning();

        yield return new WaitForSeconds(_clearLeaveDelay);
        _elevatorDoor.Open();
        _office.Senior.LeaveThrough(BuildLeavePath(_seniorStandPoint));
        _office.Coworker.LeaveThrough(BuildLeavePath(null));

        yield return new WaitUntil(() => !_office.Senior.gameObject.activeSelf && !_office.Coworker.gameObject.activeSelf);
        _overlay.ShowMessage("동료들이 먼저 퇴근했다. 엘리베이터를 타자.", _messageDuration);
    }

    // 엔딩: 엘리베이터 문이 열리면 다시 밤 10시의 사무실
    private IEnumerator EndingRoutine()
    {
        _state = GameState.Ending;
        _player.SetInputEnabled(false);
        _overlay.SetCrosshairVisible(false);
        _overlay.HidePrompt();
        _elevatorDoor.Close();
        yield return new WaitWhile(() => _elevatorDoor.IsMoving);

        yield return _overlay.FadeTo(1f, _fadeDuration);
        yield return new WaitForSeconds(_elevatorRideDuration);

        ResetRun();
        PrepareOffice(null);
        _player.Teleport(_elevatorInsidePoint.position, _elevatorInsidePoint.eulerAngles.y);
        yield return _overlay.FadeTo(0f, _fadeDuration);

        _elevatorDoor.Open();
        yield return new WaitForSeconds(_endingViewDuration);

        yield return _overlay.FadeTo(1f, _fadeDuration * 2f);
        yield return ShowStoryLine(OriginCode);
        yield return ShowStoryLine(GameTitle);

        foreach (string line in CreditLines)
        {
            yield return ShowStoryLine(line, _creditDuration);
        }

        StartCoroutine(TitleRoutine());
    }

    private IEnumerator ShowStoryLine(string line) => ShowStoryLine(line, _storyLineDuration);

    // 검은 화면 가운데에 문장 하나 표시
    private IEnumerator ShowStoryLine(string line, float duration)
    {
        _overlay.ShowCenter(line, string.Empty);
        yield return new WaitForSeconds(duration);
        _overlay.HideCenter();
        yield return new WaitForSeconds(_fadeDuration);
    }

    private void BeginPlaying()
    {
        _state = GameState.Playing;
        _submitButton.SetAvailable(true);
        _player.SetInputEnabled(true);
        _overlay.SetCrosshairVisible(true);
        _overlay.ShowHint(RuleHint);
    }

    // 판정 결과 반영(맞으면 연속 정답 +1, 틀리면 리포트 작성 후 0)
    private bool Evaluate(bool isFixRequested)
    {
        if (isFixRequested == _currentCase.HasBug)
        {
            _streak++;
            return true;
        }

        _bugReportTerminal.SetReport(BuildReport(isFixRequested));
        _streak = 0;
        return false;
    }

    // 사무실, 코드 화면, 시계를 현재 진행도에 맞추기(null이면 원본 Office_00)
    private void PrepareOffice(OfficeCase officeCase)
    {
        _currentCase = officeCase;
        _currentOfficeNumber = officeCase == null ? OriginOfficeNumber : _streak + 1;

        _office.Build(officeCase?.Effects);
        _codeMonitor.ShowOfficeCode(_currentOfficeNumber, officeCase == null ? string.Empty : officeCase.Code);
        _clock.SetTime(StartHour + _streak, 0);
    }

    // 한 회차의 진행 기록 초기화
    private void ResetRun()
    {
        _streak = 0;
        _reportCount = 0;
        _recentCaseIndices.Clear();
        _bugReportTerminal.SetReport(string.Empty);
    }

    // 최근에 나온 문제를 피해 무작위 선택
    private OfficeCase PickNextCase()
    {
        IReadOnlyList<OfficeCase> cases = _caseDatabase.Cases;
        var candidates = new List<int>();

        for (int i = 0; i < cases.Count; i++)
        {
            if (!_recentCaseIndices.Contains(i))
                candidates.Add(i);
        }

        int index = candidates[Random.Range(0, candidates.Count)];
        _recentCaseIndices.Add(index);
        if (_recentCaseIndices.Count > RecentCaseMemory)
            _recentCaseIndices.RemoveAt(0);

        return cases[index];
    }

    private string BuildReport(bool isFixRequested)
    {
        _reportCount++;
        string submitted = isFixRequested ? FixRequestLabel : MergeLabel;
        string answer = _currentCase.HasBug ? FixRequestLabel : MergeLabel;
        string category = _currentCase.HasBug ? GetCategoryLabel(_currentCase.Category) : "정상";

        return $"BUG REPORT #{_reportCount}\n\nOffice_{_currentOfficeNumber:00}\n제출 : {submitted}   정답 : {answer}\n\n[{category}]\n{_currentCase.Explanation}";
    }

    // 퇴근 경로 만들기(시작 지점이 있으면 맨 앞에 추가)
    private Vector3[] BuildLeavePath(Transform startPoint)
    {
        var path = new List<Vector3>();
        if (startPoint != null)
            path.Add(startPoint.position);

        foreach (Transform point in _leavePathPoints)
        {
            path.Add(point.position);
        }

        return path.ToArray();
    }

    private static string GetCategoryLabel(AnomalyCategory category) => category switch
    {
        AnomalyCategory.OriginalDeviation => "원본 이탈",
        AnomalyCategory.SpecMismatch => "명세 불일치",
        AnomalyCategory.CodingKnowledge => "코딩 지식",
        _ => "버그",
    };
}
