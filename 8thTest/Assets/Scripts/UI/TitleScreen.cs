using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 타이틀 화면: 엔딩의 마지막 장면(엘리베이터 안에서 보이는 22:00 사무실)을 그대로 배경으로 씀
// 출근하면 카메라가 엘리베이터에서 걸어 나와 플레이어 시점으로 옮겨 간 뒤 조작이 시작됨
public class TitleScreen : MonoBehaviour
{
    [Header("화면")]
    // 게임 제목과 메뉴 묶음 (출근할 때 서서히 사라짐)
    [SerializeField] private CanvasGroup _titleGroup;

    // 화면을 덮는 검은 판 (엔딩이 검게 끝난 뒤 이어서 밝아지며 시작)
    [SerializeField] private Image _fade;

    // 타이틀 동안 숨길 게임 화면 UI (알림, 상호작용 안내)
    [SerializeField] private Canvas _hud;

    [Header("메뉴 버튼")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;

    [Header("카메라")]
    // 타이틀 배경을 비추는 카메라 (엘리베이터 안)
    [SerializeField] private Camera _titleCamera;

    // 타이틀 동안 끌 플레이어 카메라 (시점, 손에 든 태블릿)
    [SerializeField] private Camera[] _playerCameras;

    // 출근할 때 타이틀 카메라가 옮겨 갈 플레이어 시점
    [SerializeField] private Transform _playerView;

    // 플레이어 시점까지 옮겨 가는 시간(초)
    [SerializeField, Min(0.01f)] private float _moveSeconds = 2.5f;

    // 처음 화면이 밝아지는 시간(초)
    [SerializeField, Min(0.01f)] private float _fadeInSeconds = 2f;

    // 출근할 때 제목과 메뉴가 사라지는 시간(초)
    [SerializeField, Min(0.01f)] private float _hideSeconds = 0.6f;

    [Header("연결")]
    // 설정 창과 플레이어 입력을 맡는 메뉴
    [SerializeField] private SettingsMenu _menu;

    // 타이틀 동안 문을 열어 둘 엘리베이터 (출근하면 닫힘)
    [SerializeField] private Elevator _elevator;

    private bool _isStarting;

    // 다른 오브젝트의 Start(플레이어의 커서 잠금 등)보다 먼저 타이틀 상태로 만듦
    private void Awake()
    {
        _startButton.onClick.AddListener(StartShift);
        _settingsButton.onClick.AddListener(_menu.OpenSettings);
        _quitButton.onClick.AddListener(_menu.QuitGame);
        ShowTitle();
    }

    private void OnEnable() => _menu.OnSettingsClosed += HandleSettingsClosed;

    private void Start()
    {
        Select(_startButton);
        StartCoroutine(FadeInRoutine());
    }

    private void OnDisable() => _menu.OnSettingsClosed -= HandleSettingsClosed;

    private void ShowTitle()
    {
        _menu.SetTitleMode(true);
        SetPlayerCameras(false);
        _titleCamera.enabled = true;
        _hud.enabled = false;
        _elevator.HoldOpen(true);
        _titleGroup.gameObject.SetActive(true);
        _titleGroup.alpha = 1f;
        _titleGroup.interactable = true;
        SetFade(1f);
    }

    private void StartShift()
    {
        if (_isStarting) return;

        _isStarting = true;
        StartCoroutine(StartShiftRoutine());
    }

    // 설정 창을 닫으면 키보드로 다시 고를 수 있게 설정 버튼을 선택
    private void HandleSettingsClosed()
    {
        if (_isStarting) return;

        Select(_settingsButton);
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _fadeInSeconds)
        {
            elapsed += Time.deltaTime;
            SetFade(1f - elapsed / _fadeInSeconds);
            yield return null;
        }

        SetFade(0f);
    }

    // 엘리베이터에서 나와 플레이어 시점으로 옮겨 간 뒤 카메라를 넘기고 조작을 풂
    private IEnumerator StartShiftRoutine()
    {
        _titleGroup.interactable = false;
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        Transform cameraTransform = _titleCamera.transform;
        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;
        float elapsed = 0f;
        while (elapsed < _moveSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _moveSeconds);
            cameraTransform.SetPositionAndRotation(Vector3.Lerp(startPosition, _playerView.position, t), Quaternion.Slerp(startRotation, _playerView.rotation, t));
            _titleGroup.alpha = 1f - Mathf.Clamp01(elapsed / _hideSeconds);
            yield return null;
        }

        _titleCamera.enabled = false;
        SetPlayerCameras(true);
        _hud.enabled = true;
        _titleGroup.gameObject.SetActive(false);
        _elevator.HoldOpen(false);
        _menu.SetTitleMode(false);
    }

    private void SetPlayerCameras(bool isEnabled)
    {
        foreach (Camera playerCamera in _playerCameras)
        {
            playerCamera.enabled = isEnabled;
        }
    }

    private void SetFade(float alpha)
    {
        Color color = _fade.color;
        color.a = Mathf.Clamp01(alpha);
        _fade.color = color;
    }

    private static void Select(Button button)
    {
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
