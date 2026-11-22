using UnityEngine;

// 천장 형광등: 빛과 함께 형광등의 발광 재질 색을 바꿈 (몸체 재질은 그대로)
public class OfficeLights : MonoBehaviour
{
    private const float EmissionIntensity = 2f;

    // 형광등 빛
    [SerializeField] private Light[] _lights;

    // 형광등 몸체 (같은 순서)
    [SerializeField] private Renderer[] _lampRenderers;

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
        foreach (Material material in _lampRenderers[index].materials)
        {
            if (!material.IsKeywordEnabled("_EMISSION")) continue;

            material.SetColor("_EmissionColor", isLit ? light.color * EmissionIntensity : Color.black);
        }
    }

    public void Set(int index, Color color)
    {
        _lights[index].color = color;
        foreach (Material material in _lampRenderers[index].materials)
        {
            if (!material.IsKeywordEnabled("_EMISSION")) continue;

            material.SetColor("_BaseColor", color);
            // 깜빡임으로 꺼져 있는 순간에 색이 바뀌어도 발광은 꺼진 채로 둠 (다음에 켜질 때 새 색으로 켜짐)
            material.SetColor("_EmissionColor", _lights[index].enabled ? color * EmissionIntensity : Color.black);
        }
    }
}
