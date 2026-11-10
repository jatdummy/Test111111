using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// 사무실 씬·책상 칸 프리팹·소품 묶음 프리팹·원본 데이터 에셋 일괄 생성 에디터 도구
public static class OfficeSceneSetup
{
    // 에셋 경로
    private const string ScenePath = "Assets/Scenes/Office.unity";
    private const string PrefabFolder = "Assets/Prefabs/Office";
    private const string DeskUnitPrefabPath = PrefabFolder + "/DeskUnit.prefab";
    private const string OfficePropsPrefabPath = PrefabFolder + "/OfficeProps.prefab";
    private const string DataFolder = "Assets/Data/Office";
    private const string BaseOfficeAssetPath = DataFolder + "/BaseOffice.asset";
    private const string InputActionsPath = "Assets/Input/InputSystem_Actions.inputactions";
    private const string PlayerActionMapName = "Player";

    private const string FloorMaterialPath = "Assets/Materials/Corridor_Floor.mat";
    private const string WallMaterialPath = "Assets/Materials/Corridor_Wall.mat";
    private const string CeilingMaterialPath = "Assets/Materials/Corridor_Ceiling.mat";
    private const string DarkMaterialPath = "Assets/Materials/Corridor_Dark.mat";
    private const string LightOnMaterialPath = "Assets/Materials/Light_On.mat";
    private const string LightOffMaterialPath = "Assets/Materials/Light_Off.mat";
    private const string SignMaterialPath = "Assets/Materials/Sign_Yellow.mat";

    // 외부 에셋 프리팹 폴더와 파일명
    private const string ModelPrefabFolder = "Assets/Imports/Modern Resaturant - building and interior/Models/Prefabs";
    private const string DeskModelFile = "Principal_office_desk.prefab";
    private const string ChairModelFile = "principal_office_chair_01.prefab";
    private const string LaptopModelFile = "laptop_1.prefab";
    private const string LampModelFile = "walk_in_freezer_luminescent_lamp.prefab";
    private const string KanbanBoardModelFile = "Big_board.prefab";
    private const string WhiteboardModelFile = "Small_board.prefab";
    private const string ExtinguisherModelFile = "Extinguisher.prefab";

    // A, B, C 순서의 포스터 파일명
    private static readonly string[] PosterModelFiles = { "SM_Picture_V_1.prefab", "SM_Picture_V_2.prefab", "SM_Picture_V_3.prefab" };

    // 에셋별 앞뒤 방향 보정용 Y 회전(화면에서 뒤집혀 보이면 180)
    private const float DeskYRotation = 0f;
    private const float ChairYRotation = 0f;
    private const float LaptopYRotation = 0f;
    private const float LampYRotation = 0f;
    private const float PosterYRotation = 0f;
    private const float KanbanBoardYRotation = 0f;
    private const float WhiteboardYRotation = 0f;
    private const float ExtinguisherYRotation = 0f;

    // 긴 축 맞춤용 회전 각도
    private const float QuarterTurn = 90f;

    private const string PlayerTag = "Player";
    private const string MainCameraTag = "MainCamera";

    // 건물 뼈대 치수(m)
    private const float HallStartZ = -6f;
    private const float CorridorStartZ = 0f;
    private const float HalfWidth = 2.2f;
    private const float RoomHeight = 3f;
    private const float WallThickness = 0.2f;

    // 통로 길이, 원본 사무실 데이터 officeLength와 공용
    private const float OfficeLength = 20f;
    private const float CorridorEndZ = CorridorStartZ + OfficeLength;

    // 문 시각물 치수(m)
    private const float DoorHeight = 2.2f;
    private const float ElevatorDoorWidth = 1.4f;
    private const float ReleaseDoorWidth = 1.2f;
    private const float WallDecalDepth = 0.05f;

    // 책상 축소 목표 크기(m), 길이는 복도 방향, 깊이는 벽에서 통로 방향
    private const float DeskTargetLength = 1.4f;
    private const float DeskTargetDepth = 0.8f;

    // 책상 앞면과 의자 사이 간격(m)
    private const float ChairDeskGap = 0f;

    // 경고 기준이 되는 가운데 최소 보행 폭(m)
    private const float MinWalkWidth = 1f;

    // 노트북을 책상 중심에서 벽 쪽으로 치우치는 비율(책상 깊이 절반 기준)
    private const float LaptopWallBias = 0.3f;

