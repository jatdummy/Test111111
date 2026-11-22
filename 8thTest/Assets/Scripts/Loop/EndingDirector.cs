using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 06:00 퇴근 연출: 창으로 햇빛이 들고, 동료들이 인사하며 엘리베이터로 먼저 퇴근
// 플레이어가 엘리베이터를 타면 다시 22:00 사무실이 보인 뒤 타이틀로 돌아감 (씬을 다시 불러 타이틀 화면부터)
public class EndingDirector : MonoBehaviour
{
    private const float SpeechHeight = 2.05f;
    private const float MinFaceDistance = 0.01f;

    // 말풍선 끝과 벽 사이에 남길 여유(m)
    private const float SpeechWallMargin = 0.1f;

    // 사무실 안으로 보는 경계 (이 높이보다 위, 이 z보다 안쪽, 이 x보다 앞쪽)
    // x 경계가 없으면 사무실 끝 너머 승인 계단실 꼭대기도 사무실로 잘못 봄
    private const float OfficeFloorMinY = -0.5f;
    private const float OfficeMinZ = -3.4f;
    private const float OfficeMaxX = 35f;

    [Header("창문")]
    // 창 밖 배경판
    [SerializeField] private Renderer[] _windowOutsides;

    // 밤 창 밖 재질
    [SerializeField] private Material _nightMaterial;

    // 아침 창 밖 재질
    [SerializeField] private Material _morningMaterial;

    // 창으로 들어오는 햇빛 (아침에만 켜짐)
    [SerializeField] private Light[] _sunlights;

    // 아침에 밝기를 줄일 천장 조명
    [SerializeField] private Light[] _ceilingLights;

    // 아침 천장 조명 밝기 배율 (햇빛이 잘 보이게)
    [SerializeField, Range(0f, 1f)] private float _morningCeilingScale = 0.45f;

    [Header("퇴근")]
    // 플레이어 (사무실에 올라왔는지, 인사할 때 바라볼 대상)
    [SerializeField] private Transform _player;

    // 사무실에 올라오면 조작을 멈추고 시점 · 이동을 맡길 플레이어
    [SerializeField] private FirstPersonController _playerController;

    // 먼저 퇴근하는 동료들
    [SerializeField] private Leaver[] _leavers;

    // 걷기 속도(초당 거리)
    [SerializeField, Min(0f)] private float _walkSpeed = 0.85f;

    // 걸으며 방향을 트는 속도(초당 각도, 꺾이는 지점에서 한 번에 휙 돌지 않게)
    [SerializeField, Min(0f)] private float _turnSpeed = 360f;

    // 인사하며 멈춰 있는 시간(초)
    [SerializeField, Min(0f)] private float _greetSeconds = 2.5f;

    // 앞사람 인사가 끝나고 다음 사람이 인사하기까지 기다리는 시간(초, 인사가 겹치거나 바로 이어지지 않게)
    [SerializeField, Min(0f)] private float _greetGapSeconds = 1f;

    // 엘리베이터 문짝 (동료들이 타면 닫힘)
    [SerializeField] private Elevator _elevator;

    // 연출이 시작되면 내릴 플레이어 태블릿 (햇빛 · 동료들이 가려지지 않게)
    [SerializeField] private CodeTablet _tablet;

    // 동료들을 태우고 문이 닫혀 있는 시간(초)
    [SerializeField, Min(0f)] private float _doorClosedSeconds = 2f;

    [Header("플레이어 연출")]
    // 사무실에 들어선 뒤 걸어 나올 자리 (사무실 전체와 코드판 앞 동료들이 넓게 보이는 곳)
    [SerializeField] private Vector3 _viewPoint;

    // 동료들이 06:00에 서서 바라보고 있는 코드판
    [SerializeField] private Transform _codeBoard;

    // 플레이어가 오면 동료들이 코드판에서 뒤돌아 플레이어를 보는 시간(초)
    [SerializeField, Min(0f)] private float _turnBackSeconds = 1f;

