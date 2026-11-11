using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// 사무실 그레이박스 씬·책상 칸 프리팹·소품 묶음 프리팹·원본 데이터 에셋 일괄 생성 에디터 도구
public static class OfficeSceneSetup
{
    // 에셋 경로
    private const string SceneFolder = "Assets/Scenes/Test";
    private const string ScenePath = SceneFolder + "/OfficeGraybox.unity";
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

    // 축 방향 부호
    private const float PositiveDirection = 1f;
    private const float NegativeDirection = -1f;

    private const string PlayerTag = "Player";
    private const string MainCameraTag = "MainCamera";

    // 공통 건물 치수(m)
    private const float RoomHeight = 3f;
    private const float WallThickness = 0.2f;
    private const float DoorHeight = 2.2f;
    private const float DoorLeafThickness = 0.05f;

    // 엘리베이터 홀 범위(m), 사무실과의 경계가 z=0
    private const float HallStartZ = -6f;
    private const float HallEndZ = 0f;
    private const float HallHalfWidth = 3f;

    // 사무실 범위(m), 통로 길이는 원본 사무실 데이터 officeLength와 공용
    private const float OfficeStartZ = HallEndZ;
    private const float OfficeLength = 20f;
    private const float OfficeEndZ = OfficeStartZ + OfficeLength;
    private const float OfficeHalfWidth = 4f;

    // 홀과 사무실 경계벽 게이트 폭(m)
    private const float GateWidth = 2f;

    // 가운데 통로 절반 폭(m), 책상 칸의 의자 쪽 끝이 닿는 x
    private const float AisleHalfWidth = 1f;

    // 끝벽 -x쪽 Merge 문 중심 x와 폭(m), 폭은 계단 폭과 같음
    private const float MergeDoorCenterX = -2f;
    private const float MergeDoorWidth = StairWidth;

    // 끝벽 +x쪽 탕비실 개구부 중심 x·폭과 안쪽 깊이(m)
    private const float PantryCenterX = 2f;
    private const float PantryOpeningWidth = 1.6f;
    private const float PantryDepth = 1f;

    // 홀 뒷벽 뒤 엘리베이터 칸 내부 폭·깊이와 문 개구부 폭(m)
    private const float ElevatorCarWidth = 2.8f;
    private const float ElevatorCarDepth = 2f;
    private const float ElevatorDoorWidth = 1.4f;
    private const float ElevatorCarCenterZ = HallStartZ - WallThickness - ElevatorCarDepth * 0.5f;

    private const float ElevatorCarBackZ = HallStartZ - WallThickness - ElevatorCarDepth;

    // 뒷벽 안쪽(칸 쪽) 면에 붙인 엘리베이터 문짝 z
    private const float ElevatorDoorZ = HallStartZ - WallThickness - DoorLeafThickness * 0.5f;

    // 엘리베이터 버튼, 칸 오른쪽 안벽
    private const float ElevatorButtonHeight = 1.2f;
    private static readonly Vector3 ElevatorButtonSize = new(0.05f, 0.15f, 0.15f);
    private const float ElevatorZoneHeight = 2f;

    // 버튼 범위 앞면 z, 문짝 안쪽 면에서 플레이어 지름만큼 들여 문짝과 겹친 채 누르는 것 방지
    private const float ElevatorZoneFrontZ = ElevatorDoorZ - DoorLeafThickness * 0.5f - PlayerDiameter;

    // 홀 왼쪽 벽 비상계단 문 폭(m)
    private const float StairDoorWidth = 1f;

    // 문 열림 판정 트리거의 가로·세로 크기(m)
    private const float DoorTriggerSize = 3f;

    // Merge 문 개폐 속도와 통과 판정 거리, 잠긴 직후 닫히는 틈으로 되돌아가는 것 방지
    private const float MergeDoorMoveSpeed = 6f;
    private const float MergeDoorPassDistance = 1f;

    // 비상계단 모듈 칸 수와 칸 치수(m), 모듈 원점은 위 계단 꼭대기 문턱 중앙 바닥
    private const int StairStepCount = 8;
    private const float StairStepHeight = 0.18f;
    private const float StairStepDepth = 0.28f;
    private const float StairWidth = 1.2f;

    // 위 계단 앞 평지와 아래 계단 끝 평지 깊이, 계단참 깊이(m)
    private const float StairEndAreaDepth = 1.6f;
    private const float StairLandingDepth = 1.4f;

    // 위 계단 꼭대기 바닥 기준 모듈 천장 높이(m)
    private const float StairCeilingHeight = 2.6f;

    // 이동용 경사 콜라이더 두께(m)
    private const float StairSlopeThickness = 0.2f;

    // 아래 계단 끝 평지 옆벽 문 중심의 모듈 기준 z
    private const float StairBottomDoorCenterZ = 0.8f;

    // 계단 한 줄 높이 차, 모듈 꼭대기 기준 계단참·아래 끝 바닥 높이
    private const float StairFlightRise = StairStepCount * StairStepHeight;
    private const float StairLandingY = -StairFlightRise;
    private const float StairBottomY = StairLandingY - StairFlightRise;

    // 모듈 기준 계단참 구간 z
    private const float StairLandingStartZ = StairEndAreaDepth + StairStepCount * StairStepDepth;
    private const float StairLandingEndZ = StairLandingStartZ + StairLandingDepth;

    // 모듈 기준 위 계단 줄(가운데 x=0) 절반 폭, 칸막이 건너 +x쪽 아래 계단 줄 범위
    private const float StairUpperLaneHalfWidth = StairWidth * 0.5f;
    private const float StairLowerLaneMinX = StairUpperLaneHalfWidth + WallThickness;
    private const float StairLowerLaneMaxX = StairLowerLaneMinX + StairWidth;
    private const float StairLowerLaneCenterX = StairLowerLaneMinX + StairWidth * 0.5f;
    private const float StairDividerCenterX = StairUpperLaneHalfWidth + WallThickness * 0.5f;

    // 모듈 벽과 맞붙은 건물 벽을 합친 출입구 깊이, 문짝 중심은 건물 벽 두께 가운데
    private const float StairDoorwayDepth = WallThickness + WallThickness;
    private const float StairDoorLeafDepth = WallThickness + WallThickness * 0.5f;

    // 모듈 A 원점, 위 계단 앞벽이 사무실 끝벽 바깥면에 붙어 Merge 문 바로 뒤에서 시작
    private static readonly Vector3 StairModuleAOrigin = new(MergeDoorCenterX, 0f, OfficeEndZ + StairDoorwayDepth);

    // 모듈 B 원점 z, 모듈 뒷벽 바깥면이 홀·사무실 경계벽 홀 쪽 면에 닿는 위치
    private const float StairModuleBOriginZ = HallEndZ - WallThickness * 0.5f - WallThickness - StairLandingEndZ;

    // 모듈 B 원점, 아래 끝 바닥이 홀 바닥 높이이고 오른쪽 바깥벽이 홀 왼쪽 벽 바깥면에 붙는 위치
    private static readonly Vector3 StairModuleBOrigin = new(-HallHalfWidth - StairDoorwayDepth - StairLowerLaneMaxX, -StairBottomY, StairModuleBOriginZ);

