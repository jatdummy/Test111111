using System.Collections.Generic;
using System.Text;

// 판정 기록을 모아 두고 최근 몇 줄로 리포트를 만드는 기록장
public class QaReportLog
{
    private const string ReportHeader = "<color=#5FD07A>QA 어시스턴트 리포트</color>";

    private readonly List<string> _lines = new();
    private readonly int _maxLines;

    public int Count => _lines.Count;

    // maxLines : 보관하는 최대 줄 수 (넘으면 오래된 줄부터 버림)
    public QaReportLog(int maxLines)
    {
        _maxLines = maxLines;
    }

    // 판정 기록 한 줄 추가 (리포트에는 최근 몇 줄만 담김)
    public void Add(string line)
    {
        _lines.Add(line);
        if (_lines.Count > _maxLines)
        {
            _lines.RemoveAt(0);
        }
    }

    // 태블릿에 보낼 리포트 (보낸 시각의 최근 판정 기록, 틀렸으면 이유가 붙어 있음)
    public string Build(int recentCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine(ReportHeader);
        builder.AppendLine();

        int start = System.Math.Max(0, _lines.Count - recentCount);
        for (int i = start; i < _lines.Count; i++)
        {
            builder.Append(_lines[i]);
            if (i < _lines.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }
}