    // 시점이 동료들을 따라 도는 속도(초당 각도, 걷는 동료들을 천천히 따라감)
    [SerializeField, Min(0f)] private float _lookSpeed = 60f;

    // 바라보는 높이 (동료 머리, 걸어갈 앞쪽)
    [SerializeField, Min(0f)] private float _lookHeight = 1.5f;

    // 엘리베이터로 걸어 들어가는 속도(초당 거리)
    [SerializeField, Min(0f)] private float _playerWalkSpeed = 1.2f;

    // 빈 엘리베이터로 걸어 들어가는 경로 (마지막 지점이 엘리베이터 안)
    [SerializeField] private Vector3[] _elevatorPath;

    // 들어간 뒤 바라볼 엘리베이터 스위치
    [SerializeField] private Transform _elevatorSwitch;

    // 스위치 쪽으로 돌아보는 시간(초)
    [SerializeField, Min(0f)] private float _faceSwitchSeconds = 1.5f;

    // 스위치를 누르라는 [E] 안내 (엘리베이터 안에 들어가면 보임)
    [SerializeField] private GameObject _ridePrompt;

    [Header("마무리")]
    // 22:00 사무실을 보여 주는 시간(초)
    [SerializeField, Min(0f)] private float _nightHoldSeconds = 3.5f;

    // 화면을 덮는 검은 판
    [SerializeField] private Image _fade;

    // 검게 덮이는 시간(초)
    [SerializeField, Min(0.01f)] private float _fadeSeconds = 2f;

    private float[] _ceilingIntensities;
    private bool _isPlaying;

    // 지금 인사할 차례인 동료 순번 (_leavers 순서대로 한 명씩)
    private int _greetTurn;

    // 다음 동료가 인사를 시작해도 되는 시각
    private float _nextGreetTime;

    // 동료들이 모두 퇴근해야 플레이어가 엘리베이터를 탈 수 있음
    public bool CanLeave { get; private set; }

    private void Awake() => CacheCeilingIntensities();

    private void Start()
    {
        SetMorning(false);
        HideSpeeches();
        _ridePrompt.SetActive(false);
        SetFade(0f);
    }

    // 06:00 도달
    public void BeginMorning()
    {
        if (_isPlaying) return;

        _isPlaying = true;
        CanLeave = false;
        _tablet.Close();
        // 동료들은 자리 대신 코드판 앞에 서서 코드판을 보고 있음 (플레이어가 계단을 오르는 동안 옮겨서 보이지 않음)
        foreach (Leaver leaver in _leavers)
        {
            leaver.CachePose();
            leaver.StandAtBoard(_codeBoard.position);
        }

        SetMorning(true);
        StartCoroutine(FarewellRoutine());
    }

    // 엘리베이터를 타고 내려가면 다시 밤 사무실, 잠시 뒤 타이틀로
    public void PlayReturn()
    {
        StopAllCoroutines();
        _tablet.Close();
        _elevator.HoldOpen(false);
        _elevator.HoldClosed(false);
        SetMorning(false);
        HideSpeeches();
        _ridePrompt.SetActive(false);
        foreach (Leaver leaver in _leavers)
        {
            leaver.RestorePose();
        }

        StartCoroutine(FadeToTitleRoutine());
    }

    private void CacheCeilingIntensities()
    {
        _ceilingIntensities = new float[_ceilingLights.Length];
        for (int i = 0; i < _ceilingLights.Length; i++)
        {
            _ceilingIntensities[i] = _ceilingLights[i].intensity;
        }
    }

    private void SetMorning(bool isMorning)
    {
        foreach (Renderer outside in _windowOutsides)
        {
            outside.sharedMaterial = isMorning ? _morningMaterial : _nightMaterial;
        }

        foreach (Light sunlight in _sunlights)
        {
            sunlight.enabled = isMorning;
        }

        float scale = isMorning ? _morningCeilingScale : 1f;
        for (int i = 0; i < _ceilingLights.Length; i++)
        {
            _ceilingLights[i].intensity = _ceilingIntensities[i] * scale;
        }
    }