    // 책상 칸 배치
    private const float LeftDeskYRotation = 90f;
    private const float RightDeskYRotation = -90f;
    private static readonly float[] DeskSlotZ = { 4f, 10f, 16f };

    // 천장 형광등
    private const int CeilingLightCount = 6;
    private const float CeilingLightStartZ = -3f;
    private const float CeilingLightSpacing = 4.4f;
    private const float CeilingLightRange = 7f;
    private const float CeilingLightIntensity = 1.5f;

    // 포스터, 오른쪽 벽
    private const float PosterStartZ = 6f;
    private const float PosterSpacing = 1f;
    private const float PosterCenterY = 1.7f;

    // 칸반 보드와 화이트보드, 왼쪽 벽
    private const float KanbanBoardZ = 7f;
    private const float WhiteboardZ = 13f;
    private const float BoardCenterY = 1.5f;

    // 벽시계, 오른쪽 벽
    private const float ClockZ = 13f;
    private const float ClockCenterY = 2.2f;
    private const float ClockDiameter = 0.4f;
    private const float ClockThickness = 0.03f;
    private const float ClockHandWidth = 0.02f;
    private const float ClockHandThickness = 0.01f;
    private const float MinuteHandLength = 0.16f;
    private const float HourHandLength = 0.1f;

    // 새벽 2시를 가리키는 12시 기준 시계 방향 각도
    private const float MinuteHandAngle = 0f;
    private const float HourHandAngle = 60f;

    // 소화기, 오른쪽 벽 앞
    private const float ExtinguisherZ = 19f;

    // 비상구 표지판, 릴리즈 문 위
    private const float ExitSignCenterY = 2.6f;
    private static readonly Vector3 ExitSignPanelSize = new(0.8f, 0.3f, 0.05f);
    private static readonly Vector2 ExitSignTextSize = new(0.7f, 0.25f);
    private const float ExitSignFontSizeMin = 0.5f;
    private const float ExitSignFontSizeMax = 3f;

    // 조명
    private const float DirectionalLightIntensity = 0.15f;
    private static readonly Vector3 DirectionalLightEuler = new(60f, 30f, 0f);
    private static readonly Color AmbientColor = new(0.12f, 0.12f, 0.14f);

    // 플레이어
    private static readonly Vector3 StartPosition = new(0f, 0f, -4f);
    private const float PlayerHeight = 1.8f;
    private const float PlayerRadius = 0.3f;
    private const float CameraHeight = 1.6f;
    private const float CameraNearClip = 0.05f;

    // 빌드 모니터, 판 뒷면이 왼쪽 벽 안쪽 면에 닿는 위치
    private static readonly Vector3 BuildMonitorPanelSize = new(2.4f, 1.2f, 0.05f);
    private static readonly Vector3 BuildMonitorPosition = new(-HalfWidth + BuildMonitorPanelSize.z * 0.5f, 1.6f, -2.5f);
    private const float BuildMonitorYRotation = -90f;
    private static readonly Vector2 BuildMonitorTextSize = new(2.2f, 1.0f);
    private const float BuildMonitorFontSizeMin = 0.5f;
    private const float BuildMonitorFontSizeMax = 1.5f;

    // 엘리베이터 버튼
    private static readonly Vector3 ElevatorButtonPosition = new(1.2f, 1.2f, -5.95f);
    private static readonly Vector3 ElevatorButtonSize = new(0.15f, 0.15f, 0.05f);
    private static readonly Vector3 ElevatorZonePosition = new(1.2f, 1f, -5.15f);
    private static readonly Vector3 ElevatorZoneSize = new(1.6f, 2f, 1.5f);

    // 진행도 표시
    private static readonly Vector3 ProgressPosition = new(0f, 2.6f, -5.95f);
    private const float ProgressYRotation = 180f;
    private static readonly Vector2 ProgressTextSize = new(2f, 0.4f);
    private const float ProgressFontSizeMin = 1f;
    private const float ProgressFontSizeMax = 4f;

    // 릴리즈 문 트리거, 끝벽에서 안쪽으로 들인 거리(m)
    private const float ReleaseTriggerInset = 0.7f;
    private static readonly Vector3 ReleaseTriggerPosition = new(0f, 1.5f, CorridorEndZ - ReleaseTriggerInset);
    private static readonly Vector3 ReleaseTriggerSize = new(HalfWidth * 2f, 3f, 1f);

