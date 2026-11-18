using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 타이틀 화면: Enter, Space, 클릭으로 출근(게임 시작)
public class TitleScreen : MonoBehaviour
{
    // 시작할 게임 씬 이름
    [SerializeField] private string _gameSceneName = "OfficeLoop_MVP";

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

    private static bool IsStartPressed()
    {
        bool isKey = Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        bool isClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return isKey || isClick;
    }
}
