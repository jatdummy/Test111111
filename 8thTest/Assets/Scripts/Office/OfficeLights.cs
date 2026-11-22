using UnityEngine;

// 천장 형광등: 빛과 함께 형광등의 발광 재질 색을 바꿈 (몸체 재질은 그대로)
public class OfficeLights : MonoBehaviour
{
    private const float EmissionIntensity = 2f;
    private const string EmissionKeyword = "_EMISSION";

    // 재질 색 속성 (이름 대신 번호로 찾아 매번 문자열을 비교하지 않게)
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // 형광등 빛
    [SerializeField] private Light[] _lights;

    // 형광등 몸체 (같은 순서)
    [SerializeField] private Renderer[] _lampRenderers;

    // 형광등마다 색을 바꿀 재질 (materials는 부를 때마다 배열을 새로 만들므로 처음 한 번만 가져옴)
    private Material[][] _lampMaterials;

    private void Awake() => CacheMaterials();

    public void SetAll(Color color)
    {
        for (int i = 0; i < _lights.Length; i++)
        {
            Set(i, color);
        }
    }

    // 형광등 하나를 켜고 끔 (깜빡임 이상현상, 색은 그대로 두고 빛과 발광만 끔)
    public void SetLit(int index, bool isLit)
    {
        Light light = _lights[index];
        light.enabled = isLit;
        foreach (Material material in _lampMaterials[index])
        {
            if (!material.IsKeywordEnabled(EmissionKeyword)) continue;

            material.SetColor(EmissionColorId, isLit ? light.color * EmissionIntensity : Color.black);
        }
    }

    public void Set(int index, Color color)
    {
        _lights[index].color = color;
        foreach (Material material in _lampMaterials[index])
        {
            if (!material.IsKeywordEnabled(EmissionKeyword)) continue;

            material.SetColor(BaseColorId, color);
            // 깜빡임으로 꺼져 있는 순간에 색이 바뀌어도 발광은 꺼진 채로 둠 (다음에 켜질 때 새 색으로 켜짐)
            material.SetColor(EmissionColorId, _lights[index].enabled ? color * EmissionIntensity : Color.black);
        }
    }

    private void CacheMaterials()
    {
        _lampMaterials = new Material[_lampRenderers.Length][];
        for (int i = 0; i < _lampRenderers.Length; i++)
        {
            _lampMaterials[i] = _lampRenderers[i].materials;
        }
    }
}
