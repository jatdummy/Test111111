using UnityEngine;

// 켜질 때부터 바닥을 따라 서서히 넓어짐 (고이는 핏물)
public class GrowOnEnable : MonoBehaviour
{
    // 처음 크기 배율
    [SerializeField] private float _startScale = 0.15f;

    // 다 넓어지는 데 걸리는 시간(초)
    [SerializeField] private float _growSeconds = 40f;

    private Vector3 _fullScale;
    private float _elapsed;

    private void Awake() => _fullScale = transform.localScale;

    private void OnEnable()
    {
        _elapsed = 0f;
        ApplyScale();
    }

    private void Update()
    {
        if (_elapsed >= _growSeconds) return;

        _elapsed += Time.deltaTime;
        ApplyScale();
    }

    // 두께는 그대로, 바닥 방향으로만 넓어짐
    private void ApplyScale()
    {
        float ratio = Mathf.Lerp(_startScale, 1f, Mathf.Clamp01(_elapsed / _growSeconds));
        transform.localScale = new Vector3(_fullScale.x * ratio, _fullScale.y, _fullScale.z * ratio);
    }
}
