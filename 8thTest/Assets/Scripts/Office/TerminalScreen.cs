using System.Text;
using UnityEngine;

// QA 어시스턴트 단말 화면 (상태 글씨 · 안내 글씨 · 커서 · 화면 재질)
public class TerminalScreen : MonoBehaviour
{
    // 화면은 큰 상태 한 줄 + 작은 안내 한 줄 (멀리서도, 방송 화면에서도 상태가 읽히게)
    private const string IdleTitle = "대기 중";
    private const string IdleSub = "<color=#7D848E>QA 어시스턴트</color>";
    private const string ReadyTitle = "리포트 준비됨";
    private const string ReadySub = "<color=#C9CDD3>[E] 태블릿으로 전송</color>";
    private const string SendingTitle = "<color=#F2C14E>전송 중</color>";
    private const string SentTitle = "<color=#5FD07A>전송 완료</color>";
    private const string SentSub = "<color=#C9CDD3>태블릿을 확인하세요</color>";
    private const string IdleColor = "7D848E";
    private const string ReadyColor = "F2C14E";
    private const float ReadyBlinkSpeed = 3f;

    // 리포트 준비됨 글자가 가장 어두울 때의 투명도
    private const float ReadyMinAlpha = 0.35f;
    private const float CursorBlinkSeconds = 0.5f;
    private const int DotCount = 3;

    // 전송 중 글자 (점 1~3개, 미리 만들어 두고 돌려 씀)
    private static readonly string[] SendingTexts = { SendingTitle + ".", SendingTitle + "..", SendingTitle + "..." };

    // 고장 직전 깜빡임 (글자가 꺼질 확률, 화면이 꺼질 확률)
    private const float GlitchTextHideChance = 0.25f;
    private const float GlitchScreenOffChance = 0.3f;

    // 깨진 글자 (줄 수, 줄 길이 범위, 처음 깨질 확률, 끝까지 늘어나는 확률)
    private const int GlitchLineCount = 6;
    private const int GlitchLineMin = 8;
    private const int GlitchLineMax = 30;
    private const float GlitchBaseChance = 0.3f;
    private const float GlitchGrowChance = 0.7f;

    // 단말 화면 글자 (큰 상태 글씨)
    [SerializeField] private TextMesh _screenText;

    // 상태 아래 작은 안내 글씨
    [SerializeField] private TextMesh _subText;

    // 화면 구석에서 깜빡이는 커서 (컴퓨터가 켜져 있다는 느낌)
    [SerializeField] private TextMesh _cursorText;

    // 고장 날 때 깨진 글자를 보여 줄 크기 배율 (큰 상태 글씨에 비해 작게)
    [SerializeField, Range(0f, 1f)] private float _glitchTextScale = 0.5f;

    // 모니터 화면 판 (고장 나면 꺼진 재질로 바뀜)
    [SerializeField] private Renderer _screen;

    // 꺼진 화면 재질
    [SerializeField] private Material _deadScreenMaterial;

    private Material _liveScreenMaterial;

    // 대기 / 준비 글자 색 (깜빡임은 글자 색 알파로)
    private Color _idleColor;
    private Color _readyColor;

    // 지금 화면에 넣어 둔 대기 / 준비 글자 (같으면 다시 넣지 않음)
    private string _shownTitle;

    // 플레이어가 화면을 보고 있는지 잴 때 쓰는 화면 가운데
    public Vector3 Center => _screen.bounds.center;

    private void Awake()
    {
        _liveScreenMaterial = _screen.sharedMaterial;
        ColorUtility.TryParseHtmlString("#" + IdleColor, out _idleColor);
        ColorUtility.TryParseHtmlString("#" + ReadyColor, out _readyColor);
    }

    // 대기 화면 (보낼 리포트가 있으면 준비됨)
    public void ShowIdle(bool hasReport)
    {
        _subText.text = hasReport ? ReadySub : IdleSub;
        Animate(hasReport, false);
    }

    // 리포트가 준비되면 상태 글씨가 천천히 깜빡이고, 구석 커서는 늘 깜빡임
    // 글자는 상태가 바뀔 때만 넣고, 준비됨 깜빡임은 글자 색 알파만 바꿈 (매 프레임 문자열을 만들지 않게)
    public void Animate(bool hasReport, bool isBusy)
    {
        bool isCursorOn = Time.time % (CursorBlinkSeconds * 2f) < CursorBlinkSeconds;
        _cursorText.text = isCursorOn ? "<color=#5FD07A>_</color>" : string.Empty;
        if (isBusy)
        {
            // 전송 · 고장 글자는 각자 색 태그를 씀, 끝나면 대기 / 준비 글자를 다시 넣게 비워 둠
            _screenText.color = Color.white;
            _shownTitle = null;
            return;
        }

        if (!hasReport)
        {
            ShowTitle(IdleTitle, _idleColor);
            return;
        }

        Color color = _readyColor;
        color.a = Mathf.Lerp(ReadyMinAlpha, 1f, (Mathf.Sin(Time.time * ReadyBlinkSpeed) + 1f) * 0.5f);
        ShowTitle(ReadyTitle, color);
    }

    public void SetVisible(bool isVisible)
    {
        _screenText.gameObject.SetActive(isVisible);
        _subText.gameObject.SetActive(isVisible);
        _cursorText.gameObject.SetActive(isVisible);
    }

    // 전송 중 점이 하나씩 늘어남 (progress : 0 ~ 1)
    public void ShowSending(float progress)
    {
        int dots = 1 + (int)(progress * DotCount * 2) % DotCount;
        _screenText.text = SendingTexts[dots - 1];
        _subText.text = IdleSub;
    }

    public void ShowSent()
    {
        _screenText.text = SentTitle;
        _subText.text = SentSub;
    }

    // 고장 시작 (큰 글씨만 남기고 깨진 글자를 담을 수 있게 작게)
    public void BeginGlitch()
    {
        SetVisible(true);
        _subText.gameObject.SetActive(false);
        _cursorText.gameObject.SetActive(false);
        _screenText.characterSize *= _glitchTextScale;
    }

    // 글자가 깨지며 화면이 깜빡임 (progress : 0 ~ 1, 갈수록 더 많이 깨짐)
    public void ShowGlitch(float progress)
    {
        _screenText.text = CreateGlitchText(progress);
        _screenText.gameObject.SetActive(Random.value > GlitchTextHideChance);
        _screen.sharedMaterial = Random.value > GlitchScreenOffChance ? _liveScreenMaterial : _deadScreenMaterial;
    }

    // 부서진 화면 (글자 없이 꺼진 재질)
    public void SetBroken()
    {
        SetVisible(false);
        _screen.sharedMaterial = _deadScreenMaterial;
    }

    private void ShowTitle(string title, Color color)
    {
        if (_shownTitle != title)
        {
            _shownTitle = title;
            _screenText.text = title;
        }

        _screenText.color = color;
    }

    // 시간이 갈수록 더 많이 깨지는 글자
    private static string CreateGlitchText(float progress)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<color=#FF6B6B>FATAL  QA-ASSIST  0x08</color>");
        GlitchText.AppendLines(builder, GlitchLineCount, GlitchLineMin, GlitchLineMax, GlitchBaseChance + progress * GlitchGrowChance, true);
        builder.Append("<color=#FF6B6B>세션 기록 손상 — 전송 불가</color>");
        return builder.ToString();
    }
}
