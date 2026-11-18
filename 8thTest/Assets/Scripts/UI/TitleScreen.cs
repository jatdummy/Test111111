using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 타이틀 화면: Enter, 클릭으로 출근(게임 시작)
public class TitleScreen : MonoBehaviour
{
    // 시작할 게임 씬 이름
    [SerializeField] private string _gameSceneName = "OfficeLoop_MVP";

    // 시작 키 (UI/Submit, Enter)
    [SerializeField] private InputActionReference _submitAction;

    // 시작 클릭 (UI/Click)
    [SerializeField] private InputActionReference _clickAction;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (IsStartPressed())
        {
            SceneManager.LoadScene(_gameSceneName);
        }
    }

    private bool IsStartPressed() => _submitAction.action.WasPressedThisFrame() || _clickAction.action.WasPressedThisFrame();
}
