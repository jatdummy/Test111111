using System.Collections.Generic;
using System.Text;

// 판정 기록을 모아 두고 최근 몇 줄로 리포트를 만드는 기록장
public class QaReportLog
{
    private const string ReportHeader = "<color=#5FD07A>QA 어시스턴트 리포트</color>";

    private readonly List<string> _lines = new();
    private readonly int _maxLines;

    // 지난 전송 뒤에 새로 쌓인 줄 수 (보관 줄 수를 넘어 오래된 줄이 버려지면 함께 줄어듦)
    private int _newCount;

    // 지금까지 태블릿으로 보낸 리포트 수 (리포트 제목에 붙는 회차)
    private int _sentCount;

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

        _newCount = System.Math.Min(_newCount + 1, _lines.Count);
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

    // 태블릿에 보낼 이번 회차 리포트 (지난 전송 뒤 새로 쌓인 기록만, 그중 최근 몇 줄)
    // 이미 받은 기록이 다음 리포트에 다시 섞이지 않게, 만든 뒤에는 새 기록 수를 비움
    public string BuildNew(int recentCount)
    {
        _sentCount++;

        var builder = new StringBuilder();
        builder.AppendLine($"{ReportHeader} <color=#5FD07A>#{_sentCount}</color>");
        builder.AppendLine();

        int count = System.Math.Min(recentCount, _newCount);
        int start = _lines.Count - count;
        for (int i = start; i < _lines.Count; i++)
        {
            builder.Append(_lines[i]);
            if (i < _lines.Count - 1)
            {
                builder.AppendLine();
            }
        }

        _newCount = 0;
        return builder.ToString();
    }
}
