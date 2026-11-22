using System.Text;
using UnityEngine;

// 리뷰 모니터와 태블릿에 보여 줄 빌드 코드 글자, 통로 벽시계, QA 수칙 글
public class CodeMonitor : MonoBehaviour
{
    // QA 수칙 
    private static readonly string[] RuleLines =
    {
        "1. 코드는 틀리지 않습니다. 틀리는 것은 사무실입니다.",
        "2. 코드와 다르다면, 앞쪽의 계단을 내려가십시오.",
        "3. 코드와 같다면, 뒤쪽의 계단을 올라가십시오.",
        "4. 코드는 긴급 수정될 수 있습니다. 1번을 기억하십시오.",
        "5. 버그 리포트는 컴퓨터에서. 세 번까지만.",
        "6. 06:00이 되면 퇴근하셔도 좋습니다.",
    };

    [Header("화면")]
    // 코드를 보여 줄 글자 (리뷰 모니터, 태블릿 코드 페이지, 항상 켜져 있음)
    [SerializeField] private TextMesh[] _monitorTexts;

    // 통로 벽시계 글자
    [SerializeField] private TextMesh _clockText;

    // 이번 빌드의 코드와 시각을 모니터, 시계에 표시
    public void Show(OfficeBuild build, int hoursPassed, bool isCleared)
    {
        string code = BuildCodeText(build, hoursPassed, isCleared);
        foreach (TextMesh monitorText in _monitorTexts)
        {
            monitorText.text = code;
        }

        _clockText.text = LoopTime.Format(hoursPassed);
    }

    public string BuildRulesText()
    {
        var builder = new StringBuilder();
        foreach (string line in RuleLines)
        {
            // 번호만 강조색, 나머지는 공지문처럼 한 색
            int dot = line.IndexOf(". ");
            bool isNumbered = dot > 0 && char.IsDigit(line[0]);
            builder.AppendLine(isNumbered ? RichText.Colorize(line.Substring(0, dot + 1), RichText.WaitingColor) + line.Substring(dot + 1) : line);
        }

        return builder.ToString();
    }

    private static string BuildCodeText(OfficeBuild build, int hoursPassed, bool isCleared)
    {
        var builder = new StringBuilder();
        string time = LoopTime.Format(hoursPassed);

        // 클리어하면 리뷰 대기 대신 릴리스 완료
        if (isCleared)
        {
            builder.AppendLine(RichText.Colorize($"● 릴리스 완료   {time}", RichText.PassColor));
            builder.AppendLine();
            builder.AppendLine(RichText.Colorize("// 퇴근하셔도 됩니다.", RichText.CommentColor));
            return builder.ToString();
        }

        int number = build.GetNumber(hoursPassed);
        builder.AppendLine(RichText.Colorize($"● 리뷰 대기   Office_{number:00}   {time}", RichText.WaitingColor));
        if (build.Hotfix != HotfixKind.None)
        {
            builder.AppendLine(RichText.Colorize("● 긴급 핫픽스 적용됨 — 다시 확인하세요", RichText.HotfixColor));
        }

        builder.AppendLine();

        // 바뀐 값이 없으면 원본과 같은 한 줄로 표시
        bool hasOverride = build.LightOverride != LightOverride.None || build.IsCabinetOpen || build.CodedAnomaly != null || build.Hotfix != HotfixKind.None;
        if (build.IsOriginal || !hasOverride)
        {
            builder.AppendLine($"class Office_{number:00} : BaseOffice {{ }}");
            return builder.ToString();
        }

        builder.AppendLine($"class Office_{number:00} : BaseOffice");
        builder.AppendLine("{");
        AppendOverrides(builder, build);
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendOverrides(StringBuilder builder, OfficeBuild build)
    {
        // 바뀐 줄은 + 표시와 초록색으로 한눈에 보이게
        if (build.LightOverride != LightOverride.None)
        {
            AppendOverrideLine(builder, build, build.LightOverride == LightOverride.Red ? OverrideKind.RedLight : OverrideKind.BlueLight);
        }

        if (build.IsCabinetOpen)
        {
            AppendOverrideLine(builder, build, OverrideKind.Bookcase);
        }

        // 코드로 적은 이상현상 모습 (이상현상 데이터의 코드 줄)
        if (build.CodedAnomaly != null)
        {
            builder.AppendLine(RichText.Colorize($"<b>+   {build.CodedAnomaly.Data.CodeLine}</b>", RichText.AddedColor));
        }

        // 핫픽스로 새로 들어온 줄은 주황색
        bool isHotfixAdded = build.Hotfix is HotfixKind.AddLine or HotfixKind.Legitimize;
        if (isHotfixAdded)
        {
            builder.AppendLine(RichText.Colorize($"<b>+   {OverrideText.GetCode(build.HotfixTarget)}</b>   // hotfix", RichText.HotfixColor));
        }
    }

    // 원래 있던 override 한 줄 (핫픽스로 지워졌으면 - 표시와 빨간색)
    private static void AppendOverrideLine(StringBuilder builder, OfficeBuild build, OverrideKind kind)
    {
        bool isRemoved = build.Hotfix == HotfixKind.RemoveLine && build.HotfixTarget == kind;
        // 방송 화면에서도 잘 보이게 바뀐 줄은 굵게
        builder.AppendLine(isRemoved ? RichText.Colorize($"<b>-   {OverrideText.GetCode(kind)}</b>   // hotfix", RichText.FailColor) : RichText.Colorize($"<b>+   {OverrideText.GetCode(kind)}</b>", RichText.AddedColor));
    }
}
