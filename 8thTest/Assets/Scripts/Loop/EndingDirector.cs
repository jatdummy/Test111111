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
    [SerializeField] private float _morningCeilingScale = 0.45f;

    [Header("퇴근")]
    // 플레이어 (사무실에 올라왔는지, 인사할 때 바라볼 대상)
    [SerializeField] private Transform _player;

    // 먼저 퇴근하는 동료들
    [SerializeField] private Leaver[] _leavers;

    // 걷기 속도(초당 거리)
    [SerializeField] private float _walkSpeed = 1.3f;

    // 인사하며 멈춰 있는 시간(초)
    [SerializeField] private float _greetSeconds = 2.5f;

    // 인사 말풍선 글자 (말하는 동료 머리 위로 옮겨 다님)
    [SerializeField] private TextMesh _speech;

    // 엘리베이터 문짝 (동료들이 타면 닫힘)
    [SerializeField] private GameObject _elevatorDoor;

    // 동료들을 태우고 문이 닫혀 있는 시간(초)
    [SerializeField] private float _doorClosedSeconds = 2f;

    [Header("마무리")]
    // 22:00 사무실을 보여 주는 시간(초)
    [SerializeField] private float _nightHoldSeconds = 3.5f;

    // 화면을 덮는 검은 판
    [SerializeField] private Image _fade;

    // 검게 덮이는 시간(초)
    [SerializeField] private float _fadeSeconds = 2f;

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
        foreach (Leaver leaver in _leavers)
        {
            leaver.CachePose();
        }

        SetMorning(true);
        StartCoroutine(Farewell());
    }

    // 엘리베이터를 타고 내려가면 다시 밤 사무실, 잠시 뒤 타이틀로
    public void PlayReturn()
    {
        StopAllCoroutines();
        SetMorning(false);
        _speech.gameObject.SetActive(false);
        foreach (Leaver leaver in _leavers)
        {
            leaver.RestorePose();
        }

        StartCoroutine(FadeToTitle());
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

    private IEnumerator Farewell()
    {
        // 플레이어가 계단을 올라와 사무실에 들어올 때까지 기다림
        yield return new WaitUntil(IsPlayerInOffice);

        int arrivedCount = 0;
        foreach (Leaver leaver in _leavers)
        {
            StartCoroutine(Leave(leaver, () => arrivedCount++));
        }

        yield return new WaitUntil(() => arrivedCount == _leavers.Length);

        // 모두 타면 문이 닫히고, 다시 열렸을 때는 아무도 없음
        _elevatorDoor.SetActive(true);
        yield return new WaitForSeconds(_doorClosedSeconds);

        foreach (Leaver leaver in _leavers)
        {
            leaver.Body.gameObject.SetActive(false);
        }

        _elevatorDoor.SetActive(false);
        CanLeave = true;
    }

    // 계단(바닥 아래)이 아니라 사무실 바닥에 올라와 있으면 사무실 안
    private bool IsPlayerInOffice() => _player.position.y > -0.5f && _player.position.z > -3.4f;

    private IEnumerator Leave(Leaver leaver, System.Action onArrived)
    {
        yield return new WaitForSeconds(leaver.StartDelay);

        // 앉아 있었다면 일어남
        leaver.Sit.enabled = false;
        if (leaver.Typing != null) leaver.Typing.enabled = false;
        leaver.Body.position += Vector3.up * leaver.StandRise;
        leaver.Walk.Speed = _walkSpeed;
        leaver.Walk.enabled = true;

        for (int i = 0; i < leaver.Path.Length; i++)
        {
            // 엘리베이터에 들어가기 직전에 멈춰서 플레이어에게 인사
            if (i == leaver.Path.Length - 1) yield return Greet(leaver);

            yield return WalkTo(leaver, leaver.Path[i]);
        }

        leaver.Walk.enabled = false;
        onArrived();
    }

    private IEnumerator WalkTo(Leaver leaver, Vector3 target)
    {
        while (leaver.Body.position != target)
        {
            leaver.Body.position = Vector3.MoveTowards(leaver.Body.position, target, _walkSpeed * Time.deltaTime);
            Face(leaver.Body, target);
            yield return null;
        }
    }

    private IEnumerator Greet(Leaver leaver)
    {
        leaver.Walk.enabled = false;
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
        leaver.Walk.enabled = true;
    }

    // 말풍선을 머리 위에 두고 플레이어 쪽을 보게 함
    private void ShowSpeechAbove(Transform body)
    {
        Transform speech = _speech.transform;
        speech.position = body.position + Vector3.up * SpeechHeight;
        Vector3 direction = speech.position - _player.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > MinFaceDistance) speech.rotation = Quaternion.LookRotation(direction);
    }

    private static void Face(Transform body, Vector3 target)
    {
        Vector3 direction = target - body.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < MinFaceDistance) return;

        body.rotation = Quaternion.LookRotation(direction);
    }

    private IEnumerator FadeToTitle()
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

        // 걷는 동작
        public WalkPose Walk;

        // 앉은 자세
        public SitPose Sit;

        // 타자 치는 동작 (없으면 비워 둠)
        public TypingPose Typing;

        // 앉아 있다가 일어날 때 올라가는 높이 (서 있으면 0)
        public float StandRise;

        // 출발 전 기다리는 시간(초)
        public float StartDelay;

        // 엘리베이터 안까지 걷는 경로 (마지막 지점 직전에 인사)
        public Vector3[] Path;

        // 플레이어에게 건네는 인사
        public string Greeting;

        private Vector3 _position;
        private Quaternion _rotation;
        private bool _isSitting;
        private bool _isTyping;

        public void CachePose()
        {
            _position = Body.position;
            _rotation = Body.rotation;
            _isSitting = Sit.enabled;
            _isTyping = Typing != null && Typing.enabled;
        }

        // 다시 밤이 되면 원래 자리로
        public void RestorePose()
        {
            Body.gameObject.SetActive(true);
            Body.SetPositionAndRotation(_position, _rotation);
            Walk.enabled = false;
            Sit.enabled = _isSitting;
            if (Typing != null) Typing.enabled = _isTyping;
        }
    }
}