    private IEnumerator FarewellRoutine()
    {
        // 플레이어가 계단을 올라와 사무실에 들어올 때까지 기다림
        yield return new WaitUntil(IsPlayerInOffice);

        // 사무실에 들어서면 조작을 멈추고, 사무실이 넓게 보이는 자리까지 걸어 나옴
        _playerController.IsDirected = true;
        yield return PlayerWalkRoutine(_viewPoint);

        // 플레이어가 오면 동료들이 뒤돌아 플레이어를 봄 (인사는 각자 엘리베이터를 타기 직전에, 코드판 글자와 겹치지 않게)
        yield return TurnBackRoutine();

        // 동료들이 들어갈 수 있게 엘리베이터 문을 열어 둠, 시점은 걸어가는 동료들을 따라감
        _elevator.HoldOpen(true);

        int arrivedCount = 0;
        _greetTurn = 0;
        _nextGreetTime = 0f;
        for (int i = 0; i < _leavers.Length; i++)
        {
            StartCoroutine(LeaveRoutine(_leavers[i], i, () => arrivedCount++));
        }

        while (arrivedCount < _leavers.Length)
        {
            _playerController.TurnViewTowards(GetLeaversCenter(), _lookSpeed);
            yield return null;
        }

        // 모두 타면 문이 닫히고, 다시 열렸을 때는 아무도 없음
        _elevator.HoldOpen(false);
        _elevator.HoldClosed(true);
        yield return new WaitUntil(() => _elevator.IsDoorClosed);
        yield return new WaitForSeconds(_doorClosedSeconds);

        foreach (Leaver leaver in _leavers)
        {
            leaver.Body.gameObject.SetActive(false);
        }

        _elevator.HoldClosed(false);

        // 빈 엘리베이터 문이 다시 열리면 플레이어도 따라 들어가 스위치 앞에 섬
        _elevator.HoldOpen(true);
        yield return WalkIntoElevatorRoutine();

        // 엘리베이터에 타면 조작을 돌려줌 (스위치를 직접 누르고, 문이 다시 열리면 밤 사무실을 둘러볼 수 있게)
        _playerController.IsDirected = false;
        CanLeave = true;
        _ridePrompt.SetActive(true);

        // 플레이어가 직접 스위치를 누르면 안내를 숨김 (내려간 뒤는 PlayReturn이 이어서 맡음)
        yield return new WaitUntil(() => _elevator.IsRiding);
        _ridePrompt.SetActive(false);
    }

