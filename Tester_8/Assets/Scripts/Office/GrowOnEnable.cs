using UnityEngine;
using UnityEngine.Serialization;

// 켜질 때부터 서서히 커짐 (바닥을 따라 넓어지는 핏물, 또는 위로도 자라는 나무)
public class GrowOnEnable : MonoBehaviour
{
    // 처음 크기 배율
    [SerializeField, Min(0f)] private float _startScale = 0.15f;

    // 다 넓어지는 데 걸리는 시간(초)
    [SerializeField, Min(0.01f)] private float _growSeconds = 40f;

    // 위로도 자라는지 (끄면 두께는 그대로 바닥 방향으로만 넓어짐)
    [SerializeField, FormerlySerializedAs("_growsUp")] private bool _canGrowUp;

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

    // 핏물은 두께는 그대로 바닥 방향으로만, 나무는 위로도 함께 커짐
    private void ApplyScale()
    {
        float ratio = Mathf.Lerp(_startScale, 1f, Mathf.Clamp01(_elapsed / _growSeconds));
        float height = _canGrowUp ? _fullScale.y * ratio : _fullScale.y;
        transform.localScale = new Vector3(_fullScale.x * ratio, height, _fullScale.z * ratio);
    }
}
