using TMPro;
using UnityEngine;

// 빌드 진행도 표시
public class ProgressIndicatorView : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;

    public void Show(int count, int clearCount) => _label.text = $"BUILD {count} / {clearCount}";
}
