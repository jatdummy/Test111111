using System.Collections.Generic;
using System.Text;

// 판정 기록을 모아 두고 최근 몇 줄로 리포트를 만드는 기록장
// 리포트 한 번에는 버그 하나만: 가장 최근에 틀린 세션(검수 시작 ~ 세션 초기화)만 담고, 그 전에 틀린 세션은 담지 않음
public class QaReportLog
{
    private const string ReportHeader = "<color=#5FD07A>QA 어시스턴트 리포트</color>";

    private readonly List<string> _lines = new();
    private readonly int _maxLines;

    // 지금 진행 중인 세션이 시작된 줄 (마지막 세션 초기화 다음 줄)
    private int _sessionStart;

    // 다음 리포트로 보낼 세션의 시작 줄과 끝 줄 다음 (보낼 것이 없으면 둘이 같음)
    private int _reportStart;
    private int _reportEnd;

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
        if (_lines.Count <= _maxLines) return;

        // 오래된 줄이 버려지면 기억해 둔 줄 번호도 한 칸씩 앞으로
        _lines.RemoveAt(0);
        _sessionStart = System.Math.Max(_sessionStart - 1, 0);
        _reportStart = System.Math.Max(_reportStart - 1, 0);
        _reportEnd = System.Math.Max(_reportEnd - 1, 0);
    }

    // 지금 세션이 틀려서 끝남 (이 세션이 다음 리포트가 되고, 아직 안 받은 이전 실패는 리포트에서 빠짐)
    public void EndFailedSession()
    {
        _reportStart = _sessionStart;
        _reportEnd = _lines.Count;
        _sessionStart = _lines.Count;
    }

    // 태블릿에 보낼 이번 회차 리포트 (가장 최근에 틀린 세션만, 그중 최근 몇 줄)
    // 이미 받은 세션이 다음 리포트에 다시 나오지 않게, 만든 뒤에는 보낼 세션을 비움
    public string BuildNew(int recentCount)
    {
        _sentCount++;

        var builder = new StringBuilder();
        builder.AppendLine($"{ReportHeader} <color=#5FD07A>#{_sentCount}</color>");
        builder.AppendLine();

        int start = System.Math.Max(_reportStart, _reportEnd - recentCount);
        for (int i = start; i < _reportEnd; i++)
        {
            builder.Append(_lines[i]);
            if (i < _reportEnd - 1)
            {
                builder.AppendLine();
            }
        }

        _reportStart = _reportEnd;
        return builder.ToString();
    }
}
