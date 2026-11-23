using UnityEngine;

// 텍스처 누락처럼 대상의 모든 재질이 분홍색으로 바뀜
public class MissingTextureAnomaly : Anomaly
{
    // 재질을 바꿀 대상
    [SerializeField] private Renderer _target;

    // 텍스처 누락 표시용 재질
    [SerializeField] private Material _missingMaterial;

    private Material[] _originalMaterials;

    private void Awake() => _originalMaterials = _target.sharedMaterials;

    public override void Apply(int hour)
    {
        var materials = new Material[_originalMaterials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = _missingMaterial;
        }

        _target.sharedMaterials = materials;
    }

    public override void Revert() => _target.sharedMaterials = _originalMaterials;
}
