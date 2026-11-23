using UnityEngine;

// 화분 하나만 잎이 갈색으로 시들어 주저앉고, 바닥에 마른 잎이 떨어져 있음
public class WiltedPlantAnomaly : Anomaly
{
    // 시드는 잎
    [SerializeField] private Renderer _leaves;

    // 시든 잎 재질
    [SerializeField] private Material _deadMaterial;

    // 시든 잎 크기 (높이를 줄이고 옆으로 퍼뜨려 주저앉은 모양)
    [SerializeField] private Vector3 _wiltScale = new(1.15f, 0.55f, 1.15f);

    // 시든 잎이 복도 쪽으로 기우는 각도
    [SerializeField, Range(0f, 90f)] private float _wiltTilt = 18f;

    // 화분 앞 바닥에 떨어진 마른 잎
    [SerializeField] private GameObject _fallenLeaves;

    private Material _leafMaterial;
    private Vector3 _leafScale;
    private Quaternion _leafRotation;

    private void Awake()
    {
        _leafMaterial = _leaves.sharedMaterial;
        _leafScale = _leaves.transform.localScale;
        _leafRotation = _leaves.transform.localRotation;
    }

    public override void Apply(int hour)
    {
        _leaves.sharedMaterial = _deadMaterial;
        _leaves.transform.localScale = Vector3.Scale(_leafScale, _wiltScale);
        _leaves.transform.localRotation = _leafRotation * Quaternion.Euler(_wiltTilt, 0f, 0f);
        _fallenLeaves.SetActive(true);
    }

    public override void Revert()
    {
        _leaves.sharedMaterial = _leafMaterial;
        _leaves.transform.localScale = _leafScale;
        _leaves.transform.localRotation = _leafRotation;
        _fallenLeaves.SetActive(false);
    }
}
