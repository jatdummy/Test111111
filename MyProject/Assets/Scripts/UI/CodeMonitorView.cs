using System.Text;
using UnityEngine;
using UnityEngine.UI;

// 천장에 매달린 코드 화면 표시
public class CodeMonitorView : MonoBehaviour
{
    private const string Indent = "    ";
    private const string CommentMark = "//";

    // 코드 글자(앞면, 뒷면)
    [SerializeField] private Text[] _codeTexts;

    [Header("문법 색")]
    // 키워드 색
    [SerializeField] private Color _keywordColor = new(0.34f, 0.61f, 0.84f);

    // 타입 이름 색
    [SerializeField] private Color _typeColor = new(0.31f, 0.79f, 0.69f);

    // 주석 색
    [SerializeField] private Color _commentColor = new(0.45f, 0.65f, 0.36f);

    // 사무실 번호와 클래스 본문 표시(본문이 비면 { })
    public void ShowOfficeCode(int officeNumber, string body)
    {
        var builder = new StringBuilder();
        builder.Append(Colorize("class", _keywordColor));
        builder.Append($" Office_{officeNumber:00} : ");
        builder.Append(Colorize("BaseOffice", _typeColor));

        if (string.IsNullOrWhiteSpace(body))
        {
            builder.Append(" { }");
        }
        else
        {
            builder.Append("\n{\n");
            foreach (string line in body.Split('\n'))
            {
                builder.Append(Indent).Append(HighlightComment(line)).Append('\n');
            }
            builder.Append('}');
        }

        ShowText(builder.ToString());
    }

    // 임의 문구 표시
    public void ShowText(string text)
    {
        foreach (Text codeText in _codeTexts)
        {
            codeText.text = text;
        }
    }

    private string HighlightComment(string line)
    {
        int commentIndex = line.IndexOf(CommentMark);
        if (commentIndex < 0) return line;

        return line.Substring(0, commentIndex) + Colorize(line.Substring(commentIndex), _commentColor);
    }

    private static string Colorize(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
}
