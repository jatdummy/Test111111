using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// 노트북 화면: 긴 알고리즘 코드를 한 글자씩 치는 코드 편집기
// 이 노트북으로 타자를 치는 동안에만 글자가 써짐
public class BuildLogScreen : MonoBehaviour
{
    private const float CursorBlinkSeconds = 0.5f;
    private const string TextColor = "#D4D4D4";
    private const string KeywordColor = "#569CD6";
    private const string TypeColor = "#4EC9B0";
    private const string NumberColor = "#B5CEA8";
    private const string CommentColor = "#6A9955";

    private static readonly HashSet<string> Keywords = new()
    {
        "using", "public", "private", "static", "readonly", "const", "class", "struct",
        "new", "return", "if", "else", "for", "while", "continue", "var", "int",
        "bool", "void", "object", "null", "true", "false", "out", "is", "override",
    };

    // 숫자, 이름(키워드·타입), 그 외 글자를 나눔
    private static readonly Regex TokenPattern = new(@"\b\d+\b|\b[A-Za-z_]\w*\b");

    // 코드를 보여 줄 화면 글자
    [SerializeField] private TextMesh _text;

    // 이 노트북으로 타자 치는 사람 (타자 치는 동안에만 갱신)
    [SerializeField] private NpcBody _typist;

    // 치고 있는 코드 원본 (끝까지 치면 처음부터 다시)
    [SerializeField] private TextAsset _source;

    // 화면에 보이는 줄 수 (넘치면 위로 밀려 올라감)
    [SerializeField, Min(1)] private int _visibleLines = 9;

    // 1초에 치는 글자 수
    [SerializeField, Min(0f)] private float _charsPerSecond = 12f;

    // 한 줄을 다 칠 때마다 이상한 주석이 끼어들 확률 (0이면 없음)
    [SerializeField, Range(0f, 1f)] private float _strangeLineChance = 0f;

    // 가끔 끼어드는 이상한 주석
    [SerializeField] private string[] _strangeLines =
    {
        "// 경로가 다시 시작점으로 돌아온다",
        "// 출구 노드가 없다",
        "// 06:00은 오지 않는다",
    };

    // 완성된 줄 (넣을 때 한 번만 색을 입혀 보관)
    private readonly List<string> _doneLines = new();
    private readonly StringBuilder _builder = new();
    private string[] _sourceLines;
    private int _sourceIndex;
    private string _currentLine = string.Empty;
    private int _typedChars;
    private float _typeTimer;
    private float _blinkTimer;
    private bool _isCursorOn = true;

    private void Start()
    {
        _sourceLines = _source.text.Replace("\r", "").Split('\n');

        // 화면마다 다른 곳부터, 빈 화면이 아니게 앞줄을 채워 둔 채 시작
        _sourceIndex = Random.Range(0, _sourceLines.Length);
        for (int i = 0; i < _visibleLines - 1; i++)
        {
            _doneLines.Add(Highlight(TakeSourceLine()));
        }

        StartLine(TakeSourceLine());
        Render();
    }

    private void Update()
    {
        // 자리에 앉아 손을 올리기 전에는 화면이 멈춰 있음
        if (_typist != null && !_typist.IsTyping) return;

        // 글자가 쳐졌거나 커서가 깜빡인 프레임에만 다시 그림 (매 프레임 문자열을 만들지 않게)
        bool isCursorChanged = UpdateCursor();
        bool isTyped = TypeCharacters();
        if (isCursorChanged || isTyped)
        {
            Render();
        }
    }

    private string TakeSourceLine()
    {
        string line = _sourceLines[_sourceIndex];
        _sourceIndex = (_sourceIndex + 1) % _sourceLines.Length;
        return line;
    }

    // 편집기 자동 들여쓰기처럼 앞 공백은 한 번에 들어감
    private void StartLine(string line)
    {
        _currentLine = line;
        _typedChars = line.Length - line.TrimStart().Length;
    }

    // 커서가 깜빡였으면 true
    private bool UpdateCursor()
    {
        _blinkTimer += Time.deltaTime;
        if (_blinkTimer < CursorBlinkSeconds) return false;

        _blinkTimer = 0f;
        _isCursorOn = !_isCursorOn;
        return true;
    }

    // 이번 프레임에 한 글자라도 쳤으면 true
    private bool TypeCharacters()
    {
        bool isTyped = false;
        _typeTimer += Time.deltaTime * _charsPerSecond;
        while (_typeTimer >= 1f)
        {
            _typeTimer -= 1f;
            isTyped = true;
            if (_typedChars < _currentLine.Length)
            {
                _typedChars++;
                continue;
            }

            // 한 줄을 다 치면 완성된 줄로 넘기고 다음 줄 시작
            PushDoneLine(_currentLine);
            StartLine(PickNextLine());
        }

        return isTyped;
    }

    private void PushDoneLine(string line)
    {
        _doneLines.Add(Highlight(line));
        if (_doneLines.Count > _visibleLines - 1)
        {
            _doneLines.RemoveAt(0);
        }
    }

    // 다음 줄은 원본 코드, 가끔 같은 들여쓰기로 이상한 주석이 끼어듦
    private string PickNextLine()
    {
        if (_strangeLines.Length == 0 || Random.value >= _strangeLineChance) return TakeSourceLine();

        string indent = _currentLine.Substring(0, _currentLine.Length - _currentLine.TrimStart().Length);
        return indent + _strangeLines[Random.Range(0, _strangeLines.Length)];
    }

    private void Render()
    {
        _builder.Clear();
        foreach (string line in _doneLines)
        {
            _builder.AppendLine(line);
        }

        // 치고 있는 줄은 색 없이 그대로 + 깜빡이는 커서
        string typed = _currentLine.Substring(0, Mathf.Min(_typedChars, _currentLine.Length));
        _builder.Append(Colorize(typed + (_isCursorOn ? "_" : string.Empty), TextColor));
        _text.text = _builder.ToString();
    }

    // 완성된 줄에 편집기처럼 색 입히기 (주석, 키워드, 타입, 숫자)
    private static string Highlight(string line)
    {
        int comment = line.IndexOf("//");
        string code = comment >= 0 ? line.Substring(0, comment) : line;
        string tail = comment >= 0 ? Colorize(line.Substring(comment), CommentColor) : string.Empty;

        string colored = TokenPattern.Replace(code, match =>
        {
            string token = match.Value;
            if (char.IsDigit(token[0])) return Colorize(token, NumberColor);
            if (Keywords.Contains(token)) return Colorize(token, KeywordColor);
            if (char.IsUpper(token[0])) return Colorize(token, TypeColor);
            return token;
        });

        return Colorize(colored, TextColor) + tail;
    }

    private static string Colorize(string text, string color) => $"<color={color}>{text}</color>";
}