    private IEnumerator WalkIntoElevatorRoutine()
    {
        foreach (Vector3 point in _elevatorPath)
        {
            yield return PlayerWalkRoutine(point);
        }

        float elapsed = 0f;
        while (elapsed < _faceSwitchSeconds)
        {
            _playerController.TurnViewTowards(_elevatorSwitch.position, _lookSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // 걸어갈 앞쪽을 보며 지점까지 걸음
    private IEnumerator PlayerWalkRoutine(Vector3 point)
    {
        while (!_playerController.WalkTowards(point, _playerWalkSpeed))
        {
            _playerController.TurnViewTowards(point + Vector3.up * _lookHeight, _lookSpeed);
            yield return null;
        }
    }

    // 동료들은 플레이어 쪽으로 돌아서고, 시점은 동료들 쪽으로
    private IEnumerator TurnBackRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _turnBackSeconds)
        {
            foreach (Leaver leaver in _leavers)
            {
                NpcMotion.TurnTowards(leaver.Body, _player.position - leaver.Body.position, _turnSpeed);
            }

            _playerController.TurnViewTowards(GetLeaversCenter(), _lookSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // 동료들의 가운데 머리 높이 (시점이 따라갈 곳)
    private Vector3 GetLeaversCenter()
    {
        Vector3 sum = Vector3.zero;
        foreach (Leaver leaver in _leavers)
        {
            sum += leaver.Body.position;
        }

        return sum / _leavers.Length + Vector3.up * _lookHeight;
    }

    private void HideSpeeches()
    {
        foreach (Leaver leaver in _leavers)
        {
            leaver.Speech.gameObject.SetActive(false);
        }
    }

    // 계단(바닥 아래, 승인 계단실)이 아니라 사무실 바닥에 올라와 있으면 사무실 안
    private bool IsPlayerInOffice() => _player.position.y > OfficeFloorMinY && _player.position.z > OfficeMinZ && _player.position.x < OfficeMaxX;

    private IEnumerator LeaveRoutine(Leaver leaver, int order, System.Action onArrived)
    {
        yield return new WaitForSeconds(leaver.StartDelay);

        leaver.Npc.Walk(true);
        for (int i = 0; i < leaver.Path.Length; i++)
        {
            // 엘리베이터에 들어가기 직전에 멈춰서 플레이어에게 인사
            // 앞사람이 인사를 마치고 엘리베이터로 걸어간 뒤 잠시 기다렸다가 차례대로 인사
            if (i == leaver.Path.Length - 1)
            {
                yield return WaitGreetTurnRoutine(leaver, order);
                yield return GreetRoutine(leaver);
                _greetTurn++;
                _nextGreetTime = Time.time + _greetGapSeconds;
            }

            yield return WalkToRoutine(leaver, leaver.Path[i]);
        }

        leaver.Npc.Walk(false);
        onArrived();
    }

    private IEnumerator WalkToRoutine(Leaver leaver, Vector3 target)
    {
        while (!NpcMotion.StepTowards(leaver.Body, target, _walkSpeed))
        {
            NpcMotion.TurnTowards(leaver.Body, target - leaver.Body.position, _turnSpeed);
            yield return null;
        }
    }

    // 인사할 차례가 올 때까지 멈춰 서서 플레이어 쪽을 보고 기다림
    private IEnumerator WaitGreetTurnRoutine(Leaver leaver, int order)
    {
        leaver.Npc.Walk(false);
        while (_greetTurn != order || Time.time < _nextGreetTime)
        {
            NpcMotion.TurnTowards(leaver.Body, _player.position - leaver.Body.position, _turnSpeed);
            yield return null;
        }
    }

    private IEnumerator GreetRoutine(Leaver leaver)
    {
        // 멈춰 서서 플레이어 쪽으로 돌아 인사
        leaver.Npc.Walk(false);
        leaver.Speech.text = leaver.Greeting;
        leaver.Speech.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < _greetSeconds)
        {
            NpcMotion.TurnTowards(leaver.Body, _player.position - leaver.Body.position, _turnSpeed);
            ShowSpeechAbove(leaver);
            elapsed += Time.deltaTime;
            yield return null;
        }

        leaver.Speech.gameObject.SetActive(false);
        leaver.Npc.Walk(true);
    }

    // 동료마다 자기 말풍선을 머리 위에 두고 플레이어 쪽을 보게 함 (두 사람이 겹쳐 인사해도 말이 끊기지 않게)
    private void ShowSpeechAbove(Leaver leaver)
    {
        Transform speech = leaver.Speech.transform;
        speech.position = leaver.Body.position + Vector3.up * SpeechHeight;
        Vector3 direction = speech.position - _player.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > MinFaceDistance)
        {
            speech.rotation = Quaternion.LookRotation(direction);
        }

        KeepSpeechOffWalls(leaver);
    }

    // 말풍선 양 끝이 벽 안으로 들어가면 벽 반대쪽으로 밀어 냄 (벽 가까이서 인사해도 글자가 가려지지 않게)
    private static void KeepSpeechOffWalls(Leaver leaver)
    {
        Transform speech = leaver.Speech.transform;
        float halfWidth = leaver.SpeechRenderer.localBounds.extents.x * speech.lossyScale.x + SpeechWallMargin;
        Vector3 center = speech.position;
        center = PushFromWall(center, speech.right, halfWidth);
        center = PushFromWall(center, -speech.right, halfWidth);
        speech.position = center;
    }

    // 가운데에서 한쪽 끝까지 사이에 벽이 있으면 그만큼 반대로 옮긴 위치
    private static Vector3 PushFromWall(Vector3 center, Vector3 direction, float halfWidth)
    {
        if (!Physics.Raycast(center, direction, out RaycastHit hit, halfWidth, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return center;
        }

        return center - direction * (halfWidth - hit.distance);
    }

    private IEnumerator FadeToTitleRoutine()
    {
        yield return new WaitForSeconds(_nightHoldSeconds);

        float elapsed = 0f;
        while (elapsed < _fadeSeconds)
        {
            elapsed += Time.deltaTime;
            SetFade(elapsed / _fadeSeconds);
            yield return null;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void SetFade(float alpha)
    {
        Color color = _fade.color;
        color.a = Mathf.Clamp01(alpha);
        _fade.color = color;
    }

    // 먼저 퇴근하는 동료 한 명
    [System.Serializable]
    private class Leaver
    {
        // 동료 본체
        [SerializeField, FormerlySerializedAs("Body")] private Transform _body;

        // 걷기 · 서기 · 타자 동작
        [SerializeField, FormerlySerializedAs("Npc")] private NpcBody _npc;

        // 06:00에 서서 코드판을 보고 있을 자리
        [SerializeField] private Vector3 _boardPosition;

        // 출발 전 기다리는 시간(초)
        [SerializeField, Min(0f), FormerlySerializedAs("StartDelay")] private float _startDelay;

        // 엘리베이터 안까지 걷는 경로 (마지막 지점 직전에 인사)
        [SerializeField, FormerlySerializedAs("Path")] private Vector3[] _path;

        // 플레이어에게 건네는 인사
        [SerializeField, FormerlySerializedAs("Greeting")] private string _greeting;

        // 인사 말풍선 글자 (이 동료 머리 위에 뜸)
        [SerializeField] private TextMesh _speech;

        private Vector3 _position;
        private Quaternion _rotation;
        private bool _isTyping;
        private Renderer _speechRenderer;

        public Transform Body => _body;
        public NpcBody Npc => _npc;
        public float StartDelay => _startDelay;
        public Vector3[] Path => _path;
        public string Greeting => _greeting;
        public TextMesh Speech => _speech;

        // 말풍선 글자의 폭을 잴 렌더러 (처음 쓸 때 한 번 찾아 둠)
        public Renderer SpeechRenderer
        {
            get
            {
                if (_speechRenderer == null)
                {
                    _speechRenderer = _speech.GetComponent<Renderer>();
                }

                return _speechRenderer;
            }
        }

        // 코드판 앞에 서서 코드판을 바라봄
        public void StandAtBoard(Vector3 boardPoint)
        {
            Vector3 direction = boardPoint - _boardPosition;
            direction.y = 0f;
            _body.SetPositionAndRotation(_boardPosition, Quaternion.LookRotation(direction));
            _npc.StandUp();
        }

        public void CachePose()
        {
            _position = _body.position;
            _rotation = _body.rotation;
            _isTyping = _npc.IsTyping;
        }

        // 다시 밤이 되면 원래 자리로
        public void RestorePose()
        {
            // 다시 켜면 Animator가 처음 상태로 돌아가므로, 켠 뒤에 자세를 다시 정함
            _body.gameObject.SetActive(true);
            _body.SetPositionAndRotation(_position, _rotation);
            if (_isTyping)
            {
                _npc.StartTyping();
            }
            else
            {
                _npc.StandUp();
            }
        }
    }
}
