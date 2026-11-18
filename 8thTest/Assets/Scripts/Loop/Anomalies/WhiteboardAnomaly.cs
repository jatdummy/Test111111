using UnityEngine;

// 화이트보드: 이른 시간에는 회의 메모 마지막 줄만 슬쩍 바뀌고, 그 뒤로는 피로 쓴 글
public class WhiteboardAnomaly : Anomaly
{
    // 회의 메모의 마지막 줄
    [SerializeField] private TextMesh _lastLine;

    // 이른 시간 이상현상에서 바뀐 마지막 줄
    [SerializeField] private string _changedLine = "· 퇴근 없음";

    // 피로 쓴 글 묶음 (글자와 흘러내린 자국)
    [SerializeField] private GameObject _bloodWriting;

    // 피로 쓴 글자
    [SerializeField] private TextMesh _bloodMessage;

    // 피로 쓴 서명 글자 (서명이 없는 문구면 숨김)
    [SerializeField] private TextMesh _bloodSignature;

    // 피로 쓰는 문구 (나올 때마다 하나를 고름)
    [SerializeField] private string[] _bloodPhrases =
    {
        "제발\n탈출시켜줘",
        "코드를\n믿지마",
        "06시는\n오지 않아",
        "뒤에\n있어",
        "나도\n승인했었어",
    };

    // 문구마다 붙는 서명 (_bloodPhrases와 같은 순서, 빈 칸이면 서명 없음)
    [SerializeField] private string[] _bloodSignatures = { "", "", "- 01", "", "- 07" };

    // 피로 쓴 글이 나오기 시작하는 경과 시간 (22:00 기준, 3이면 01:00)
    [SerializeField, Min(0)] private int _bloodStartHour = 3;

    private string _originalLine;

    private void Awake() => _originalLine = _lastLine.text;

    public override void Apply(int hour)
    {
        if (hour < _bloodStartHour)
        {
            _lastLine.text = _changedLine;
            return;
        }

        int index = Random.Range(0, _bloodPhrases.Length);
        _bloodMessage.text = _bloodPhrases[index];

        // 서명은 문구를 쓴 사람마다 다르고, 없는 문구도 있음
        string signature = index < _bloodSignatures.Length ? _bloodSignatures[index] : string.Empty;
        _bloodSignature.text = signature;
        _bloodSignature.gameObject.SetActive(!string.IsNullOrEmpty(signature));
        _bloodWriting.SetActive(true);
    }

    public override void Revert()
    {
        _lastLine.text = _originalLine;
        _bloodWriting.SetActive(false);
    }
}
