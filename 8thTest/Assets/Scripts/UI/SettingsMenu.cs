using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Esc로 여닫는 일시정지 메뉴 (헤드밥 켜고 끄기)
public class SettingsMenu : MonoBehaviour
{
    private const string HeadBobKey = "Option.HeadBob";
    private const string OnColor = "#5FD07A";
    private const string OffColor = "#6E7681";
    private const float KnobTravel = 22f;

    // 메뉴 전체 (평소엔 꺼져 있음)
    [SerializeField] private GameObject _panel;

    // 헤드밥 (끄면 카메라가 흔들리지 않음)
    [SerializeField] private HeadBob _headBob;

    // 메뉴가 열린 동안 멈출 플레이어
    [SerializeField] private FirstPersonController _player;

    [Header("스위치")]
    // 헤드밥 스위치 (누르는 부분, 손잡이, 바탕, 상태 글자)
    [SerializeField] private Button _headBobButton;
    [SerializeField] private RectTransform _headBobKnob;
    [SerializeField] private Image _headBobTrack;
    [SerializeField] private Text _headBobState;

    private InputActionMap _playerActions;
    private bool _isOpen;
    private float _savedTimeScale = 1f;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        _playerActions = InputSystem.actions.FindActionMap("Player");
        _headBobButton.onClick.AddListener(ToggleHeadBob);
    }

    // 저장된 설정을 불러와 적용 (처음엔 켜짐)
    private void Start()
    {
        ApplyHeadBob(PlayerPrefs.GetInt(HeadBobKey, 1) == 1);
        _panel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetOpen(!_isOpen);
        }
    }

    // 열면 시간을 멈추고 커서를 풀며, 닫으면 되돌림
    private void SetOpen(bool isOpen)
    {
        _isOpen = isOpen;
        _panel.SetActive(isOpen);
        _player.IsInputBlocked = isOpen;

        if (isOpen)
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _playerActions.Disable();
        }
        else
        {
            Time.timeScale = _savedTimeScale;
            _playerActions.Enable();
        }

        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
    }

    private void ToggleHeadBob() => ApplyHeadBob(!_headBob.enabled);

    private void ApplyHeadBob(bool isOn)
    {
        _headBob.enabled = isOn;
        PlayerPrefs.SetInt(HeadBobKey, isOn ? 1 : 0);
        ShowSwitch(isOn, _headBobKnob, _headBobTrack, _headBobState);
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
