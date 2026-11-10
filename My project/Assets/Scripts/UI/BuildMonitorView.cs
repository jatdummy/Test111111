using TMPro;
using UnityEngine;

// 빌드 모니터 코드 표시
public class BuildMonitorView : MonoBehaviour
{
    [SerializeField] private TMP_Text _codeText;

    public void Show(string code) => _codeText.text = code;
}
