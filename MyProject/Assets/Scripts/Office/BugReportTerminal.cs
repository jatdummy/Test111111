using UnityEngine;
using UnityEngine.UI;

// 마지막 실패 원인을 보여 주는 버그 리포트 컴퓨터
public class BugReportTerminal : MonoBehaviour, IInteractable
{
    private const string EmptyScreenText = "BUG REPORT\n\n새 리포트 없음";
    private const string ClosedScreenText = "BUG REPORT\n\n<color=#ff6b6b>새 리포트 1건</color>\n\nE  열기";

    // 컴퓨터 화면 글자
    [SerializeField] private Text _screenText;

    private string _report;
    private bool _isOpen;

    public string PromptText => _isOpen ? "E  리포트 닫기" : "E  버그 리포트 열기";
    public bool CanInteract => !string.IsNullOrEmpty(_report);

    private void Awake() => RefreshScreen();

    // 리포트 내용 지정(빈 문자열이면 리포트 없음)
    public void SetReport(string report)
    {
        _report = report;
        _isOpen = false;
        RefreshScreen();
    }

    public void Interact()
    {
        _isOpen = !_isOpen;
        RefreshScreen();
    }

    private void RefreshScreen()
    {
        if (string.IsNullOrEmpty(_report))
        {
            _screenText.text = EmptyScreenText;
        }
        else
        {
            _screenText.text = _isOpen ? _report : ClosedScreenText;
        }
    }
}
