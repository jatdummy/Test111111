using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 테스트용 가짜 이상현상을 만들고, 테스트가 끝나면 모두 지움 (씬, 파일에 남지 않음)
public class TestAnomalyFactory
{
    private readonly List<Object> _created = new();

    public Anomaly Create(string label, OverrideKind[] blockedOverrides = null, LightOverride requiredLight = LightOverride.None, bool canBeLegitimized = false, OverrideKind legitimizeOverride = OverrideKind.RedLight)
    {
        var data = ScriptableObject.CreateInstance<AnomalyData>();
        _created.Add(data);

        // 데이터 칸은 private이라 에디터의 SerializedObject로 채움 (게임 코드에 테스트용 setter를 만들지 않게)
        var serializedData = new SerializedObject(data);
        serializedData.FindProperty("_reportLabel").stringValue = label;
        SerializedProperty blocked = serializedData.FindProperty("_blockedOverrides");
        OverrideKind[] kinds = blockedOverrides ?? new OverrideKind[0];
        blocked.arraySize = kinds.Length;
        for (int i = 0; i < kinds.Length; i++)
        {
            blocked.GetArrayElementAtIndex(i).intValue = (int)kinds[i];
        }

        serializedData.FindProperty("_requiredLight").intValue = (int)requiredLight;
        serializedData.FindProperty("_canBeLegitimized").boolValue = canBeLegitimized;
        serializedData.FindProperty("_legitimizeOverride").intValue = (int)legitimizeOverride;
        serializedData.ApplyModifiedPropertiesWithoutUndo();

        var gameObject = new GameObject($"TestAnomaly_{label}");
        _created.Add(gameObject);
        // 에디터 전용 테스트 어셈블리의 컴포넌트는 오브젝트에 붙일 수 없어서, 게임 코드의 가장 단순한 이상현상을 씀 (출제 규칙만 확인하므로 동작은 쓰지 않음)
        var anomaly = gameObject.AddComponent<ActivateObjectAnomaly>();
        var serializedAnomaly = new SerializedObject(anomaly);
        serializedAnomaly.FindProperty("_data").objectReferenceValue = data;
        serializedAnomaly.ApplyModifiedPropertiesWithoutUndo();
        return anomaly;
    }

    public void DestroyAll()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }

        _created.Clear();
    }
}
