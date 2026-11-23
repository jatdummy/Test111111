using UnityEngine;

// 글자가 바뀜 (자리 명패 이름, 문 이름판, 표지판)
public class TextAnomaly : Anomaly
{
    // 바뀌는 글자
    [SerializeField] private TextMesh _text;

    // 이상현상일 때의 글자 (TextMesh 서식 태그 사용 가능)
    [SerializeField, TextArea] private string _changedText;

    private string _originalText;

    private void Awake() => _originalText = _text.text;

    public override void Apply(int hour) => _text.text = _changedText;

    public override void Revert() => _text.text = _originalText;
}