    // 외부 조명이 비상계단 모듈 범위에서 떨어져야 하는 여유 거리(m)
    private const float StairLightClearance = 0.1f;

    // 홀 왼쪽 벽 비상계단 문 중심 z
    private const float StairDoorCenterZ = StairModuleBOriginZ + StairBottomDoorCenterZ;

    // 계단참 층 표지판, 계단참 끝벽
    private const string FloorSignText = "8F";
    private const float FloorSignCenterHeight = 1.8f;
    private static readonly Vector3 FloorSignPanelSize = new(0.6f, 0.4f, 0.05f);
    private static readonly Vector2 FloorSignTextSize = new(0.5f, 0.35f);
    private const float FloorSignFontSizeMin = 0.5f;
    private const float FloorSignFontSizeMax = 3f;

    // 계단참 순간 이동 트리거 높이(m)
    private const float WarpTriggerHeight = 2f;

    // 책상 축소 목표 크기(m), 길이는 복도 방향, 깊이는 벽에서 통로 방향
    private const float DeskTargetLength = 1.4f;
    private const float DeskTargetDepth = 0.8f;

    // 책상 앞면과 의자 사이 간격(m)
    private const float ChairDeskGap = 0f;

    // 노트북을 책상 중심에서 벽 쪽으로 치우치는 비율(책상 깊이 절반 기준)
    private const float LaptopWallBias = 0.3f;

    // 책상 칸 배치, 양옆 줄 3칸씩
    private const float LeftDeskYRotation = 90f;
    private const float RightDeskYRotation = -90f;
    private static readonly float[] DeskSlotZ = { 4f, 10f, 16f };

    // 인물 자리 표시 캡슐 치수(m)
    private const float SeatMarkerHeight = 1.3f;
    private const float SeatMarkerDiameter = 0.5f;

    // 천장 형광등, 사무실 가운데 통로 위, 범위는 계단 모듈에 닿지 않게 줄임
    private const int CeilingLightCount = 6;
    private const float CeilingLightStartZ = 2.5f;
    private const float CeilingLightSpacing = 2.8f;
    private const float CeilingLightRange = 7f;
    private const float CeilingLightIntensity = 1.5f;

    // 홀·엘리베이터·계단 보조 조명, 홀·엘리베이터 범위는 계단 모듈에 닿지 않게 줄임
    private static readonly Vector3 HallLightPosition = new(1f, 2.8f, -3f);
    private const float HallLightRange = 8f;
    private const float ElevatorLightHeight = 2.8f;
    private const float ElevatorLightRange = 4f;
    private const float StairLightHeight = 2f;
    private const float StairLightRange = 7f;
    private const float AuxLightIntensity = 1.2f;

    // 포스터, 사무실 오른쪽 벽
    private const float PosterStartZ = 6f;
    private const float PosterSpacing = 1f;
    private const float PosterCenterY = 1.7f;

    // 칸반 보드와 화이트보드, 사무실 왼쪽 벽
    private const float KanbanBoardZ = 7f;
    private const float WhiteboardZ = 13f;
    private const float BoardCenterY = 1.5f;

    // 벽시계, 홀 오른쪽 벽
    private const float ClockZ = -5.2f;
    private const float ClockCenterY = 2.2f;
    private const float ClockDiameter = 0.4f;
    private const float ClockThickness = 0.03f;
    private const float ClockHandWidth = 0.02f;
    private const float ClockHandThickness = 0.01f;
    private const float MinuteHandLength = 0.16f;
    private const float HourHandLength = 0.1f;

    // 밤 10시(22:00)를 가리키는 12시 기준 시계 방향 각도
    private const float MinuteHandAngle = 0f;
    private const float HourHandAngle = 300f;

    // 소화기, 사무실 오른쪽 벽 끝 앞
    private const float ExtinguisherZ = 19f;

    // 비상구 표지판, Merge 문 위
    private const float ExitSignCenterY = 2.6f;
    private static readonly Vector3 ExitSignPanelSize = new(0.8f, 0.3f, 0.05f);
    private static readonly Vector2 ExitSignTextSize = new(0.7f, 0.25f);
    private const float ExitSignFontSizeMin = 0.5f;
    private const float ExitSignFontSizeMax = 3f;

    // 조명
    private const float DirectionalLightIntensity = 0.15f;
    private static readonly Vector3 DirectionalLightEuler = new(60f, 30f, 0f);
    private static readonly Color AmbientColor = new(0.12f, 0.12f, 0.14f);

    // 플레이어, 홀 안에서 사무실 쪽(+z)을 보는 시작 위치
    private static readonly Vector3 StartPosition = new(0f, 0f, -4f);
    private const float PlayerHeight = 1.8f;
    private const float PlayerRadius = 0.3f;
    private const float PlayerDiameter = PlayerRadius * 2f;
    private const float CameraHeight = 1.6f;
    private const float CameraNearClip = 0.05f;

    // 빌드 모니터, 판 뒷면이 홀 오른쪽 벽 안쪽 면에 닿는 위치
    private static readonly Vector3 BuildMonitorPanelSize = new(2.4f, 1.2f, 0.05f);
    private static readonly Vector3 BuildMonitorPosition = new(HallHalfWidth - BuildMonitorPanelSize.z * 0.5f, 1.6f, -3f);
    private const float BuildMonitorYRotation = 90f;
    private static readonly Vector2 BuildMonitorTextSize = new(2.2f, 1.0f);
    private const float BuildMonitorFontSizeMin = 0.5f;
    private const float BuildMonitorFontSizeMax = 1.5f;

    // 진행도 표시, 엘리베이터 개구부 위
    private static readonly Vector3 ProgressPosition = new(0f, 2.6f, -5.95f);
    private const float ProgressYRotation = 180f;
    private static readonly Vector2 ProgressTextSize = new(2f, 0.4f);
    private const float ProgressFontSizeMin = 1f;
    private const float ProgressFontSizeMax = 4f;

    // 릴리즈 제출 트리거, 비상계단 문을 지나 홀로 들어온 바로 안쪽
    private const float ReleaseTriggerInset = 0.6f;
    private const float ReleaseTriggerDepth = 0.6f;
    private static readonly Vector3 ReleaseTriggerPosition = new(-HallHalfWidth + ReleaseTriggerInset, RoomHeight * 0.5f, StairDoorCenterZ);
    private static readonly Vector3 ReleaseTriggerSize = new(ReleaseTriggerDepth, RoomHeight, StairDoorWidth);

    // 텍스트를 판 앞으로 띄우는 거리(m)
    private const float TextFrontOffset = 0.03f;

