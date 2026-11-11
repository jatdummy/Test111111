using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 전체 연출 UI(페이드, 큰 글자, 알림, 안내 문구)
public class ScreenOverlayView : MonoBehaviour
{
    [Header("페이드")]
    // 화면을 덮는 검은 이미지
    [SerializeField] private Image _fadeImage;

    [Header("글자")]
    // 화면 가운데 큰 글자
    [SerializeField] private Text _centerText;

    // 큰 글자 아래 작은 글자
    [SerializeField] private Text _subText;

    // 화면 위쪽 알림 글자
    [SerializeField] private Text _messageText;

    // 화면 아래쪽 규칙 안내 글자
    [SerializeField] private Text _hintText;

    // 상호작용 안내 글자
    [SerializeField] private Text _promptText;

    // 조준점
    [SerializeField] private GameObject _crosshair;

    private Coroutine _messageRoutine;

    // 화면을 지정 불투명도까지 서서히 덮기(1이면 완전히 검정)
    public IEnumerator FadeTo(float alpha, float duration)
    {
        float startAlpha = _fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFade(Mathf.Lerp(startAlpha, alpha, elapsed / duration));
            yield return null;
        }

        SetFade(alpha);
    }

    public void SetFade(float alpha)
    {
        Color color = _fadeImage.color;
        color.a = alpha;
        _fadeImage.color = color;
    }

    public void ShowCenter(string title, string sub)
    {
        _centerText.text = title;
        _subText.text = sub;
    }

    public void HideCenter() => ShowCenter(string.Empty, string.Empty);

    // 위쪽 알림을 일정 시간 표시
    public void ShowMessage(string message, float duration)
    {
        if (_messageRoutine != null)
            StopCoroutine(_messageRoutine);

        _messageRoutine = StartCoroutine(ShowMessageRoutine(message, duration));
    }

    public void ShowHint(string hint) => _hintText.text = hint;

    public void HideHint() => _hintText.text = string.Empty;

    public void ShowPrompt(string prompt) => _promptText.text = prompt;

    public void HidePrompt() => _promptText.text = string.Empty;

    public void SetCrosshairVisible(bool isVisible) => _crosshair.SetActive(isVisible);

    private IEnumerator ShowMessageRoutine(string message, float duration)
    {
        _messageText.text = message;
        yield return new WaitForSeconds(duration);
        _messageText.text = string.Empty;
        _messageRoutine = null;
    }
}