    // 텍스트를 판 앞으로 띄우는 거리(m)
    private const float TextFrontOffset = 0.03f;

    [MenuItem("Tools/8번 코딩/사무실 씬 생성")]
    private static void CreateOfficeScene()
    {
        bool hasScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
        if (hasScene && !EditorUtility.DisplayDialog("사무실 씬 생성", $"{ScenePath}이(가) 이미 있습니다. 덮어쓸까요?", "덮어쓰기", "취소")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(PrefabFolder);
        EnsureFolder(DataFolder);

        GameObject deskUnitPrefab = CreateDeskUnitPrefab(out Bounds deskBounds, out Bounds chairBounds);
        OfficeProps officePropsPrefab = CreateOfficePropsPrefab();
        BaseOfficeData baseOfficeData = CreateBaseOfficeData(deskUnitPrefab, deskBounds.size.z, officePropsPrefab);

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateBuilding();
        CreateLighting();
        FirstPersonController player = CreatePlayer();
        Transform startPoint = CreateStartPoint();
        BuildMonitorView buildMonitorView = CreateBuildMonitor();
        ElevatorButton elevatorButton = CreateElevatorButton();
        ProgressIndicatorView progressIndicatorView = CreateProgressIndicator();
        ReleaseDoorTrigger releaseDoorTrigger = CreateReleaseDoorTrigger();
        OfficeBuilder officeBuilder = CreateOfficeBuilder();

        var loopManagerObject = new GameObject("LoopManager");
        LoopManager loopManager = loopManagerObject.AddComponent<LoopManager>();
        var loopManagerSerialized = new SerializedObject(loopManager);
        loopManagerSerialized.FindProperty("_baseOfficeData").objectReferenceValue = baseOfficeData;
        loopManagerSerialized.FindProperty("_officeBuilder").objectReferenceValue = officeBuilder;
        loopManagerSerialized.FindProperty("_player").objectReferenceValue = player;
        loopManagerSerialized.FindProperty("_startPoint").objectReferenceValue = startPoint;
        loopManagerSerialized.FindProperty("_buildMonitorView").objectReferenceValue = buildMonitorView;
        loopManagerSerialized.FindProperty("_progressIndicatorView").objectReferenceValue = progressIndicatorView;
        loopManagerSerialized.FindProperty("_elevatorButton").objectReferenceValue = elevatorButton;
        loopManagerSerialized.FindProperty("_releaseDoorTrigger").objectReferenceValue = releaseDoorTrigger;
        loopManagerSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        LogResult(deskBounds, chairBounds);
    }

    // 상위 폴더부터 차례로 없는 폴더 생성
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        int separatorIndex = path.LastIndexOf('/');
        string parent = path.Substring(0, separatorIndex);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separatorIndex + 1));
    }

    // 책상·노트북·의자 에셋으로 구성된 책상 칸 프리팹 저장, 측정한 책상·의자 bounds 반환
    private static GameObject CreateDeskUnitPrefab(out Bounds deskBounds, out Bounds chairBounds)
    {
        var root = new GameObject("DeskUnit");
        deskBounds = new Bounds();
        chairBounds = new Bounds();

        GameObject desk = InstantiateModel(DeskModelFile, root.transform, DeskYRotation);
        if (desk != null)
        {
            OrientLongAxis(desk, false);
            ScaleDeskToTarget(desk);
            AlignBounds(desk, Vector3.down, Vector3.zero);
            deskBounds = MeasureBounds(desk);
        }

        // 책상 윗면 위, 벽 쪽으로 치우친 위치
        GameObject laptop = InstantiateModel(LaptopModelFile, root.transform, LaptopYRotation);
        if (laptop != null)
            AlignBounds(laptop, Vector3.down, new Vector3(0f, deskBounds.max.y, -deskBounds.extents.z * LaptopWallBias));

        // 의자의 책상 쪽 면을 책상 앞면에서 간격만큼 띄운 위치
        GameObject chair = InstantiateModel(ChairModelFile, root.transform, ChairYRotation);
        if (chair != null)
        {
            AlignBounds(chair, Vector3.down + Vector3.back, new Vector3(0f, 0f, deskBounds.max.z + ChairDeskGap));
            chairBounds = MeasureBounds(chair);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DeskUnitPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // 긴 축 정렬 뒤 측정 크기 기준 목표 길이·깊이로의 책상 축소, 높이 유지
    private static void ScaleDeskToTarget(GameObject desk)
    {
        Vector3 size = MeasureBounds(desk).size;

        // 회전된 모델에서 월드 X(길이)·Z(깊이)에 대응하는 로컬 축
        Vector3 lengthAxis = ToAbsolute(desk.transform.InverseTransformDirection(Vector3.right));
        Vector3 depthAxis = ToAbsolute(desk.transform.InverseTransformDirection(Vector3.forward));

        Vector3 scale = Vector3.up + lengthAxis * (DeskTargetLength / size.x) + depthAxis * (DeskTargetDepth / size.z);
        // 수정 기록은 바로 뒤 AlignBounds의 transform 기록에 포함
        desk.transform.localScale = Vector3.Scale(desk.transform.localScale, scale);
    }

    private static Vector3 ToAbsolute(Vector3 value) => new(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

    // 완료 로그와 측정 크기 출력, 통로가 좁으면 경고
    private static void LogResult(Bounds deskBounds, Bounds chairBounds)
    {
        // 벽에서 의자의 통로 쪽 끝까지의 거리
        float unitDepth = Mathf.Max(deskBounds.max.z, chairBounds.max.z) - deskBounds.min.z;
        float walkWidth = (HalfWidth - unitDepth) * 2f;

        Debug.Log($"사무실 씬 생성 완료 : {ScenePath}\n책상 크기 {deskBounds.size}, 의자 크기 {chairBounds.size}, 책상 칸 전체 깊이 {unitDepth:F2}m, 가운데 보행 폭 {walkWidth:F2}m");

        if (walkWidth < MinWalkWidth)
            Debug.LogWarning($"가운데 보행 폭 {walkWidth:F2}m가 최소 보행 폭 {MinWalkWidth:F2}m보다 좁습니다.");
    }

    // 형광등·포스터·보드·시계·소화기·표지판 소품 묶음 프리팹 저장
    private static OfficeProps CreateOfficePropsPrefab()
    {
        var root = new GameObject("OfficeProps");
        Transform parent = root.transform;
        OfficeProps props = root.AddComponent<OfficeProps>();

        var serialized = new SerializedObject(props);
        SetObjectArray(serialized.FindProperty("_ceilingLights"), CreateCeilingLights(parent));
        SetObjectArray(serialized.FindProperty("_posters"), CreatePosters(parent));
        serialized.FindProperty("_kanbanBoard").objectReferenceValue = PlaceOnWall(KanbanBoardModelFile, parent, KanbanBoardYRotation, Vector3.left, BoardCenterY, KanbanBoardZ);
        serialized.FindProperty("_whiteboard").objectReferenceValue = PlaceOnWall(WhiteboardModelFile, parent, WhiteboardYRotation, Vector3.left, BoardCenterY, WhiteboardZ);
        serialized.FindProperty("_clock").objectReferenceValue = CreateClock(parent);
        serialized.FindProperty("_exitSign").objectReferenceValue = CreateExitSign(parent);
        serialized.FindProperty("_extinguisher").objectReferenceValue = CreateExtinguisher(parent);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, OfficePropsPrefabPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<OfficeProps>();
    }

    private static void SetObjectArray(SerializedProperty property, List<Object> values)
    {
        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    // 천장에 붙인 형광등 모델과 실시간 조명 생성
    private static List<Object> CreateCeilingLights(Transform parent)
    {
        var lights = new List<Object>();
        for (int i = 0; i < CeilingLightCount; i++)
        {
            GameObject lamp = InstantiateModel(LampModelFile, parent, LampYRotation);
            if (lamp == null) break;

            lamp.name = $"CeilingLamp_{i + 1}";
            PrefabUtility.RecordPrefabInstancePropertyModifications(lamp);
            OrientLongAxis(lamp, true);
            AlignBounds(lamp, Vector3.up, new Vector3(0f, RoomHeight, CeilingLightStartZ + CeilingLightSpacing * i));
            lights.Add(SetUpLampLight(lamp));
        }

        return lights;
    }

    // 램프 에셋에 든 조명 값 덮어쓰기
    private static Light SetUpLampLight(GameObject lamp)
    {
        Light lampLight = lamp.GetComponentInChildren<Light>();
        lampLight.type = LightType.Point;
        lampLight.color = Color.white;
        lampLight.range = CeilingLightRange;
        lampLight.intensity = CeilingLightIntensity;
        lampLight.shadows = LightShadows.None;
        // 스크립트로 바꾼 프리팹 인스턴스 값의 저장 누락 방지
        PrefabUtility.RecordPrefabInstancePropertyModifications(lampLight);
        return lampLight;
    }

    // 오른쪽 벽에 A, B, C 순서로 포스터 배치
    private static List<Object> CreatePosters(Transform parent)
    {
        var posters = new List<Object>();
        for (int i = 0; i < PosterModelFiles.Length; i++)
            posters.Add(PlaceOnWall(PosterModelFiles[i], parent, PosterYRotation, Vector3.right, PosterCenterY, PosterStartZ + PosterSpacing * i));

        return posters;
    }

    // 얇은 축을 벽 법선으로 돌려 뒷면을 벽 안쪽 면에 맞춘 배치
    private static Transform PlaceOnWall(string fileName, Transform parent, float yRotation, Vector3 wallSide, float centerY, float z)
    {
        GameObject model = InstantiateModel(fileName, parent, yRotation);
        if (model == null) return null;

        OrientLongAxis(model, true);
        AlignBounds(model, wallSide, new Vector3(wallSide.x * HalfWidth, centerY, z));
        return model.transform;
    }

    // 원판과 바늘 두 개로 만든 임시 벽시계 생성
    private static Transform CreateClock(Transform parent)
    {
        var clock = new GameObject("Clock");
        clock.transform.SetParent(parent, false);
        clock.transform.localPosition = new Vector3(HalfWidth - ClockThickness * 0.5f, ClockCenterY, ClockZ);

        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        face.name = "Face";
        face.transform.SetParent(clock.transform, false);
        // 원기둥 축(Y)을 벽 법선(X)으로 눕힘
        face.transform.localRotation = Quaternion.Euler(0f, 0f, QuarterTurn);
        // 기본 원기둥 높이 2에 맞춘 두께 절반 스케일
        face.transform.localScale = new Vector3(ClockDiameter, ClockThickness * 0.5f, ClockDiameter);
        Object.DestroyImmediate(face.GetComponent<Collider>());

        CreateClockHand("MinuteHand", clock.transform, MinuteHandLength, MinuteHandAngle);
        CreateClockHand("HourHand", clock.transform, HourHandLength, HourHandAngle);
        return clock.transform;
    }

    // 12시 기준 시계 방향 각도의 바늘 생성
    private static void CreateClockHand(string name, Transform clock, float length, float clockAngle)
    {
        // 오른쪽 벽을 보는 시점의 시계 방향은 X축 음의 회전
        Quaternion rotation = Quaternion.Euler(-clockAngle, 0f, 0f);
        Vector3 position = rotation * Vector3.up * (length * 0.5f) + Vector3.left * ((ClockThickness + ClockHandThickness) * 0.5f);
        GameObject hand = CreateCube(name, clock, position, new Vector3(ClockHandThickness, length, ClockHandWidth), LoadMaterial(DarkMaterialPath), false);
        hand.transform.localRotation = rotation;
    }

    // 판과 EXIT 글자로 만든 임시 비상구 표지판 생성
    private static Transform CreateExitSign(Transform parent)
    {
        var sign = new GameObject("ExitSign");
        sign.transform.SetParent(parent, false);
        sign.transform.localPosition = new Vector3(0f, ExitSignCenterY, CorridorEndZ - ExitSignPanelSize.z * 0.5f);

        CreateCube("Panel", sign.transform, Vector3.zero, ExitSignPanelSize, LoadMaterial(LightOnMaterialPath), false);

        // 회전 없이 통로 쪽(-Z)에서 읽히는 방향
        TextMeshPro label = CreateText("Label", sign.transform, ExitSignTextSize, ExitSignFontSizeMin, ExitSignFontSizeMax, TextAlignmentOptions.Center);
        label.transform.localPosition = new Vector3(0f, 0f, -(ExitSignPanelSize.z * 0.5f + TextFrontOffset));
        label.text = "EXIT";
        // 밝은 판 위 가독성 확보
        label.color = Color.black;
        return sign.transform;
    }

    // 오른쪽 벽에 붙여 바닥에 세운 소화기 배치
    private static Transform CreateExtinguisher(Transform parent)
    {
        GameObject extinguisher = InstantiateModel(ExtinguisherModelFile, parent, ExtinguisherYRotation);
        if (extinguisher == null) return null;

        AlignBounds(extinguisher, Vector3.down + Vector3.right, new Vector3(HalfWidth, 0f, ExtinguisherZ));
        return extinguisher.transform;
    }

    // 외부 에셋 프리팹 인스턴스 생성, 없으면 오류 로그 후 null 반환
    private static GameObject InstantiateModel(string fileName, Transform parent, float yRotation)
    {
        string path = $"{ModelPrefabFolder}/{fileName}";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"프리팹을 찾지 못해 건너뜁니다 : {path}");
            return null;
        }

        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        // 프리팹 자체 회전 위에 방향 보정 회전 적용
        model.transform.rotation = Quaternion.Euler(0f, yRotation, 0f) * model.transform.rotation;
        return model;
    }

    // 자식 Renderer 전체를 합친 월드 bounds 측정
    private static Bounds MeasureBounds(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(model.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
            bounds.Encapsulate(renderer.bounds);

        return bounds;
    }

    // 가로 방향 긴 축이 X 또는 Z가 되도록 필요 시 90도 회전
    private static void OrientLongAxis(GameObject model, bool isAlongZ)
    {
        Vector3 size = MeasureBounds(model).size;
        bool isLongAxisZ = size.z > size.x;
        if (isLongAxisZ == isAlongZ) return;

        model.transform.rotation = Quaternion.Euler(0f, QuarterTurn, 0f) * model.transform.rotation;
    }

    // bounds 기준면(축별 -1 최소, 0 중심, 1 최대)을 목표 좌표로 옮기는 이동
    private static void AlignBounds(GameObject model, Vector3 anchor, Vector3 target)
    {
        Bounds bounds = MeasureBounds(model);
        Vector3 anchorPoint = bounds.center + Vector3.Scale(bounds.extents, anchor);
        model.transform.position += target - anchorPoint;
        // 스크립트로 바꾼 프리팹 인스턴스 값의 저장 누락 방지
        PrefabUtility.RecordPrefabInstancePropertyModifications(model.transform);
    }

    // 원본 사무실 데이터 에셋 생성 또는 갱신
    private static BaseOfficeData CreateBaseOfficeData(GameObject deskUnitPrefab, float deskDepth, OfficeProps officePropsPrefab)
    {
        BaseOfficeData data = AssetDatabase.LoadAssetAtPath<BaseOfficeData>(BaseOfficeAssetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BaseOfficeData>();
            AssetDatabase.CreateAsset(data, BaseOfficeAssetPath);
        }

        var serialized = new SerializedObject(data);
        serialized.FindProperty("_deskUnitPrefab").objectReferenceValue = deskUnitPrefab;
        serialized.FindProperty("_officePropsPrefab").objectReferenceValue = officePropsPrefab;
        serialized.FindProperty("_officeLength").floatValue = OfficeLength;

        // 책상 뒤쪽 면이 벽 안쪽 면에 닿는 책상 중심 x
        float slotOffsetX = HalfWidth - deskDepth * 0.5f;

        SerializedProperty slots = serialized.FindProperty("_deskSlots");
        slots.arraySize = DeskSlotZ.Length * 2;
        for (int i = 0; i < DeskSlotZ.Length; i++)
        {
            SetDeskSlot(slots.GetArrayElementAtIndex(i * 2), new Vector3(-slotOffsetX, 0f, DeskSlotZ[i]), LeftDeskYRotation);
            SetDeskSlot(slots.GetArrayElementAtIndex(i * 2 + 1), new Vector3(slotOffsetX, 0f, DeskSlotZ[i]), RightDeskYRotation);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void SetDeskSlot(SerializedProperty slot, Vector3 position, float yRotation)
    {
        slot.FindPropertyRelative("_position").vector3Value = position;
        slot.FindPropertyRelative("_yRotation").floatValue = yRotation;
    }

    // 홀·통로 바닥, 천장, 벽, 문 시각물 생성
    private static void CreateBuilding()
    {
        var building = new GameObject("Building");
        Transform parent = building.transform;

        float length = CorridorEndZ - HallStartZ;
        float centerZ = (HallStartZ + CorridorEndZ) * 0.5f;
        float width = HalfWidth * 2f;
        float outerWidth = width + WallThickness * 2f;
        float outerLength = length + WallThickness * 2f;
        float halfThickness = WallThickness * 0.5f;
        float wallCenterY = RoomHeight * 0.5f;

        Material wallMaterial = LoadMaterial(WallMaterialPath);

        CreateCube("Floor", parent, new Vector3(0f, -halfThickness, centerZ), new Vector3(outerWidth, WallThickness, outerLength), LoadMaterial(FloorMaterialPath), true);
        CreateCube("Ceiling", parent, new Vector3(0f, RoomHeight + halfThickness, centerZ), new Vector3(outerWidth, WallThickness, outerLength), LoadMaterial(CeilingMaterialPath), true);
        CreateCube("LeftWall", parent, new Vector3(-HalfWidth - halfThickness, wallCenterY, centerZ), new Vector3(WallThickness, RoomHeight, length), wallMaterial, true);
        CreateCube("RightWall", parent, new Vector3(HalfWidth + halfThickness, wallCenterY, centerZ), new Vector3(WallThickness, RoomHeight, length), wallMaterial, true);
        CreateCube("ElevatorWall", parent, new Vector3(0f, wallCenterY, HallStartZ - halfThickness), new Vector3(width, RoomHeight, WallThickness), wallMaterial, true);
        CreateCube("EndWall", parent, new Vector3(0f, wallCenterY, CorridorEndZ + halfThickness), new Vector3(width, RoomHeight, WallThickness), wallMaterial, true);

        float doorCenterY = DoorHeight * 0.5f;
        float decalOffset = WallDecalDepth * 0.5f;
        CreateCube("ElevatorDoor", parent, new Vector3(0f, doorCenterY, HallStartZ + decalOffset), new Vector3(ElevatorDoorWidth, DoorHeight, WallDecalDepth), LoadMaterial(DarkMaterialPath), false);
        CreateCube("ReleaseDoor", parent, new Vector3(0f, doorCenterY, CorridorEndZ - decalOffset), new Vector3(ReleaseDoorWidth, DoorHeight, WallDecalDepth), LoadMaterial(SignMaterialPath), false);

        foreach (Transform child in parent)
            child.gameObject.isStatic = true;

        building.isStatic = true;
    }

    // 약한 방향광과 어두운 앰비언트 설정
    private static void CreateLighting()
    {
        var lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(DirectionalLightEuler);
        Light directionalLight = lightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = DirectionalLightIntensity;
        // 천장에 막혀 실내가 그림자로 덮이는 것 방지
        directionalLight.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientColor;
    }

    // 캐릭터 컨트롤러와 카메라를 갖춘 플레이어 생성
    private static FirstPersonController CreatePlayer()
    {
        var playerObject = new GameObject("Player");
        playerObject.tag = PlayerTag;
        playerObject.transform.position = StartPosition;

        FirstPersonController controller = playerObject.AddComponent<FirstPersonController>();
        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        characterController.height = PlayerHeight;
        characterController.center = new Vector3(0f, PlayerHeight * 0.5f, 0f);
        characterController.radius = PlayerRadius;

        var cameraPivot = new GameObject("CameraPivot");
        cameraPivot.tag = MainCameraTag;
        cameraPivot.transform.SetParent(playerObject.transform, false);
        cameraPivot.transform.localPosition = new Vector3(0f, CameraHeight, 0f);
        Camera playerCamera = cameraPivot.AddComponent<Camera>();
        playerCamera.nearClipPlane = CameraNearClip;
        cameraPivot.AddComponent<AudioListener>();

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("_moveAction").objectReferenceValue = FindActionReference("Move");
        serialized.FindProperty("_lookAction").objectReferenceValue = FindActionReference("Look");
        serialized.FindProperty("_cameraPivot").objectReferenceValue = cameraPivot.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return controller;
    }

    private static Transform CreateStartPoint()
    {
        var startPoint = new GameObject("StartPoint");
        startPoint.transform.SetPositionAndRotation(StartPosition, Quaternion.identity);
        return startPoint.transform;
    }

    // 홀 왼쪽 벽 빌드 모니터 생성
    private static BuildMonitorView CreateBuildMonitor()
    {
        var monitor = new GameObject("BuildMonitor");
        monitor.transform.SetPositionAndRotation(BuildMonitorPosition, Quaternion.Euler(0f, BuildMonitorYRotation, 0f));

        CreateCube("Panel", monitor.transform, Vector3.zero, BuildMonitorPanelSize, LoadMaterial(DarkMaterialPath), false);

        TextMeshPro codeText = CreateText("CodeText", monitor.transform, BuildMonitorTextSize, BuildMonitorFontSizeMin, BuildMonitorFontSizeMax, TextAlignmentOptions.TopLeft);
        codeText.transform.localPosition = new Vector3(0f, 0f, -(BuildMonitorPanelSize.z * 0.5f + TextFrontOffset));

        BuildMonitorView view = monitor.AddComponent<BuildMonitorView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("_codeText").objectReferenceValue = codeText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    // 엘리베이터 버튼 시각물과 상호작용 범위 생성
    private static ElevatorButton CreateElevatorButton()
    {
        Material lightOffMaterial = LoadMaterial(LightOffMaterialPath);
        GameObject buttonVisual = CreateCube("ElevatorButtonVisual", null, ElevatorButtonPosition, ElevatorButtonSize, lightOffMaterial, false);

        var zone = new GameObject("ElevatorButtonZone");
        zone.transform.position = ElevatorZonePosition;
        BoxCollider zoneCollider = zone.AddComponent<BoxCollider>();
        zoneCollider.isTrigger = true;
        zoneCollider.size = ElevatorZoneSize;

        ElevatorButton button = zone.AddComponent<ElevatorButton>();
        var serialized = new SerializedObject(button);
        serialized.FindProperty("_interactAction").objectReferenceValue = FindActionReference("Interact");
        serialized.FindProperty("_buttonRenderer").objectReferenceValue = buttonVisual.GetComponent<Renderer>();
        serialized.FindProperty("_activeMaterial").objectReferenceValue = LoadMaterial(LightOnMaterialPath);
        serialized.FindProperty("_inactiveMaterial").objectReferenceValue = lightOffMaterial;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    // 엘리베이터 문 위 진행도 표시 생성
    private static ProgressIndicatorView CreateProgressIndicator()
    {
        var indicator = new GameObject("ProgressIndicator");
        indicator.transform.SetPositionAndRotation(ProgressPosition, Quaternion.Euler(0f, ProgressYRotation, 0f));

        TextMeshPro label = CreateText("Label", indicator.transform, ProgressTextSize, ProgressFontSizeMin, ProgressFontSizeMax, TextAlignmentOptions.Center);

        ProgressIndicatorView view = indicator.AddComponent<ProgressIndicatorView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("_label").objectReferenceValue = label;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static ReleaseDoorTrigger CreateReleaseDoorTrigger()
    {
        var triggerObject = new GameObject("ReleaseDoorTrigger");
        triggerObject.transform.position = ReleaseTriggerPosition;
        BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = ReleaseTriggerSize;
        return triggerObject.AddComponent<ReleaseDoorTrigger>();
    }

    // 책상 칸 재생성 담당 오브젝트와 부모 생성
    private static OfficeBuilder CreateOfficeBuilder()
    {
        var builderObject = new GameObject("OfficeBuilder");
        var runtimeRoot = new GameObject("RuntimeObjects");
        runtimeRoot.transform.SetParent(builderObject.transform, false);

        OfficeBuilder builder = builderObject.AddComponent<OfficeBuilder>();
        var serialized = new SerializedObject(builder);
        serialized.FindProperty("_runtimeRoot").objectReferenceValue = runtimeRoot.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return builder;
    }

    // 지정 위치·크기의 큐브 생성, 필요 시 콜라이더 제거
    private static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material, bool hasCollider)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = size;

        if (material != null)
            cube.GetComponent<Renderer>().sharedMaterial = material;

        if (!hasCollider)
            Object.DestroyImmediate(cube.GetComponent<Collider>());

        return cube;
    }

    // 자동 크기 맞춤 3D 텍스트 생성
    private static TextMeshPro CreateText(string name, Transform parent, Vector2 size, float fontSizeMin, float fontSizeMax, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.rectTransform.sizeDelta = size;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSizeMin;
        text.fontSizeMax = fontSizeMax;
        text.color = Color.white;
        return text;
    }

    private static Material LoadMaterial(string path) => AssetDatabase.LoadAssetAtPath<Material>(path);

    // project-wide actions의 Player 맵 액션 참조 검색
    private static InputActionReference FindActionReference(string actionName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(InputActionsPath))
        {
            if (asset is not InputActionReference reference) continue;

            InputAction action = reference.action;
            if (action != null && action.name == actionName && action.actionMap.name == PlayerActionMapName)
                return reference;
        }

        Debug.LogError($"InputActionReference를 찾지 못했습니다 : {PlayerActionMapName}/{actionName} ({InputActionsPath})");
        return null;
    }
}