    [MenuItem("Tools/8번 코딩/사무실 씬 생성")]
    private static void CreateOfficeScene()
    {
        bool hasScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
        if (hasScene && !EditorUtility.DisplayDialog("사무실 씬 생성", $"{ScenePath}이(가) 이미 있습니다. 덮어쓸까요?", "덮어쓰기", "취소")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(SceneFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(DataFolder);

        GameObject deskUnitPrefab = CreateDeskUnitPrefab(out Bounds deskBounds, out Bounds chairBounds);
        // 책상 칸 기준 통로 쪽(의자 쪽) 끝 z
        float unitAisleEdge = Mathf.Max(deskBounds.max.z, chairBounds.max.z);
        OfficeProps officePropsPrefab = CreateOfficePropsPrefab(unitAisleEdge, chairBounds.center);
        BaseOfficeData baseOfficeData = CreateBaseOfficeData(deskUnitPrefab, unitAisleEdge, officePropsPrefab);

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Transform staticRoot = new GameObject("Office_Static").transform;
        CreateStaticGeometry(staticRoot, out Transform landingAnchorA, out Transform landingAnchorB);
        Transform interactables = CreateGroup("Interactables", staticRoot);
        Transform displays = CreateGroup("Displays", staticRoot);

        CreateLighting();
        FirstPersonController player = CreatePlayer();
        Transform startPoint = CreateStartPoint();
        BuildMonitorView buildMonitorView = CreateBuildMonitor(displays);
        ProgressIndicatorView progressIndicatorView = CreateProgressIndicator(displays);
        ElevatorButton elevatorButton = CreateElevatorButton(interactables);
        Elevator elevator = CreateElevator(interactables, elevatorButton);
        ProximityDoor mergeDoor = CreateMergeDoor(interactables);
        CreateStairDoor(interactables);
        ReleaseDoorTrigger releaseDoorTrigger = CreateReleaseDoorTrigger(interactables);
        CreateStairLandingWarp(interactables, player, landingAnchorA, landingAnchorB);
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
        loopManagerSerialized.FindProperty("_elevator").objectReferenceValue = elevator;
        loopManagerSerialized.FindProperty("_releaseDoorTrigger").objectReferenceValue = releaseDoorTrigger;
        loopManagerSerialized.FindProperty("_mergeDoor").objectReferenceValue = mergeDoor;
        loopManagerSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        LogResult(deskBounds, chairBounds, unitAisleEdge);
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

    // 완료 로그와 측정 크기 출력, 책상 칸이 벽을 넘으면 경고
    private static void LogResult(Bounds deskBounds, Bounds chairBounds, float unitAisleEdge)
    {
        // 의자 쪽 끝에서 책상 뒷면까지의 거리
        float unitDepth = unitAisleEdge - deskBounds.min.z;
        float backGap = OfficeHalfWidth - AisleHalfWidth - unitDepth;

        Debug.Log($"사무실 그레이박스 씬 생성 완료 : {ScenePath}\n책상 크기 {deskBounds.size}, 의자 크기 {chairBounds.size}, 책상 칸 전체 깊이 {unitDepth:F2}m, 가운데 통로 폭 {AisleHalfWidth * 2f:F2}m, 책상 뒤 벽까지 여유 {backGap:F2}m");

        if (backGap < 0f)
            Debug.LogWarning($"책상 칸이 사무실 벽을 {-backGap:F2}m 넘습니다.");
    }

    // 형광등·포스터·보드·시계·소화기·표지판·인물 자리 소품 묶음 프리팹 저장
    private static OfficeProps CreateOfficePropsPrefab(float unitAisleEdge, Vector3 chairCenter)
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

        CreateSeatMarkers(parent, unitAisleEdge, chairCenter);

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
        lampLight.range = ClampRangeOutsideStairs(lampLight.transform.position, CeilingLightRange);
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

    // 얇은 축을 벽 법선으로 돌려 뒷면을 사무실 옆벽 안쪽 면에 맞춘 배치
    private static Transform PlaceOnWall(string fileName, Transform parent, float yRotation, Vector3 wallSide, float centerY, float z)
    {
        GameObject model = InstantiateModel(fileName, parent, yRotation);
        if (model == null) return null;

        OrientLongAxis(model, true);
        AlignBounds(model, wallSide, new Vector3(wallSide.x * OfficeHalfWidth, centerY, z));
        return model.transform;
    }

    // 원판과 바늘 두 개로 만든 홀 오른쪽 벽 임시 벽시계 생성
    private static Transform CreateClock(Transform parent)
    {
        var clock = new GameObject("Clock");
        clock.transform.SetParent(parent, false);
        clock.transform.localPosition = new Vector3(HallHalfWidth - ClockThickness * 0.5f, ClockCenterY, ClockZ);

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

    // 판과 EXIT 글자로 만든 Merge 문 위 임시 비상구 표지판 생성
    private static Transform CreateExitSign(Transform parent)
    {
        var sign = new GameObject("ExitSign");
        sign.transform.SetParent(parent, false);
        sign.transform.localPosition = new Vector3(MergeDoorCenterX, ExitSignCenterY, OfficeEndZ - ExitSignPanelSize.z * 0.5f);

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

        AlignBounds(extinguisher, Vector3.down + Vector3.right, new Vector3(OfficeHalfWidth, 0f, ExtinguisherZ));
        return extinguisher.transform;
    }

    // 시니어(탕비실 쪽 오른쪽 줄 마지막)·동료(홀 쪽 왼쪽 줄 첫) 자리의 인물 표시 생성
    private static void CreateSeatMarkers(Transform parent, float unitAisleEdge, Vector3 chairCenter)
    {
        Vector3 seniorSlot = GetDeskSlotPosition(PositiveDirection, DeskSlotZ[DeskSlotZ.Length - 1], unitAisleEdge);
        CreateSeatMarker("SeniorSeat", parent, seniorSlot, RightDeskYRotation, chairCenter);

        Vector3 colleagueSlot = GetDeskSlotPosition(NegativeDirection, DeskSlotZ[0], unitAisleEdge);
        CreateSeatMarker("ColleagueSeat", parent, colleagueSlot, LeftDeskYRotation, chairCenter);
    }

    // 책상 칸 의자 위치에 콜라이더 없는 캡슐 자리 표시 생성
    private static void CreateSeatMarker(string name, Transform parent, Vector3 slotPosition, float yRotation, Vector3 chairCenter)
    {
        Vector3 seat = slotPosition + Quaternion.Euler(0f, yRotation, 0f) * chairCenter;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        marker.name = name;
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = new Vector3(seat.x, SeatMarkerHeight * 0.5f, seat.z);
        // 기본 캡슐 높이 2·지름 1 기준 스케일
        marker.transform.localScale = new Vector3(SeatMarkerDiameter, SeatMarkerHeight * 0.5f, SeatMarkerDiameter);
        marker.GetComponent<Renderer>().sharedMaterial = LoadMaterial(DarkMaterialPath);
        Object.DestroyImmediate(marker.GetComponent<Collider>());
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
    private static BaseOfficeData CreateBaseOfficeData(GameObject deskUnitPrefab, float unitAisleEdge, OfficeProps officePropsPrefab)
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

        SerializedProperty slots = serialized.FindProperty("_deskSlots");
        slots.arraySize = DeskSlotZ.Length * 2;
        for (int i = 0; i < DeskSlotZ.Length; i++)
        {
            SetDeskSlot(slots.GetArrayElementAtIndex(i * 2), GetDeskSlotPosition(NegativeDirection, DeskSlotZ[i], unitAisleEdge), LeftDeskYRotation);
            SetDeskSlot(slots.GetArrayElementAtIndex(i * 2 + 1), GetDeskSlotPosition(PositiveDirection, DeskSlotZ[i], unitAisleEdge), RightDeskYRotation);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    // 책상 칸의 의자 쪽 끝이 가운데 통로 가장자리에 닿는 책상 칸 중심 위치
    private static Vector3 GetDeskSlotPosition(float side, float z, float unitAisleEdge) => new(side * (AisleHalfWidth + unitAisleEdge), 0f, z);

    private static void SetDeskSlot(SerializedProperty slot, Vector3 position, float yRotation)
    {
        slot.FindPropertyRelative("_position").vector3Value = position;
        slot.FindPropertyRelative("_yRotation").floatValue = yRotation;
    }

    // 바뀌지 않는 건물 뼈대 생성 후 정적 표시, 두 계단 모듈의 계단참 기준점 반환
    private static void CreateStaticGeometry(Transform staticRoot, out Transform landingAnchorA, out Transform landingAnchorB)
    {
        MarkStatic(CreateHall(staticRoot));
        MarkStatic(CreateOffice(staticRoot));
        MarkStatic(CreatePantry(staticRoot));
        MarkStatic(CreateElevatorCar(staticRoot));

        Transform stairA = CreateStairModule("StairModuleA", staticRoot, StairModuleAOrigin, out landingAnchorA);
        Transform stairB = CreateStairModule("StairModuleB", staticRoot, StairModuleBOrigin, out landingAnchorB);
        CreateDeadEndDoors(stairA, stairB);
        MarkStatic(stairA);
        MarkStatic(stairB);
    }

    // 홀 바닥·천장·벽과 엘리베이터·비상계단 문 개구부, 보조 조명 생성
    private static Transform CreateHall(Transform parent)
    {
        Transform hall = CreateGroup("Hall", parent);
        Material wallMaterial = LoadMaterial(WallMaterialPath);

        float outerX = HallHalfWidth + WallThickness;
        float backOuterZ = HallStartZ - WallThickness;
        float elevatorHalfWidth = ElevatorDoorWidth * 0.5f;
        float doorMinZ = StairDoorCenterZ - StairDoorWidth * 0.5f;
        float doorMaxZ = StairDoorCenterZ + StairDoorWidth * 0.5f;

        CreateBox("Floor", hall, new Vector3(-outerX, -WallThickness, backOuterZ), new Vector3(outerX, 0f, HallEndZ), LoadMaterial(FloorMaterialPath), true);
        CreateBox("Ceiling", hall, new Vector3(-outerX, RoomHeight, backOuterZ), new Vector3(outerX, RoomHeight + WallThickness, HallEndZ), LoadMaterial(CeilingMaterialPath), true);

        // 엘리베이터 개구부가 뚫린 뒷벽
        CreateBox("BackWall_Left", hall, new Vector3(-outerX, 0f, backOuterZ), new Vector3(-elevatorHalfWidth, RoomHeight, HallStartZ), wallMaterial, true);
        CreateBox("BackWall_Right", hall, new Vector3(elevatorHalfWidth, 0f, backOuterZ), new Vector3(outerX, RoomHeight, HallStartZ), wallMaterial, true);
        CreateBox("BackWall_Lintel", hall, new Vector3(-elevatorHalfWidth, DoorHeight, backOuterZ), new Vector3(elevatorHalfWidth, RoomHeight, HallStartZ), wallMaterial, true);

        // 비상계단 문 개구부가 뚫린 왼쪽 벽
        CreateBox("LeftWall_Back", hall, new Vector3(-outerX, 0f, HallStartZ), new Vector3(-HallHalfWidth, RoomHeight, doorMinZ), wallMaterial, true);
        CreateBox("LeftWall_Front", hall, new Vector3(-outerX, 0f, doorMaxZ), new Vector3(-HallHalfWidth, RoomHeight, HallEndZ), wallMaterial, true);
        CreateBox("LeftWall_Lintel", hall, new Vector3(-outerX, DoorHeight, doorMinZ), new Vector3(-HallHalfWidth, RoomHeight, doorMaxZ), wallMaterial, true);

        CreateBox("RightWall", hall, new Vector3(HallHalfWidth, 0f, HallStartZ), new Vector3(outerX, RoomHeight, HallEndZ), wallMaterial, true);
        CreatePointLight("HallLight", hall, HallLightPosition, ClampRangeOutsideStairs(HallLightPosition, HallLightRange));
        return hall;
    }

    // 사무실 바닥·천장·옆벽, 게이트가 뚫린 경계벽, Merge 문·탕비실 개구부가 뚫린 끝벽 생성
    private static Transform CreateOffice(Transform parent)
    {
        Transform office = CreateGroup("Office", parent);
        Material wallMaterial = LoadMaterial(WallMaterialPath);

        float outerX = OfficeHalfWidth + WallThickness;
        float endOuterZ = OfficeEndZ + WallThickness;
        float frontMinZ = OfficeStartZ - WallThickness * 0.5f;
        float frontMaxZ = OfficeStartZ + WallThickness * 0.5f;
        float gateHalfWidth = GateWidth * 0.5f;
        float mergeMinX = MergeDoorCenterX - MergeDoorWidth * 0.5f;
        float mergeMaxX = MergeDoorCenterX + MergeDoorWidth * 0.5f;
        float pantryMinX = PantryCenterX - PantryOpeningWidth * 0.5f;
        float pantryMaxX = PantryCenterX + PantryOpeningWidth * 0.5f;

        CreateBox("Floor", office, new Vector3(-outerX, -WallThickness, OfficeStartZ), new Vector3(outerX, 0f, endOuterZ), LoadMaterial(FloorMaterialPath), true);
        CreateBox("Ceiling", office, new Vector3(-outerX, RoomHeight, OfficeStartZ), new Vector3(outerX, RoomHeight + WallThickness, endOuterZ), LoadMaterial(CeilingMaterialPath), true);
        CreateBox("LeftWall", office, new Vector3(-outerX, 0f, OfficeStartZ), new Vector3(-OfficeHalfWidth, RoomHeight, OfficeEndZ), wallMaterial, true);
        CreateBox("RightWall", office, new Vector3(OfficeHalfWidth, 0f, OfficeStartZ), new Vector3(outerX, RoomHeight, OfficeEndZ), wallMaterial, true);

        // 홀과의 경계벽, 가운데 게이트 개구부
        CreateBox("FrontWall_Left", office, new Vector3(-outerX, 0f, frontMinZ), new Vector3(-gateHalfWidth, RoomHeight, frontMaxZ), wallMaterial, true);
        CreateBox("FrontWall_Right", office, new Vector3(gateHalfWidth, 0f, frontMinZ), new Vector3(outerX, RoomHeight, frontMaxZ), wallMaterial, true);
        CreateBox("FrontWall_Lintel", office, new Vector3(-gateHalfWidth, DoorHeight, frontMinZ), new Vector3(gateHalfWidth, RoomHeight, frontMaxZ), wallMaterial, true);

        // 통로 끝벽, -x쪽 Merge 문과 +x쪽 탕비실 개구부
        CreateBox("EndWall_Left", office, new Vector3(-outerX, 0f, OfficeEndZ), new Vector3(mergeMinX, RoomHeight, endOuterZ), wallMaterial, true);
        CreateBox("EndWall_Middle", office, new Vector3(mergeMaxX, 0f, OfficeEndZ), new Vector3(pantryMinX, RoomHeight, endOuterZ), wallMaterial, true);
        CreateBox("EndWall_Right", office, new Vector3(pantryMaxX, 0f, OfficeEndZ), new Vector3(outerX, RoomHeight, endOuterZ), wallMaterial, true);
        CreateBox("EndWall_MergeLintel", office, new Vector3(mergeMinX, DoorHeight, OfficeEndZ), new Vector3(mergeMaxX, RoomHeight, endOuterZ), wallMaterial, true);
        CreateBox("EndWall_PantryLintel", office, new Vector3(pantryMinX, DoorHeight, OfficeEndZ), new Vector3(pantryMaxX, RoomHeight, endOuterZ), wallMaterial, true);
        return office;
    }

    // 끝벽 너머 짧은 막힌 탕비실 공간과 입구를 막는 보이지 않는 벽 생성
    private static Transform CreatePantry(Transform parent)
    {
        Transform pantry = CreateGroup("Pantry", parent);
        Material wallMaterial = LoadMaterial(WallMaterialPath);

        float minX = PantryCenterX - PantryOpeningWidth * 0.5f;
        float maxX = PantryCenterX + PantryOpeningWidth * 0.5f;
        float startZ = OfficeEndZ + WallThickness;
        float backZ = startZ + PantryDepth;

        CreateBox("Floor", pantry, new Vector3(minX - WallThickness, -WallThickness, startZ), new Vector3(maxX + WallThickness, 0f, backZ + WallThickness), LoadMaterial(FloorMaterialPath), true);
        CreateBox("Ceiling", pantry, new Vector3(minX - WallThickness, RoomHeight, startZ), new Vector3(maxX + WallThickness, RoomHeight + WallThickness, backZ + WallThickness), LoadMaterial(CeilingMaterialPath), true);
        CreateBox("LeftWall", pantry, new Vector3(minX - WallThickness, 0f, startZ), new Vector3(minX, RoomHeight, backZ), wallMaterial, true);
        CreateBox("RightWall", pantry, new Vector3(maxX, 0f, startZ), new Vector3(maxX + WallThickness, RoomHeight, backZ), wallMaterial, true);
        CreateBox("BackWall", pantry, new Vector3(minX - WallThickness, 0f, backZ), new Vector3(maxX + WallThickness, RoomHeight, backZ + WallThickness), wallMaterial, true);
        CreateInvisibleCollider("EntranceBlocker", pantry, new Vector3(minX, 0f, OfficeEndZ), new Vector3(maxX, DoorHeight, startZ));
        return pantry;
    }

    // 홀 뒷벽 뒤로 움푹 들어간 엘리베이터 칸 생성
    private static Transform CreateElevatorCar(Transform parent)
    {
        Transform car = CreateGroup("ElevatorCar", parent);
        Material wallMaterial = LoadMaterial(WallMaterialPath);

        float halfWidth = ElevatorCarWidth * 0.5f;
        float outerX = halfWidth + WallThickness;
        float frontZ = HallStartZ - WallThickness;
        float backZ = frontZ - ElevatorCarDepth;

        CreateBox("Floor", car, new Vector3(-outerX, -WallThickness, backZ - WallThickness), new Vector3(outerX, 0f, frontZ), LoadMaterial(FloorMaterialPath), true);
        CreateBox("Ceiling", car, new Vector3(-outerX, RoomHeight, backZ - WallThickness), new Vector3(outerX, RoomHeight + WallThickness, frontZ), LoadMaterial(CeilingMaterialPath), true);
        CreateBox("LeftWall", car, new Vector3(-outerX, 0f, backZ), new Vector3(-halfWidth, RoomHeight, frontZ), wallMaterial, true);
        CreateBox("RightWall", car, new Vector3(halfWidth, 0f, backZ), new Vector3(outerX, RoomHeight, frontZ), wallMaterial, true);
        CreateBox("BackWall", car, new Vector3(-outerX, 0f, backZ - WallThickness), new Vector3(outerX, RoomHeight, backZ), wallMaterial, true);

        var lightPosition = new Vector3(0f, ElevatorLightHeight, ElevatorCarCenterZ);
        CreatePointLight("ElevatorLight", car, lightPosition, ClampRangeOutsideStairs(lightPosition, ElevatorLightRange));
        return car;
    }

    // 위 계단·U턴 계단참·아래 계단과 둘러싼 벽으로 된 비상계단 모듈 생성, 계단참 기준점 반환
    private static Transform CreateStairModule(string name, Transform parent, Vector3 origin, out Transform landingAnchor)
    {
        Transform module = CreateGroup(name, parent);
        module.localPosition = origin;

        Material floorMaterial = LoadMaterial(FloorMaterialPath);
        Material wallMaterial = LoadMaterial(WallMaterialPath);

        float upperMinX = -StairUpperLaneHalfWidth;
        float outerMinX = upperMinX - WallThickness;
        float outerMaxX = StairLowerLaneMaxX + WallThickness;
        float frontOuterZ = -WallThickness;
        float backOuterZ = StairLandingEndZ + WallThickness;
        float doorMinZ = StairBottomDoorCenterZ - StairDoorWidth * 0.5f;
        float doorMaxZ = StairBottomDoorCenterZ + StairDoorWidth * 0.5f;

        CreateBox("BaseFloor", module, new Vector3(outerMinX, StairBottomY - WallThickness, frontOuterZ), new Vector3(outerMaxX, StairBottomY, backOuterZ), floorMaterial, true);
        CreateBox("Ceiling", module, new Vector3(outerMinX, StairCeilingHeight, frontOuterZ), new Vector3(outerMaxX, StairCeilingHeight + WallThickness, backOuterZ), LoadMaterial(CeilingMaterialPath), true);
        CreateBox("TopFloor", module, new Vector3(upperMinX, -WallThickness, 0f), new Vector3(StairUpperLaneHalfWidth, 0f, StairEndAreaDepth), floorMaterial, true);
        CreateBox("LandingFloor", module, new Vector3(upperMinX, StairLandingY - WallThickness, StairLandingStartZ), new Vector3(StairLowerLaneMaxX, StairLandingY, StairLandingEndZ), floorMaterial, true);

        CreateBox("LeftWall", module, new Vector3(outerMinX, StairBottomY, frontOuterZ), new Vector3(upperMinX, StairCeilingHeight, backOuterZ), wallMaterial, true);

        // 아래 계단 끝 평지 문 개구부가 뚫린 오른쪽 벽
        CreateBox("RightWall_Front", module, new Vector3(StairLowerLaneMaxX, StairBottomY, frontOuterZ), new Vector3(outerMaxX, StairCeilingHeight, doorMinZ), wallMaterial, true);
        CreateBox("RightWall_Back", module, new Vector3(StairLowerLaneMaxX, StairBottomY, doorMaxZ), new Vector3(outerMaxX, StairCeilingHeight, backOuterZ), wallMaterial, true);
        CreateBox("RightWall_Lintel", module, new Vector3(StairLowerLaneMaxX, StairBottomY + DoorHeight, doorMinZ), new Vector3(outerMaxX, StairCeilingHeight, doorMaxZ), wallMaterial, true);

        // 위 계단 꼭대기 문 개구부가 뚫린 앞벽
        CreateBox("FrontWall_Below", module, new Vector3(upperMinX, StairBottomY, frontOuterZ), new Vector3(StairUpperLaneHalfWidth, 0f, 0f), wallMaterial, true);
        CreateBox("FrontWall_Lintel", module, new Vector3(upperMinX, DoorHeight, frontOuterZ), new Vector3(StairUpperLaneHalfWidth, StairCeilingHeight, 0f), wallMaterial, true);
        CreateBox("FrontWall_Side", module, new Vector3(StairUpperLaneHalfWidth, StairBottomY, frontOuterZ), new Vector3(StairLowerLaneMaxX, StairCeilingHeight, 0f), wallMaterial, true);

        CreateBox("BackWall", module, new Vector3(upperMinX, StairBottomY, StairLandingEndZ), new Vector3(StairLowerLaneMaxX, StairCeilingHeight, backOuterZ), wallMaterial, true);

        // 두 계단 줄 사이 칸막이, 계단참에서 끊겨 U턴 공간 확보
        CreateBox("Divider", module, new Vector3(StairUpperLaneHalfWidth, StairBottomY, 0f), new Vector3(StairLowerLaneMinX, StairCeilingHeight, StairLandingStartZ), wallMaterial, true);

        CreateStairFlight("Upper", module, 0f, 0f, StairEndAreaDepth, PositiveDirection, StairBottomY);
        CreateStairFlight("Lower", module, StairLowerLaneCenterX, StairLandingY, StairLandingStartZ, NegativeDirection, StairBottomY);

        float landingCenterZ = (StairLandingStartZ + StairLandingEndZ) * 0.5f;
        CreateFloorSign(module, new Vector3(StairDividerCenterX, StairLandingY + FloorSignCenterHeight, StairLandingEndZ - FloorSignPanelSize.z * 0.5f));
        CreatePointLight("StairLight", module, new Vector3(StairDividerCenterX, StairLandingY + StairLightHeight, landingCenterZ), StairLightRange);

        var anchor = new GameObject("LandingAnchor");
        anchor.transform.SetParent(module, false);
        anchor.transform.localPosition = new Vector3(0f, StairLandingY, StairLandingStartZ);
        landingAnchor = anchor.transform;
        return module;
    }

    // 칸마다 보이는 계단 박스와 계단 코를 잇는 보이지 않는 경사 콜라이더로 된 계단 한 줄 생성
    private static void CreateStairFlight(string name, Transform parent, float centerX, float topY, float startZ, float zDirection, float baseY)
    {
        float minX = centerX - StairWidth * 0.5f;
        float maxX = centerX + StairWidth * 0.5f;
        Material material = LoadMaterial(FloorMaterialPath);

        for (int i = 0; i < StairStepCount; i++)
        {
            float nearZ = startZ + zDirection * StairStepDepth * i;
            float farZ = nearZ + zDirection * StairStepDepth;
            float stepTopY = topY - StairStepHeight * (i + 1);
            CreateBox($"{name}Step_{i + 1}", parent, new Vector3(minX, baseY, Mathf.Min(nearZ, farZ)), new Vector3(maxX, stepTopY, Mathf.Max(nearZ, farZ)), material, false);
        }

        // 윗바닥 끝에서 아랫바닥 시작까지, 각 칸의 아래쪽 끝 모서리(계단 코)를 지나는 선
        var slopeStart = new Vector3(centerX, topY, startZ);
        var slopeEnd = new Vector3(centerX, topY - StairFlightRise, startZ + zDirection * StairStepDepth * StairStepCount);
        CreateSlopeCollider($"{name}Slope", parent, slopeStart, slopeEnd);
    }

    // 두 점을 잇는 선이 윗면이 되는 렌더러 없는 경사 콜라이더 생성
    private static void CreateSlopeCollider(string name, Transform parent, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        Quaternion rotation = Quaternion.LookRotation(direction);
        // 윗면이 선과 겹치도록 두께 절반만큼 경사면 아래로 이동
        Vector3 center = (start + end) * 0.5f - rotation * Vector3.up * (StairSlopeThickness * 0.5f);

        var slope = new GameObject(name);
        slope.transform.SetParent(parent, false);
        slope.transform.SetLocalPositionAndRotation(center, rotation);
        BoxCollider slopeCollider = slope.AddComponent<BoxCollider>();
        slopeCollider.size = new Vector3(StairWidth, StairSlopeThickness, direction.magnitude);
    }

    // 모듈 A 아래 끝과 모듈 B 꼭대기 개구부에 반대편 진짜 문과 같은 깊이의 막다른 문과 문틀 생성
    private static void CreateDeadEndDoors(Transform stairA, Transform stairB)
    {
        Material wallMaterial = LoadMaterial(WallMaterialPath);
        Material floorMaterial = LoadMaterial(FloorMaterialPath);
        float halfLeaf = DoorLeafThickness * 0.5f;

        // 모듈 A 아래 끝, 홀 벽 두께 가운데에 문짝이 있는 모듈 B 비상계단 문과 같은 모양
        float innerX = StairLowerLaneMaxX;
        float frameMinX = innerX + WallThickness;
        float frameMaxX = innerX + StairDoorwayDepth;
        float leafX = innerX + StairDoorLeafDepth;
        float doorMinZ = StairBottomDoorCenterZ - StairDoorWidth * 0.5f;
        float doorMaxZ = StairBottomDoorCenterZ + StairDoorWidth * 0.5f;
        float doorTopY = StairBottomY + DoorHeight;

        CreateBox("DeadEndDoor", stairA, new Vector3(leafX - halfLeaf, StairBottomY, doorMinZ), new Vector3(leafX + halfLeaf, doorTopY, doorMaxZ), LoadMaterial(DarkMaterialPath), true);
        CreateBox("DeadEndBacking", stairA, new Vector3(leafX + halfLeaf, StairBottomY, doorMinZ), new Vector3(frameMaxX, doorTopY, doorMaxZ), wallMaterial, true);
        CreateBox("DeadEndJamb_Front", stairA, new Vector3(frameMinX, StairBottomY, doorMinZ - WallThickness), new Vector3(frameMaxX, doorTopY, doorMinZ), wallMaterial, true);
        CreateBox("DeadEndJamb_Back", stairA, new Vector3(frameMinX, StairBottomY, doorMaxZ), new Vector3(frameMaxX, doorTopY, doorMaxZ + WallThickness), wallMaterial, true);
        CreateBox("DeadEndLintel", stairA, new Vector3(frameMinX, doorTopY, doorMinZ - WallThickness), new Vector3(frameMaxX, StairBottomY + RoomHeight, doorMaxZ + WallThickness), wallMaterial, true);
        // 모듈 B 문턱 아래 홀 바닥과 같은 높이·두께의 문턱 바닥
        CreateBox("DeadEndThreshold", stairA, new Vector3(frameMinX, StairBottomY - WallThickness, doorMinZ - WallThickness), new Vector3(frameMaxX, StairBottomY, doorMaxZ + WallThickness), floorMaterial, true);

        // 모듈 B 꼭대기, 사무실 끝벽 두께 가운데에 문짝이 있는 모듈 A Merge 문과 같은 모양
        float frameMinZ = -StairDoorwayDepth;
        float frameMaxZ = -WallThickness;
        float leafZ = -StairDoorLeafDepth;
        float laneMinX = -StairUpperLaneHalfWidth;
        float laneMaxX = StairUpperLaneHalfWidth;

        CreateBox("DeadEndDoor", stairB, new Vector3(laneMinX, 0f, leafZ - halfLeaf), new Vector3(laneMaxX, DoorHeight, leafZ + halfLeaf), LoadMaterial(SignMaterialPath), true);
        CreateBox("DeadEndBacking", stairB, new Vector3(laneMinX, 0f, frameMinZ), new Vector3(laneMaxX, DoorHeight, leafZ - halfLeaf), wallMaterial, true);
        CreateBox("DeadEndJamb_Left", stairB, new Vector3(laneMinX - WallThickness, 0f, frameMinZ), new Vector3(laneMinX, DoorHeight, frameMaxZ), wallMaterial, true);
        CreateBox("DeadEndJamb_Right", stairB, new Vector3(laneMaxX, 0f, frameMinZ), new Vector3(laneMaxX + WallThickness, DoorHeight, frameMaxZ), wallMaterial, true);
        CreateBox("DeadEndLintel", stairB, new Vector3(laneMinX - WallThickness, DoorHeight, frameMinZ), new Vector3(laneMaxX + WallThickness, RoomHeight, frameMaxZ), wallMaterial, true);
        // 모듈 A 문턱 아래 사무실 바닥과 같은 높이·두께의 문턱 바닥
        CreateBox("DeadEndThreshold", stairB, new Vector3(laneMinX - WallThickness, -WallThickness, frameMinZ), new Vector3(laneMaxX + WallThickness, 0f, frameMaxZ), floorMaterial, true);
    }

    // 외부 조명이 두 비상계단 모듈 범위에 닿지 않도록 줄인 범위
    private static float ClampRangeOutsideStairs(Vector3 position, float range)
    {
        // 광원이 두 모듈 범위 밖에 있다는 전제의 거리 계산(안에 있으면 범위가 음수)
        float sqrDistance = Mathf.Min(GetStairModuleBounds(StairModuleAOrigin).SqrDistance(position), GetStairModuleBounds(StairModuleBOrigin).SqrDistance(position));
        return Mathf.Min(range, Mathf.Sqrt(sqrDistance) - StairLightClearance);
    }

    // 바깥벽과 맞붙은 건물 벽 출입구 문틀까지 포함한 계단 모듈 월드 범위
    private static Bounds GetStairModuleBounds(Vector3 origin)
    {
        var min = new Vector3(-StairUpperLaneHalfWidth - WallThickness, StairBottomY - WallThickness, -StairDoorwayDepth);
        var max = new Vector3(StairLowerLaneMaxX + StairDoorwayDepth, StairCeilingHeight + WallThickness, StairLandingEndZ + WallThickness);
        var bounds = new Bounds();
        bounds.SetMinMax(origin + min, origin + max);
        return bounds;
    }

    // 판과 층 글자로 만든 계단참 층 표지판 생성
    private static void CreateFloorSign(Transform parent, Vector3 localPosition)
    {
        var sign = new GameObject("FloorSign");
        sign.transform.SetParent(parent, false);
        sign.transform.localPosition = localPosition;

        CreateCube("Panel", sign.transform, Vector3.zero, FloorSignPanelSize, LoadMaterial(SignMaterialPath), false);

        // 회전 없이 위 계단 쪽(-Z)에서 읽히는 방향
        TextMeshPro label = CreateText("Label", sign.transform, FloorSignTextSize, FloorSignFontSizeMin, FloorSignFontSizeMax, TextAlignmentOptions.Center);
        label.transform.localPosition = new Vector3(0f, 0f, -(FloorSignPanelSize.z * 0.5f + TextFrontOffset));
        label.text = FloorSignText;
        // 밝은 판 위 가독성 확보
        label.color = Color.black;
    }

    // 그림자 없는 보조 점광원 생성
    private static void CreatePointLight(string name, Transform parent, Vector3 localPosition, float range)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;

        Light pointLight = lightObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = range;
        pointLight.intensity = AuxLightIntensity;
        pointLight.shadows = LightShadows.None;
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

    // 홀 오른쪽 벽 빌드 모니터 생성
    private static BuildMonitorView CreateBuildMonitor(Transform parent)
    {
        var monitor = new GameObject("BuildMonitor");
        monitor.transform.SetParent(parent, false);
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

    // 엘리베이터 칸 안쪽 벽 버튼 시각물과 칸 뒷벽부터 문짝 앞 여유까지의 상호작용 범위 생성
    private static ElevatorButton CreateElevatorButton(Transform parent)
    {
        float halfWidth = ElevatorCarWidth * 0.5f;
        var buttonPosition = new Vector3(halfWidth - ElevatorButtonSize.x * 0.5f, ElevatorButtonHeight, ElevatorCarCenterZ);
        Material lightOffMaterial = LoadMaterial(LightOffMaterialPath);
        GameObject buttonVisual = CreateCube("ElevatorButtonVisual", parent, buttonPosition, ElevatorButtonSize, lightOffMaterial, false);

        float zoneDepth = ElevatorZoneFrontZ - ElevatorCarBackZ;
        var zone = new GameObject("ElevatorButtonZone");
        zone.transform.SetParent(parent, false);
        zone.transform.localPosition = new Vector3(0f, ElevatorZoneHeight * 0.5f, ElevatorCarBackZ + zoneDepth * 0.5f);
        BoxCollider zoneCollider = zone.AddComponent<BoxCollider>();
        zoneCollider.isTrigger = true;
        zoneCollider.size = new Vector3(ElevatorCarWidth, ElevatorZoneHeight, zoneDepth);

        ElevatorButton button = zone.AddComponent<ElevatorButton>();
        var serialized = new SerializedObject(button);
        serialized.FindProperty("_interactAction").objectReferenceValue = FindActionReference("Interact");
        serialized.FindProperty("_buttonRenderer").objectReferenceValue = buttonVisual.GetComponent<Renderer>();
        serialized.FindProperty("_activeMaterial").objectReferenceValue = LoadMaterial(LightOnMaterialPath);
        serialized.FindProperty("_inactiveMaterial").objectReferenceValue = lightOffMaterial;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    // 열린 상태로 배치한 미닫이 문 2짝과 엘리베이터 동작 생성
    private static Elevator CreateElevator(Transform parent, ElevatorButton button)
    {
        var elevatorObject = new GameObject("Elevator");
        elevatorObject.transform.SetParent(parent, false);
        elevatorObject.transform.localPosition = new Vector3(0f, 0f, ElevatorDoorZ);

        float leafWidth = ElevatorDoorWidth * 0.5f;
        var leafSize = new Vector3(leafWidth, DoorHeight, DoorLeafThickness);
        Material doorMaterial = LoadMaterial(DarkMaterialPath);

        // 닫힘 위치(개구부 절반 중심)에서 한 짝 폭만큼 바깥쪽인 열림 위치
        float closedCenterX = leafWidth * 0.5f;
        float openCenterX = closedCenterX + leafWidth;
        GameObject leftDoor = CreateCube("LeftDoor", elevatorObject.transform, new Vector3(-openCenterX, DoorHeight * 0.5f, 0f), leafSize, doorMaterial, true);
        GameObject rightDoor = CreateCube("RightDoor", elevatorObject.transform, new Vector3(openCenterX, DoorHeight * 0.5f, 0f), leafSize, doorMaterial, true);

        Elevator elevator = elevatorObject.AddComponent<Elevator>();
        var serialized = new SerializedObject(elevator);
        serialized.FindProperty("_button").objectReferenceValue = button;
        serialized.FindProperty("_leftDoor").objectReferenceValue = leftDoor.transform;
        serialized.FindProperty("_rightDoor").objectReferenceValue = rightDoor.transform;
        serialized.FindProperty("_slideDistance").floatValue = leafWidth;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return elevator;
    }

    // 사무실 끝벽 Merge 문 생성, 앞쪽(+z)인 비상계단으로 통과하면 잠김
    private static ProximityDoor CreateMergeDoor(Transform parent)
    {
        var position = new Vector3(MergeDoorCenterX, 0f, OfficeEndZ + WallThickness * 0.5f);
        var leafSize = new Vector3(MergeDoorWidth, DoorHeight, DoorLeafThickness);
        ProximityDoor door = CreateProximityDoor("MergeDoor", parent, position, leafSize, Vector3.left * MergeDoorWidth, LoadMaterial(SignMaterialPath), true);

        var serialized = new SerializedObject(door);
        serialized.FindProperty("_moveSpeed").floatValue = MergeDoorMoveSpeed;
        serialized.FindProperty("_passDistance").floatValue = MergeDoorPassDistance;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return door;
    }

    // 홀 왼쪽 벽 양방향 비상계단 문 생성
    private static void CreateStairDoor(Transform parent)
    {
        var position = new Vector3(-HallHalfWidth - WallThickness * 0.5f, 0f, StairDoorCenterZ);
        var leafSize = new Vector3(DoorLeafThickness, DoorHeight, StairDoorWidth);
        CreateProximityDoor("StairDoor", parent, position, leafSize, Vector3.forward * StairDoorWidth, LoadMaterial(DarkMaterialPath), false);
    }

    // 문짝과 열림 판정 트리거를 갖춘 근접 개폐 문 생성, 문 앞쪽은 +z
    private static ProximityDoor CreateProximityDoor(string name, Transform parent, Vector3 position, Vector3 leafSize, Vector3 openOffset, Material leafMaterial, bool isOneWay)
    {
        var doorObject = new GameObject(name);
        doorObject.transform.SetParent(parent, false);
        doorObject.transform.localPosition = position;

        var center = new Vector3(0f, DoorHeight * 0.5f, 0f);
        GameObject leaf = CreateCube("Leaf", doorObject.transform, center, leafSize, leafMaterial, true);

        BoxCollider trigger = doorObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = center;
        trigger.size = new Vector3(DoorTriggerSize, DoorHeight, DoorTriggerSize);

        ProximityDoor door = doorObject.AddComponent<ProximityDoor>();
        var serialized = new SerializedObject(door);
        serialized.FindProperty("_doorLeaf").objectReferenceValue = leaf.transform;
        serialized.FindProperty("_openOffset").vector3Value = openOffset;
        serialized.FindProperty("_isOneWay").boolValue = isOneWay;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return door;
    }

    // 엘리베이터 개구부 위 진행도 표시 생성
    private static ProgressIndicatorView CreateProgressIndicator(Transform parent)
    {
        var indicator = new GameObject("ProgressIndicator");
        indicator.transform.SetParent(parent, false);
        indicator.transform.SetPositionAndRotation(ProgressPosition, Quaternion.Euler(0f, ProgressYRotation, 0f));

        TextMeshPro label = CreateText("Label", indicator.transform, ProgressTextSize, ProgressFontSizeMin, ProgressFontSizeMax, TextAlignmentOptions.Center);

        ProgressIndicatorView view = indicator.AddComponent<ProgressIndicatorView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("_label").objectReferenceValue = label;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static ReleaseDoorTrigger CreateReleaseDoorTrigger(Transform parent)
    {
        var triggerObject = new GameObject("ReleaseDoorTrigger");
        triggerObject.transform.SetParent(parent, false);
        triggerObject.transform.localPosition = ReleaseTriggerPosition;
        BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = ReleaseTriggerSize;
        return triggerObject.AddComponent<ReleaseDoorTrigger>();
    }

    // 모듈 A 계단참의 아래 계단 줄 쪽 절반을 덮는 순간 이동 트리거 생성
    private static void CreateStairLandingWarp(Transform parent, FirstPersonController player, Transform sourceAnchor, Transform destinationAnchor)
    {
        var warpObject = new GameObject("StairLandingWarp");
        warpObject.transform.SetParent(parent, false);
        float landingCenterZ = (StairLandingStartZ + StairLandingEndZ) * 0.5f;
        warpObject.transform.localPosition = StairModuleAOrigin + new Vector3(StairLowerLaneCenterX, StairLandingY + WarpTriggerHeight * 0.5f, landingCenterZ);

        BoxCollider triggerCollider = warpObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(StairWidth, WarpTriggerHeight, StairLandingDepth);

        StairLandingWarp warp = warpObject.AddComponent<StairLandingWarp>();
        var serialized = new SerializedObject(warp);
        serialized.FindProperty("_player").objectReferenceValue = player;
        serialized.FindProperty("_sourceAnchor").objectReferenceValue = sourceAnchor;
        serialized.FindProperty("_destinationAnchor").objectReferenceValue = destinationAnchor;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // 매 루프 재생성 오브젝트의 루트이자 재생성 담당인 Office_Dynamic 생성
    private static OfficeBuilder CreateOfficeBuilder()
    {
        var dynamicRoot = new GameObject("Office_Dynamic");
        OfficeBuilder builder = dynamicRoot.AddComponent<OfficeBuilder>();
        var serialized = new SerializedObject(builder);
        serialized.FindProperty("_runtimeRoot").objectReferenceValue = dynamicRoot.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return builder;
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        var group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    // 하위 오브젝트 전체 정적 표시
    private static void MarkStatic(Transform root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
            child.gameObject.isStatic = true;
    }

    // 최소·최대 모서리로 범위를 지정한 큐브 생성
    private static void CreateBox(string name, Transform parent, Vector3 min, Vector3 max, Material material, bool hasCollider) => CreateCube(name, parent, (min + max) * 0.5f, max - min, material, hasCollider);

    // 최소·최대 모서리로 범위를 지정한 렌더러 없는 콜라이더 생성
    private static void CreateInvisibleCollider(string name, Transform parent, Vector3 min, Vector3 max)
    {
        var colliderObject = new GameObject(name);
        colliderObject.transform.SetParent(parent, false);
        colliderObject.transform.localPosition = (min + max) * 0.5f;
        BoxCollider box = colliderObject.AddComponent<BoxCollider>();
        box.size = max - min;
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
