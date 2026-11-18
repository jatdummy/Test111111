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

    public void Set(int index, Color color)
    {
        _lights[index].color = color;
        foreach (Material material in _lampRenderers[index].materials)
        {
            if (!material.IsKeywordEnabled("_EMISSION")) continue;

            material.SetColor("_BaseColor", color);
            material.SetColor("_EmissionColor", color * EmissionIntensity);
        }
    }
}
