using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 06:00 퇴근 연출: 창으로 햇빛이 들고, 동료들이 인사하며 엘리베이터로 먼저 퇴근
// 플레이어가 엘리베이터를 타면 다시 22:00 사무실이 보인 뒤 타이틀로 돌아감
public class EndingDirector : MonoBehaviour
{
    private const string TitleSceneName = "Title";
    private const float SpeechHeight = 2.05f;
    private const float MinFaceDistance = 0.01f;

    // 사무실 안으로 보는 경계 (이 높이보다 위, 이 z보다 안쪽)
    private const float OfficeFloorMinY = -0.5f;
    private const float OfficeMinZ = -3.4f;

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

    // 먼저 퇴근하는 동료들
    [SerializeField] private Leaver[] _leavers;

    // 걷기 속도(초당 거리)
    [SerializeField, Min(0f)] private float _walkSpeed = 1.3f;

    // 걸으며 방향을 트는 속도(초당 각도, 꺾이는 지점에서 한 번에 휙 돌지 않게)
    [SerializeField, Min(0f)] private float _turnSpeed = 360f;

    // 인사하며 멈춰 있는 시간(초)
    [SerializeField, Min(0f)] private float _greetSeconds = 2.5f;

    // 인사 말풍선 글자 (말하는 동료 머리 위로 옮겨 다님)
    [SerializeField] private TextMesh _speech;

    // 엘리베이터 문짝 (동료들이 타면 닫힘)
    [SerializeField] private Elevator _elevator;

    // 연출이 시작되면 내릴 플레이어 태블릿 (햇빛 · 동료들이 가려지지 않게)
    [SerializeField] private CodeTablet _tablet;

    // 동료들을 태우고 문이 닫혀 있는 시간(초)
    [SerializeField, Min(0f)] private float _doorClosedSeconds = 2f;

    [Header("마무리")]
    // 22:00 사무실을 보여 주는 시간(초)
    [SerializeField, Min(0f)] private float _nightHoldSeconds = 3.5f;

    // 화면을 덮는 검은 판
    [SerializeField] private Image _fade;

    // 검게 덮이는 시간(초)
    [SerializeField, Min(0.01f)] private float _fadeSeconds = 2f;

    private float[] _ceilingIntensities;
    private bool _isPlaying;

    // 동료들이 모두 퇴근해야 플레이어가 엘리베이터를 탈 수 있음
    public bool CanLeave { get; private set; }

    private void Awake() => CacheCeilingIntensities();

    private void Start()
    {
        SetMorning(false);
        _speech.gameObject.SetActive(false);
        SetFade(0f);
    }

    // 06:00 도달
    public void BeginMorning()
    {
        if (_isPlaying) return;

        _isPlaying = true;
        CanLeave = false;
        _tablet.Close();
        foreach (Leaver leaver in _leavers)
        {
            leaver.CachePose();
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
        _speech.gameObject.SetActive(false);
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

        // 동료들이 들어갈 수 있게 엘리베이터 문을 열어 둠
        _elevator.HoldOpen(true);

        int arrivedCount = 0;
        foreach (Leaver leaver in _leavers)
        {
            StartCoroutine(LeaveRoutine(leaver, () => arrivedCount++));
        }

        yield return new WaitUntil(() => arrivedCount == _leavers.Length);

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
        CanLeave = true;
    }

    // 계단(바닥 아래)이 아니라 사무실 바닥에 올라와 있으면 사무실 안
    private bool IsPlayerInOffice() => _player.position.y > OfficeFloorMinY && _player.position.z > OfficeMinZ;

    private IEnumerator LeaveRoutine(Leaver leaver, System.Action onArrived)
    {
        yield return new WaitForSeconds(leaver.StartDelay);

        // 앉아 있었다면 일어남
        leaver.Npc.StandUp();
        leaver.Body.position += Vector3.up * leaver.StandRise;
        leaver.Npc.Walk(true);

        for (int i = 0; i < leaver.Path.Length; i++)
        {
            // 엘리베이터에 들어가기 직전에 멈춰서 플레이어에게 인사
            if (i == leaver.Path.Length - 1)
            {
                yield return GreetRoutine(leaver);
            }

            yield return WalkToRoutine(leaver, leaver.Path[i]);
        }

        leaver.Npc.Walk(false);
        onArrived();
    }

    private IEnumerator WalkToRoutine(Leaver leaver, Vector3 target)
    {
        while (leaver.Body.position != target)
        {
            leaver.Body.position = Vector3.MoveTowards(leaver.Body.position, target, _walkSpeed * Time.deltaTime);
            Face(leaver.Body, target);
            yield return null;
        }
    }

    private IEnumerator GreetRoutine(Leaver leaver)
    {
        // 멈춰 서서 플레이어 쪽으로 돌아 인사
        leaver.Npc.Walk(false);
        _speech.text = leaver.Greeting;
        _speech.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < _greetSeconds)
        {
            Face(leaver.Body, _player.position);
            ShowSpeechAbove(leaver.Body);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _speech.gameObject.SetActive(false);
        leaver.Npc.Walk(true);
    }

    // 말풍선을 머리 위에 두고 플레이어 쪽을 보게 함
    private void ShowSpeechAbove(Transform body)
    {
        Transform speech = _speech.transform;
        speech.position = body.position + Vector3.up * SpeechHeight;
        Vector3 direction = speech.position - _player.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > MinFaceDistance)
        {
            speech.rotation = Quaternion.LookRotation(direction);
        }
    }

    // 몸을 대상 쪽으로 조금씩 돌림
    private void Face(Transform body, Vector3 target)
    {
        Vector3 direction = target - body.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < MinFaceDistance) return;

        body.rotation = Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction), _turnSpeed * Time.deltaTime);
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
        SceneManager.LoadScene(TitleSceneName);
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
        public Transform Body;

        // 걷기 · 서기 · 타자 동작
        public NpcBody Npc;

        // 일어날 때 올라가는 높이 (앉는 애니메이션이 엉덩이를 내리므로 의자 높이 보정용, 보통 0)
        public float StandRise;

        // 출발 전 기다리는 시간(초)
        public float StartDelay;

        // 엘리베이터 안까지 걷는 경로 (마지막 지점 직전에 인사)
        public Vector3[] Path;

        // 플레이어에게 건네는 인사
        public string Greeting;

        private Vector3 _position;
        private Quaternion _rotation;
        private bool _isTyping;

        public void CachePose()
        {
            _position = Body.position;
            _rotation = Body.rotation;
            _isTyping = Npc.IsTyping;
        }

        // 다시 밤이 되면 원래 자리로
        public void RestorePose()
        {
            // 다시 켜면 Animator가 처음 상태로 돌아가므로, 켠 뒤에 자세를 다시 정함
            Body.gameObject.SetActive(true);
            Body.SetPositionAndRotation(_position, _rotation);
            if (_isTyping)
            {
                Npc.StartTyping();
            }
            else
            {
                Npc.StandUp();
            }
        }
    }
}
