using System.Globalization;
using System.Text;

// LoopSpec 기반 빌드 모니터 코드 텍스트 생성
public static class CodeTextGenerator
{
    private const string Indent = "    ";

    // 플랫폼별 줄바꿈 차이 방지용 고정 줄바꿈
    private const char NewLine = '\n';

    // 명세와 사무실 길이로 모니터 코드 문자열 생성
    public static string Generate(LoopSpec spec, float officeLength)
    {
        var builder = new StringBuilder();
        builder.Append("class Office_").Append(spec.StageIndex.ToString("00", CultureInfo.InvariantCulture)).Append(" : BaseOffice {").Append(NewLine);
        builder.Append(Indent).Append("const float OfficeLength = ").Append(officeLength.ToString(CultureInfo.InvariantCulture)).Append("f;").Append(NewLine);

        foreach (string line in spec.OverrideLines)
            builder.Append(Indent).Append(line).Append(NewLine);

        builder.Append('}');
        return builder.ToString();
    }
}
