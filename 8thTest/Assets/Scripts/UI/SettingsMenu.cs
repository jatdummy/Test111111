using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Esc로 여닫는 일시정지 메뉴 (계속하기 · 설정 · 타이틀로 · 게임 종료)
// 설정 창(마우스 감도 · 헤드밥 · 전체 화면)은 타이틀 화면에서도 같이 씀
public class SettingsMenu : MonoBehaviour
{
    private const string HeadBobKey = "Option.HeadBob";
    private const string SensitivityKey = "Option.Sensitivity";
    private const string OnColor = "#5FD07A";
    private const string OffColor = "#6E7681";
    private const float KnobTravel = 22f;

    // 마우스 감도 배율 범위와 맞춰 쓰는 단위 (8번 출구처럼 기본값 1)
    private const float MinSensitivity = 0.2f;
    private const float MaxSensitivity = 3f;
    private const float SensitivityStep = 0.1f;
    private const float DefaultSensitivity = 1f;

    // 일시정지 동안 끌 액션 맵 (액션 활성화를 바꾸는 곳은 이 클래스뿐, TechSpec 3장)
    private const string PlayerMapName = "Player";

    [Header("창")]
    // 일시 정지 창 (평소엔 꺼져 있음)
    [SerializeField] private GameObject _pausePanel;

    // 설정 창 (평소엔 꺼져 있음)
    [SerializeField, FormerlySerializedAs("_panel")] private GameObject _settingsPanel;

    // 헤드밥 (끄면 카메라가 흔들리지 않음)
    [SerializeField] private HeadBob _headBob;

    // 메뉴가 열린 동안 멈출 플레이어
    [SerializeField] private FirstPersonController _player;

    // 메뉴 열기 / 닫기 (UI/Cancel, Esc)
    [SerializeField] private InputActionReference _pauseAction;

    [Header("일시 정지 버튼")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _titleButton;
    [SerializeField] private Button _quitButton;

    [Header("설정")]
    // 설정 창을 닫고 돌아가기 (일시 정지 창 또는 타이틀 화면으로)
    [SerializeField] private Button _settingsBackButton;

    // 마우스 감도 슬라이더와 지금 값
    [SerializeField] private Slider _sensitivitySlider;
    [SerializeField] private Text _sensitivityValue;

    [Header("스위치")]
    // 헤드밥 스위치 (누르는 부분, 손잡이, 바탕, 상태 글자)
    [SerializeField] private Button _headBobButton;
    [SerializeField] private RectTransform _headBobKnob;
    [SerializeField] private Image _headBobTrack;
    [SerializeField] private Text _headBobState;

    // 전체 화면 스위치 (누르는 부분, 손잡이, 바탕, 상태 글자)
    [SerializeField] private Button _fullScreenButton;
    [SerializeField] private RectTransform _fullScreenKnob;
    [SerializeField] private Image _fullScreenTrack;
    [SerializeField] private Text _fullScreenState;

    private bool _isPaused;
    private bool _isTitle;
    private float _sensitivity = DefaultSensitivity;
    private float _savedTimeScale = 1f;

    // 설정 창을 닫았을 때 (타이틀 화면이 메뉴 선택을 되돌리게)
    public event Action OnSettingsClosed;

    private void Awake() => BindButtons();

    // 저장된 설정을 불러와 적용 (처음엔 헤드밥 켜짐, 감도 1)
    private void Start()
    {
        ApplyHeadBob(PlayerPrefs.GetInt(HeadBobKey, 1) == 1);
        ApplySensitivity(PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity));
        ShowSwitch(Screen.fullScreen, _fullScreenKnob, _fullScreenTrack, _fullScreenState);
        _pausePanel.SetActive(false);
        _settingsPanel.SetActive(false);
    }

    // 설정 창이 열려 있으면 Esc는 설정 창만 닫음, 타이틀 화면에서는 일시 정지하지 않음
    private void Update()
    {
        if (!_pauseAction.action.WasPressedThisFrame()) return;

        if (_settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (_isTitle) return;

        SetPaused(!_isPaused);
    }

    // 타이틀 화면 동안 플레이어 입력을 막고 커서를 풂 (출근하면 되돌림)
    public void SetTitleMode(bool isTitle)
    {
        _isTitle = isTitle;
        _player.IsInputBlocked = isTitle;
        SetPlayerActionsEnabled(!isTitle);
        SetCursorFree(isTitle);
    }

    // 설정 창 열기 (일시 정지 창, 타이틀 화면 공용)
    public void OpenSettings()
    {
        _pausePanel.SetActive(false);
        _settingsPanel.SetActive(true);
        Select(_settingsBackButton);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindButtons()
    {
        _resumeButton.onClick.AddListener(Resume);
        _settingsButton.onClick.AddListener(OpenSettings);
        _titleButton.onClick.AddListener(ReturnToTitle);
        _quitButton.onClick.AddListener(QuitGame);
        _settingsBackButton.onClick.AddListener(CloseSettings);
        _sensitivitySlider.minValue = MinSensitivity;
        _sensitivitySlider.maxValue = MaxSensitivity;
        _sensitivitySlider.onValueChanged.AddListener(ApplySensitivity);
        _headBobButton.onClick.AddListener(ToggleHeadBob);
        _fullScreenButton.onClick.AddListener(ToggleFullScreen);
    }

    // 열면 시간을 멈추고 커서를 풀며, 닫으면 되돌림
    private void SetPaused(bool isPaused)
    {
        _isPaused = isPaused;
        _pausePanel.SetActive(isPaused);
        _player.IsInputBlocked = isPaused;

        if (isPaused)
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Select(_resumeButton);
        }
        else
        {
            Time.timeScale = _savedTimeScale;
        }

        SetPlayerActionsEnabled(!isPaused);
        SetCursorFree(isPaused);
    }

    private void Resume() => SetPaused(false);

    // 일시 정지 중이었으면 일시 정지 창으로, 타이틀 화면이었으면 타이틀 메뉴로 돌아감
    private void CloseSettings()
    {
        _settingsPanel.SetActive(false);
        if (_isPaused)
        {
            _pausePanel.SetActive(true);
            Select(_resumeButton);
        }

        OnSettingsClosed?.Invoke();
    }

    // 씬을 다시 불러 타이틀 화면부터 (진행 중인 근무는 처음부터)
    private void ReturnToTitle()
    {
        Time.timeScale = _savedTimeScale;
        SetPlayerActionsEnabled(true);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 0.1 단위로 맞춰 저장하고 플레이어 시점 회전에 적용 (슬라이더 손잡이도 맞춘 값 자리로)
    private void ApplySensitivity(float value)
    {
        _sensitivity = Mathf.Clamp(Mathf.Round(value / SensitivityStep) * SensitivityStep, MinSensitivity, MaxSensitivity);
        _player.LookSensitivityScale = _sensitivity;
        PlayerPrefs.SetFloat(SensitivityKey, _sensitivity);
        _sensitivitySlider.SetValueWithoutNotify(_sensitivity);
        _sensitivityValue.text = _sensitivity.ToString("0.0");
    }

    private void ToggleHeadBob() => ApplyHeadBob(!_headBob.enabled);

    private void ApplyHeadBob(bool isOn)
    {
        _headBob.enabled = isOn;
        PlayerPrefs.SetInt(HeadBobKey, isOn ? 1 : 0);
        ShowSwitch(isOn, _headBobKnob, _headBobTrack, _headBobState);
    }

    // 전체 화면 ↔ 창 모드 (Unity가 다음 실행에도 기억함)
    private void ToggleFullScreen()
    {
        bool isFullScreen = !Screen.fullScreen;
        Screen.fullScreenMode = isFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        ShowSwitch(isFullScreen, _fullScreenKnob, _fullScreenTrack, _fullScreenState);
    }

    private void SetPlayerActionsEnabled(bool isEnabled)
    {
        // 다른 오브젝트의 Awake에서 불릴 수 있어서 미리 저장해 두지 않고 그때 찾음
        InputActionMap playerActions = InputSystem.actions.FindActionMap(PlayerMapName);
        if (isEnabled)
        {
            playerActions.Enable();
        }
        else
        {
            playerActions.Disable();
        }
    }

    private static void SetCursorFree(bool isFree)
    {
        Cursor.lockState = isFree ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isFree;
    }

    // 키보드(방향키 · Enter)로도 고를 수 있게 첫 버튼을 선택해 둠
    private static void Select(Button button)
    {
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    // 켜지면 손잡이가 오른쪽 · 초록 바탕, 꺼지면 왼쪽 · 회색 바탕
    private static void ShowSwitch(bool isOn, RectTransform knob, Image track, Text state)
    {
        knob.anchoredPosition = new Vector2(isOn ? KnobTravel : -KnobTravel, 0f);
        ColorUtility.TryParseHtmlString(isOn ? OnColor : OffColor, out Color color);
        track.color = color;
        state.text = isOn ? "켜짐" : "꺼짐";
        state.color = color;
    }
}
