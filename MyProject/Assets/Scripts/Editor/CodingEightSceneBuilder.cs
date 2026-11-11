using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 「8번 코딩」 씬, 머티리얼, 문제 데이터 생성(좌표 단위 m, 플레이어 시작 방향 +Z)
public static class CodingEightSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/CodingEight.unity";
    private const string MaterialFolder = "Assets/Materials/CodingEight";
    private const string DatabasePath = "Assets/Data/OfficeCaseDatabase.asset";
    private const string InputActionsPath = "Assets/Input/InputSystem_Actions.inputactions";
    private const string LitShaderName = "Universal Render Pipeline/Lit";
    private const string PlayerMapName = "Player";
    private const float WorldCanvasScale = 0.002f;
    private const float WorldTextPadding = 20f;

    private static readonly Vector3[] LampPositions =
    {
        new(-2f, 0f, 4f), new(2f, 0f, 4f), new(-2f, 0f, 8f), new(2f, 0f, 8f), new(-2f, 0f, 12f), new(2f, 0f, 12f),
    };

    // 0~2 왼쪽 줄 앞에서 뒤, 3~5 오른쪽 줄 앞에서 뒤
    private static readonly Vector3[] DeskPositions =
    {
        new(-3f, 0f, 5.5f), new(-3f, 0f, 8.5f), new(-3f, 0f, 11.5f), new(3f, 0f, 5.5f), new(3f, 0f, 8.5f), new(3f, 0f, 11.5f),
    };

    private static readonly Dictionary<string, Material> s_materials = new();
    private static Font s_font;

    [MenuItem("Tools/8번 코딩/씬 생성")]
    public static void BuildScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureFolder("Assets/Materials", "CodingEight");
        EnsureFolder("Assets", "Data");

        // 새 씬을 연 뒤에 에셋을 만들어야 씬 전환 때 참조가 끊기지 않음
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateMaterials();
        OfficeCaseDatabase database = CreateDatabase();
        SetupEnvironmentLighting();

        Transform office = new GameObject("Office").transform;
        BuildRoom(office);
        BuildExitCorridor(office, out PlayerZoneTrigger mergeZone);
        BuildElevator(office, out ElevatorDoor elevatorDoor, out PlayerZoneTrigger elevatorZone);
        BuildLamps(office, out List<Renderer> lampRenderers, out List<Light> lampLights);
        BuildDesks(office, out List<Transform> chairs, out List<Renderer> screens);
        BuildPantry(office, out List<GameObject> cups, out GameObject plant, out Transform poster);
        WorkerCharacter senior = BuildSenior(office);
        WorkerCharacter coworker = BuildCoworker(office);
        CodeMonitorView codeMonitor = BuildCodeScreen(office);
        WallClock clock = BuildClock(office);
        SubmitButton submitButton = BuildSubmitButton(office);
        BugReportTerminal terminal = BuildBugReportTerminal(office);
        Transform points = BuildPoints(out Transform playerStart, out Transform seniorStand, out List<Transform> leavePath, out Transform elevatorInside);

        ScreenOverlayView overlay = BuildOverlay();
        FirstPersonController player = BuildPlayer(playerStart, overlay, out Transform playerCamera);

        var officeController = office.gameObject.AddComponent<OfficeController>();
        SetFields(officeController,
            ("_lampOnMaterial", s_materials["LampOn"]), ("_lampOffMaterial", s_materials["LampOff"]), ("_lampRedMaterial", s_materials["LampRed"]),
            ("_screenOnMaterial", s_materials["ScreenOn"]), ("_screenOffMaterial", s_materials["ScreenOff"]), ("_missingTextureMaterial", s_materials["MissingTexture"]),
            ("_plant", plant), ("_poster", poster), ("_senior", senior), ("_coworker", coworker), ("_stareTarget", playerCamera));
        SetArray(officeController, "_lampRenderers", lampRenderers);
        SetArray(officeController, "_lampLights", lampLights);
        SetArray(officeController, "_chairs", chairs);
        SetArray(officeController, "_monitorScreens", screens);
        SetArray(officeController, "_cups", cups);

        var gameFlow = new GameObject("GameFlowManager").AddComponent<GameFlowManager>();
        SetFields(gameFlow,
            ("_caseDatabase", database), ("_office", officeController), ("_codeMonitor", codeMonitor), ("_clock", clock),
            ("_bugReportTerminal", terminal), ("_submitButton", submitButton), ("_mergeZone", mergeZone),
            ("_elevatorDoor", elevatorDoor), ("_elevatorZone", elevatorZone), ("_seniorStandPoint", seniorStand),
            ("_elevatorInsidePoint", elevatorInside), ("_player", player), ("_playerStartPoint", playerStart),
            ("_interactAction", FindActionReference("Interact")), ("_overlay", overlay));
        SetArray(gameFlow, "_leavePathPoints", leavePath);

        points.SetAsLastSibling();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        Debug.Log($"[8번 코딩] 씬 생성 완료 : {ScenePath}");
    }

    // 머티리얼

    private static void CreateMaterials()
    {
        CreateMaterial("Floor", new Color(0.30f, 0.31f, 0.33f));
        CreateMaterial("Wall", new Color(0.80f, 0.80f, 0.76f));
        CreateMaterial("Ceiling", new Color(0.88f, 0.88f, 0.87f));
        CreateMaterial("Desk", new Color(0.78f, 0.76f, 0.72f));
        CreateMaterial("DeskPanel", new Color(0.55f, 0.55f, 0.55f));
        CreateMaterial("Chair", new Color(0.13f, 0.13f, 0.15f));
        CreateMaterial("MonitorBody", new Color(0.08f, 0.08f, 0.09f));
        CreateMaterial("ScreenOn", new Color(0.12f, 0.2f, 0.32f), new Color(0.25f, 0.42f, 0.65f));
        CreateMaterial("ScreenOff", new Color(0.02f, 0.02f, 0.02f));
        CreateMaterial("MissingTexture", new Color(1f, 0f, 1f), new Color(1f, 0f, 1f));
        CreateMaterial("LampOn", Color.white, new Color(1f, 0.95f, 0.88f) * 1.5f);
        CreateMaterial("LampOff", new Color(0.45f, 0.45f, 0.45f));
        CreateMaterial("LampRed", new Color(1f, 0.2f, 0.15f), new Color(1f, 0.1f, 0.05f) * 1.5f);
        CreateMaterial("CodeScreen", new Color(0.03f, 0.03f, 0.05f));
        CreateMaterial("Cup", new Color(0.95f, 0.95f, 0.93f));
        CreateMaterial("PlantPot", new Color(0.45f, 0.3f, 0.2f));
        CreateMaterial("PlantLeaf", new Color(0.16f, 0.42f, 0.2f));
        CreateMaterial("Poster", new Color(0.95f, 0.83f, 0.3f));
        CreateMaterial("Skin", new Color(0.86f, 0.71f, 0.6f));
        CreateMaterial("SeniorShirt", new Color(0.33f, 0.36f, 0.44f));
        CreateMaterial("CoworkerShirt", new Color(0.62f, 0.68f, 0.78f));
        CreateMaterial("Pants", new Color(0.14f, 0.14f, 0.18f));
        CreateMaterial("Hair", new Color(0.07f, 0.06f, 0.06f));
        CreateMaterial("Eye", new Color(0.02f, 0.02f, 0.02f));
        CreateMaterial("Elevator", new Color(0.62f, 0.64f, 0.67f), null, 0.7f);
        CreateMaterial("Corridor", new Color(0.09f, 0.09f, 0.1f));
        CreateMaterial("DoorPanel", new Color(0.34f, 0.35f, 0.39f));
        CreateMaterial("ButtonRed", new Color(0.9f, 0.1f, 0.1f), new Color(0.6f, 0.02f, 0.02f));
        CreateMaterial("ClockFace", new Color(0.95f, 0.95f, 0.92f));
        CreateMaterial("ClockHand", new Color(0.05f, 0.05f, 0.05f));
        AssetDatabase.SaveAssets();
    }

    private static void CreateMaterial(string materialName, Color color, Color? emission = null, float smoothness = 0.25f)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(LitShaderName));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);

        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
        }

        EditorUtility.SetDirty(material);
        s_materials[materialName] = material;
    }

    // 문제 데이터

    private static OfficeCaseDatabase CreateDatabase()
    {
        OfficeCaseDatabase database = AssetDatabase.LoadAssetAtPath<OfficeCaseDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<OfficeCaseDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        database.SetCases(CreateCases());
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        return database;
    }

    private static List<OfficeCase> CreateCases()
    {
        return new List<OfficeCase>
        {
            // 정상(미끼, 무서운 정상 포함)
            Normal("", "코드도 사무실도 원본 그대로였다."),
            Normal("lights = 4;", "앞쪽 조명 2개가 꺼져 4개만 켜져 있었다. 코드대로다.", Effect(OfficeEffectType.LightsOff, 2)),
            Normal("lights.color = Color.red;", "조명이 전부 빨간색이었다. 섬뜩하지만 코드대로다.", Effect(OfficeEffectType.LightsRed)),
            Normal("// 뒤를 돌아보지 마\nsenior.head.rotation = 180;", "시니어의 고개가 뒤로 돌아가 있었다. 코드대로다.", Effect(OfficeEffectType.SeniorHeadTurned)),
            Normal("coworker.pose = Pose.T;", "동료가 T자 자세로 걸어왔다. 코드대로다.", Effect(OfficeEffectType.CoworkerTPose)),
            Normal("senior.pose = Pose.T;", "시니어가 T자 자세로 앉아 있었다. 코드대로다.", Effect(OfficeEffectType.SeniorTPose)),
            Normal("chairs.RemoveAt(2);", "2번 의자가 없었다. 코드대로다.", Effect(OfficeEffectType.ChairMissing, 2)),
            Normal("cups = 2 + 3;", "컵이 5개였다. 코드대로다.", Effect(OfficeEffectType.CupCount, 5)),
            Normal("cups = 3 - 3;  // 설거지 완료", "컵이 하나도 없었다. 코드대로다.", Effect(OfficeEffectType.CupCount, 0)),
            Normal("for (int i = 0; i < 2; i++)\n    monitors[i].Off();", "i < 2 는 0, 1 두 번 반복한다. 0번, 1번 모니터만 꺼진 게 맞다.",
                Effect(OfficeEffectType.MonitorOff, 0), Effect(OfficeEffectType.MonitorOff, 1)),
            Normal("coworker.LookAt(player);", "동료가 멈춰 서서 플레이어를 계속 바라봤다. 코드대로다.", Effect(OfficeEffectType.CoworkerStaring)),
            Normal("poster.Rotate(180);", "포스터가 거꾸로 걸려 있었다. 코드대로다.", Effect(OfficeEffectType.PosterFlipped)),
            Normal("plant = null;", "화분이 사라져 있었다. 코드대로다.", Effect(OfficeEffectType.PlantMissing)),
            Normal("if (false)\n{\n    lights = 0;\n}", "if (false) 안의 코드는 실행되지 않는다. 사무실은 원본 그대로였다."),

            // 원본 이탈
            Bug("", AnomalyCategory.OriginalDeviation, "코드는 비어 있는데 동료가 T자 자세였다.", Effect(OfficeEffectType.CoworkerTPose)),
            Bug("", AnomalyCategory.OriginalDeviation, "코드는 비어 있는데 3번 모니터의 텍스처가 빠져 있었다(분홍색).", Effect(OfficeEffectType.MonitorTextureMissing, 3)),
            Bug("", AnomalyCategory.OriginalDeviation, "코드는 비어 있는데 3번 의자가 바닥에 파묻혀 있었다(클리핑).", Effect(OfficeEffectType.ChairSunken, 3)),
            Bug("lights.color = Color.red;", AnomalyCategory.OriginalDeviation, "빨간 조명은 코드대로였지만, 화분이 사라져 있었다.",
                Effect(OfficeEffectType.LightsRed), Effect(OfficeEffectType.PlantMissing)),
            Bug("coworker.pose = Pose.T;", AnomalyCategory.OriginalDeviation, "T자 자세는 코드대로였지만, 동료가 책상을 뚫고 걸어 다녔다(클리핑).",
                Effect(OfficeEffectType.CoworkerTPose), Effect(OfficeEffectType.CoworkerClipping)),
            Bug("// 오늘도 무사히", AnomalyCategory.OriginalDeviation, "주석은 실행되지 않는다. 그런데 시니어가 자리에 없었다.", Effect(OfficeEffectType.SeniorAbsent)),

            // 명세 불일치
            Bug("lights = 4;", AnomalyCategory.SpecMismatch, "코드는 lights = 4 인데 켜진 조명은 5개였다.", Effect(OfficeEffectType.LightsOff, 1)),
            Bug("cups = 2 + 3;", AnomalyCategory.SpecMismatch, "코드대로라면 컵은 5개인데 4개였다.", Effect(OfficeEffectType.CupCount, 4)),
            Bug("chairs.RemoveAt(2);", AnomalyCategory.SpecMismatch, "사라져야 할 의자는 2번인데 4번 의자가 사라져 있었다.", Effect(OfficeEffectType.ChairMissing, 4)),
            Bug("senior.pose = Pose.T;", AnomalyCategory.SpecMismatch, "코드는 시니어의 T자 자세인데, T자 자세를 한 건 동료였다.", Effect(OfficeEffectType.CoworkerTPose)),

            // 코딩 지식
            Bug("for (int i = 0; i <= 2; i++)\n    monitors[i].Off();", AnomalyCategory.CodingKnowledge, "i <= 2 는 0, 1, 2 세 번 반복한다. 그런데 꺼진 모니터는 2대뿐이었다.",
                Effect(OfficeEffectType.MonitorOff, 0), Effect(OfficeEffectType.MonitorOff, 1)),
            Bug("monitors[1].Off();", AnomalyCategory.CodingKnowledge, "배열은 0부터 센다. 꺼져야 할 것은 1번인데 0번 모니터가 꺼져 있었다.", Effect(OfficeEffectType.MonitorOff, 0)),
            Bug("cups = 7 / 2;", AnomalyCategory.CodingKnowledge, "정수 나눗셈 7 / 2 는 3이다. 그런데 컵이 4개였다.", Effect(OfficeEffectType.CupCount, 4)),
            Bug("if (false)\n{\n    senior.Leave();\n}", AnomalyCategory.CodingKnowledge, "if (false) 안은 실행되지 않는다. 그런데 시니어가 자리에 없었다.", Effect(OfficeEffectType.SeniorAbsent)),
            Bug("senior.head.rotation = 360;", AnomalyCategory.CodingKnowledge, "360도 회전은 제자리다. 그런데 시니어의 고개가 뒤로 돌아가 있었다.", Effect(OfficeEffectType.SeniorHeadTurned)),
        };
    }

    private static OfficeCase Normal(string code, string explanation, params OfficeEffect[] effects) => new(code, false, AnomalyCategory.None, explanation, effects);

    private static OfficeCase Bug(string code, AnomalyCategory category, string explanation, params OfficeEffect[] effects) => new(code, true, category, explanation, effects);

    private static OfficeEffect Effect(OfficeEffectType type, int value = 0) => new(type, value);

    // 공간

    private static void SetupEnvironmentLighting()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.23f);
    }

    // 방 크기 : X -6~6, Z 0~16, 높이 3
    private static void BuildRoom(Transform office)
    {
        Transform room = CreateGroup("Room", office);

        // 조명 영향 개수 제한을 피하려고 바닥, 천장을 4m 단위로 분할
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = 0; z < 4; z++)
            {
                CreateBox($"Floor_{x}_{z}", room, new Vector3(x * 3f, -0.05f, 2f + z * 4f), new Vector3(6f, 0.1f, 4f), "Floor");
                CreateBox($"Ceiling_{x}_{z}", room, new Vector3(x * 3f, 3.05f, 2f + z * 4f), new Vector3(6f, 0.1f, 4f), "Ceiling");
            }
        }

        CreateBox("Wall_West", room, new Vector3(-6.1f, 1.5f, 8f), new Vector3(0.2f, 3f, 16.4f), "Wall");
        CreateBox("Wall_South", room, new Vector3(0f, 1.5f, -0.1f), new Vector3(12.4f, 3f, 0.2f), "Wall");

        // 북쪽 벽 가운데는 Merge 출구(폭 2m, 높이 2.4m)
        CreateBox("Wall_North_Left", room, new Vector3(-3.6f, 1.5f, 16.1f), new Vector3(5.2f, 3f, 0.2f), "Wall");
        CreateBox("Wall_North_Right", room, new Vector3(3.6f, 1.5f, 16.1f), new Vector3(5.2f, 3f, 0.2f), "Wall");
        CreateBox("Wall_North_Lintel", room, new Vector3(0f, 2.7f, 16.1f), new Vector3(2f, 0.6f, 0.2f), "Wall");

        // 동쪽 벽 Z 12~14는 엘리베이터 입구
        CreateBox("Wall_East_Front", room, new Vector3(6.1f, 1.5f, 6f), new Vector3(0.2f, 3f, 12f), "Wall");
        CreateBox("Wall_East_Back", room, new Vector3(6.1f, 1.5f, 15f), new Vector3(0.2f, 3f, 2f), "Wall");
        CreateBox("Wall_East_Lintel", room, new Vector3(6.1f, 2.7f, 13f), new Vector3(0.2f, 0.6f, 2f), "Wall");

        CreateWorldText("MergeSign", room, new Vector3(0f, 2.7f, 15.97f), Quaternion.identity, new Vector2(2f, 0.5f), 120, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 0.5f), "MERGE  ↑");
        CreateWorldText("ElevatorSign", room, new Vector3(5.97f, 2.7f, 13f), Quaternion.Euler(0f, 90f, 0f), new Vector2(1.6f, 0.4f), 100, TextAnchor.MiddleCenter, new Color(1f, 0.75f, 0.3f), "▼  1F");
    }

    private static void BuildExitCorridor(Transform office, out PlayerZoneTrigger mergeZone)
    {
        Transform corridor = CreateGroup("ExitCorridor", office);
        CreateBox("Floor", corridor, new Vector3(0f, -0.05f, 19.2f), new Vector3(2.4f, 0.1f, 6f), "Corridor");
        CreateBox("Ceiling", corridor, new Vector3(0f, 2.45f, 19.2f), new Vector3(2.4f, 0.1f, 6f), "Corridor");
        CreateBox("Wall_Left", corridor, new Vector3(-1.1f, 1.2f, 19.2f), new Vector3(0.2f, 2.4f, 6f), "Corridor");
        CreateBox("Wall_Right", corridor, new Vector3(1.1f, 1.2f, 19.2f), new Vector3(0.2f, 2.4f, 6f), "Corridor");
        CreateBox("Wall_End", corridor, new Vector3(0f, 1.2f, 22.3f), new Vector3(2.4f, 2.4f, 0.2f), "Corridor");
        CreateLight("CorridorLight", corridor, new Vector3(0f, 2.2f, 17.5f), new Color(0.5f, 1f, 0.6f), 0.6f, 4f);

        mergeZone = CreateTrigger<PlayerZoneTrigger>("MergeZone", corridor, new Vector3(0f, 1.2f, 18.5f), new Vector3(2f, 2.4f, 1f));
    }

    // 엘리베이터 칸 : X 6.2~8.5, Z 12~14
    private static void BuildElevator(Transform office, out ElevatorDoor door, out PlayerZoneTrigger elevatorZone)
    {
        Transform elevator = CreateGroup("Elevator", office);
        CreateBox("Floor", elevator, new Vector3(7.35f, -0.05f, 13f), new Vector3(2.3f, 0.1f, 2.2f), "Elevator");
        CreateBox("Ceiling", elevator, new Vector3(7.35f, 2.65f, 13f), new Vector3(2.3f, 0.1f, 2.2f), "Elevator");
        CreateBox("Wall_Back", elevator, new Vector3(8.55f, 1.3f, 13f), new Vector3(0.1f, 2.6f, 2.2f), "Elevator");
        CreateBox("Wall_Left", elevator, new Vector3(7.35f, 1.3f, 11.95f), new Vector3(2.3f, 2.6f, 0.1f), "Elevator");
        CreateBox("Wall_Right", elevator, new Vector3(7.35f, 1.3f, 14.05f), new Vector3(2.3f, 2.6f, 0.1f), "Elevator");
        CreateLight("ElevatorLight", elevator, new Vector3(7.35f, 2.4f, 13f), new Color(1f, 0.95f, 0.85f), 1f, 4f);

        // 문의 로컬 X축이 월드 +Z를 향하도록 회전(문짝이 Z 방향으로 열림)
        Transform doorRoot = CreateGroup("Door", elevator);
        doorRoot.SetLocalPositionAndRotation(new Vector3(6.1f, 0f, 13f), Quaternion.Euler(0f, -90f, 0f));
        GameObject leftPanel = CreateBox("Panel_Left", doorRoot, new Vector3(-0.5f, 1.2f, 0f), new Vector3(1f, 2.4f, 0.08f), "Elevator");
        GameObject rightPanel = CreateBox("Panel_Right", doorRoot, new Vector3(0.5f, 1.2f, 0f), new Vector3(1f, 2.4f, 0.08f), "Elevator");
        door = doorRoot.gameObject.AddComponent<ElevatorDoor>();
        SetFields(door, ("_leftPanel", leftPanel.transform), ("_rightPanel", rightPanel.transform));

        elevatorZone = CreateTrigger<PlayerZoneTrigger>("ElevatorZone", elevator, new Vector3(7.6f, 1.2f, 13f), new Vector3(1.4f, 2.4f, 1.6f));
    }

    // 조명 번호 0,1 앞줄 / 2,3 가운데 / 4,5 안쪽
    private static void BuildLamps(Transform office, out List<Renderer> lampRenderers, out List<Light> lampLights)
    {
        Transform lamps = CreateGroup("Lamps", office);
        lampRenderers = new List<Renderer>();
        lampLights = new List<Light>();

        for (int i = 0; i < LampPositions.Length; i++)
        {
            Vector3 position = LampPositions[i];
            GameObject lamp = CreateBox($"Lamp_{i}", lamps, new Vector3(position.x, 2.97f, position.z), new Vector3(1.2f, 0.05f, 0.6f), "LampOn", false);
            lampRenderers.Add(lamp.GetComponent<Renderer>());
            lampLights.Add(CreateLight($"LampLight_{i}", lamps, new Vector3(position.x, 2.7f, position.z), new Color(1f, 0.95f, 0.88f), 2.2f, 7.5f));
        }
    }

    private static void BuildDesks(Transform office, out List<Transform> chairs, out List<Renderer> screens)
    {
        Transform desks = CreateGroup("Desks", office);
        chairs = new List<Transform>();
        screens = new List<Renderer>();

        for (int i = 0; i < DeskPositions.Length; i++)
        {
            Vector3 position = DeskPositions[i];
            Transform desk = CreateGroup($"Desk_{i}", desks);
            desk.localPosition = position;
            CreateBox("Top", desk, new Vector3(0f, 0.74f, 0f), new Vector3(1.6f, 0.05f, 0.8f), "Desk");
            CreateBox("Side_Left", desk, new Vector3(-0.76f, 0.36f, 0f), new Vector3(0.05f, 0.72f, 0.76f), "DeskPanel");
            CreateBox("Side_Right", desk, new Vector3(0.76f, 0.36f, 0f), new Vector3(0.05f, 0.72f, 0.76f), "DeskPanel");
            CreateBox("Keyboard", desk, new Vector3(0f, 0.775f, -0.15f), new Vector3(0.45f, 0.02f, 0.15f), "MonitorBody", false);

            // 모니터 화면은 플레이어 쪽(-Z)을 향함
            Transform monitor = CreateGroup("Monitor", desk);
            monitor.localPosition = new Vector3(0f, 0.765f, 0.18f);
            CreateBox("Stand", monitor, new Vector3(0f, 0.12f, 0.03f), new Vector3(0.08f, 0.24f, 0.06f), "MonitorBody", false);
            CreateBox("Frame", monitor, new Vector3(0f, 0.42f, 0f), new Vector3(0.62f, 0.4f, 0.04f), "MonitorBody");
            GameObject screen = CreateBox("Screen", monitor, new Vector3(0f, 0.43f, -0.022f), new Vector3(0.56f, 0.32f, 0.005f), "ScreenOn", false);
            screens.Add(screen.GetComponent<Renderer>());
            CreateWorldText("Label", monitor, new Vector3(0f, 0.245f, -0.025f), Quaternion.identity, new Vector2(0.3f, 0.06f), 22, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.85f), $"#{i}");

            // 의자 등받이는 플레이어 쪽(-Z)
            Transform chair = CreateGroup($"Chair_{i}", desks);
            chair.localPosition = position + new Vector3(0f, 0f, -0.75f);
            CreateBox("Base", chair, new Vector3(0f, 0.03f, 0f), new Vector3(0.5f, 0.04f, 0.5f), "Chair", false);
            CreateBox("Post", chair, new Vector3(0f, 0.24f, 0f), new Vector3(0.06f, 0.4f, 0.06f), "Chair", false);
            CreateBox("Seat", chair, new Vector3(0f, 0.46f, 0f), new Vector3(0.5f, 0.07f, 0.5f), "Chair");
            CreateBox("Back", chair, new Vector3(0f, 0.76f, -0.24f), new Vector3(0.5f, 0.55f, 0.06f), "Chair");
            CreateWorldText("Label", chair, new Vector3(0f, 0.86f, -0.28f), Quaternion.identity, new Vector2(0.4f, 0.12f), 44, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.9f), $"#{i}");
            chairs.Add(chair);
        }
    }

    private static void BuildPantry(Transform office, out List<GameObject> cups, out GameObject plant, out Transform poster)
    {
        Transform pantry = CreateGroup("Pantry", office);

        Transform table = CreateGroup("CupTable", pantry);
        table.localPosition = new Vector3(4.6f, 0f, 2.4f);
        CreateBox("Top", table, new Vector3(0f, 0.74f, 0f), new Vector3(1.4f, 0.05f, 0.7f), "Desk");
        CreateBox("Side_Left", table, new Vector3(-0.66f, 0.36f, 0f), new Vector3(0.05f, 0.72f, 0.66f), "DeskPanel");
        CreateBox("Side_Right", table, new Vector3(0.66f, 0.36f, 0f), new Vector3(0.05f, 0.72f, 0.66f), "DeskPanel");
        CreateBox("WaterDispenser", pantry, new Vector3(5.6f, 0.55f, 3.6f), new Vector3(0.35f, 1.1f, 0.35f), "Cup");

        cups = new List<GameObject>();
        const int CupSlots = 8;
        for (int i = 0; i < CupSlots; i++)
        {
            cups.Add(CreatePrimitive(PrimitiveType.Cylinder, $"Cup_{i}", table, new Vector3(-0.56f + i * 0.16f, 0.815f, 0f), new Vector3(0.08f, 0.05f, 0.08f), "Cup", false));
        }

        Transform plantRoot = CreateGroup("Plant", pantry);
        plantRoot.localPosition = new Vector3(-5.3f, 0f, 1f);
        CreatePrimitive(PrimitiveType.Cylinder, "Pot", plantRoot, new Vector3(0f, 0.2f, 0f), new Vector3(0.4f, 0.2f, 0.4f), "PlantPot");
        CreatePrimitive(PrimitiveType.Sphere, "Leaves", plantRoot, new Vector3(0f, 0.8f, 0f), new Vector3(0.6f, 0.7f, 0.6f), "PlantLeaf", false);
        CreatePrimitive(PrimitiveType.Sphere, "LeavesTop", plantRoot, new Vector3(0.1f, 1.15f, 0.05f), new Vector3(0.4f, 0.5f, 0.4f), "PlantLeaf", false);
        plant = plantRoot.gameObject;

        // 로컬 +Z가 벽(동쪽)을 향하도록 회전, 글자는 방 안에서 읽힘
        poster = CreateGroup("Poster", pantry);
        poster.SetLocalPositionAndRotation(new Vector3(5.99f, 1.7f, 7f), Quaternion.Euler(0f, 90f, 0f));
        CreateBox("Board", poster, Vector3.zero, new Vector3(0.7f, 0.95f, 0.02f), "Poster", false);
        CreateWorldText("Title", poster, new Vector3(0f, 0.15f, -0.015f), Quaternion.identity, new Vector2(0.64f, 0.5f), 70, TextAnchor.MiddleCenter, new Color(0.2f, 0.15f, 0.1f), "버그 없는\n하루");
        CreateWorldText("Footer", poster, new Vector3(0f, -0.33f, -0.015f), Quaternion.identity, new Vector2(0.64f, 0.15f), 36, TextAnchor.MiddleCenter, new Color(0.35f, 0.25f, 0.15f), "QA TEAM ↑");
    }

    // 시니어 : 1번 책상에 앉아 등을 보임
    private static WorkerCharacter BuildSenior(Transform office)
    {
        // 앉은 자세 표현을 위해 발을 바닥 아래로 내림
        WorkerCharacter senior = CreateWorker("Senior", office, new Vector3(-3f, -0.3f, 7.75f), 0f, "SeniorShirt", true);
        SetArray(senior, "_patrolPoints", new List<Transform>());
        return senior;
    }

    // 동료 : 가운데 통로를 따라 플레이어 쪽으로 걸어왔다가 돌아감
    private static WorkerCharacter BuildCoworker(Transform office)
    {
        Transform path = CreateGroup("CoworkerPath", office);
        Transform far = CreateGroup("Point_Far", path);
        far.localPosition = new Vector3(0f, 0f, 14f);
        Transform near = CreateGroup("Point_Near", path);
        near.localPosition = new Vector3(0f, 0f, 3f);

        WorkerCharacter coworker = CreateWorker("Coworker", office, far.localPosition, 180f, "CoworkerShirt", false);
        SetArray(coworker, "_patrolPoints", new List<Transform> { far, near });
        return coworker;
    }

    private static WorkerCharacter CreateWorker(string workerName, Transform parent, Vector3 position, float yaw, string shirtMaterial, bool hasHair)
    {
        Transform root = CreateGroup(workerName, parent);
        root.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        Transform visual = CreateGroup("Visual", root);
        CreatePrimitive(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, 0.95f, 0f), new Vector3(0.45f, 0.5f, 0.32f), shirtMaterial);
        CreateBox("Leg_Left", visual, new Vector3(-0.1f, 0.24f, 0f), new Vector3(0.15f, 0.48f, 0.16f), "Pants", false);
        CreateBox("Leg_Right", visual, new Vector3(0.1f, 0.24f, 0f), new Vector3(0.15f, 0.48f, 0.16f), "Pants", false);

        Transform head = CreateGroup("Head", visual);
        head.localPosition = new Vector3(0f, 1.62f, 0f);
        CreatePrimitive(PrimitiveType.Sphere, "Skull", head, Vector3.zero, new Vector3(0.28f, 0.3f, 0.28f), "Skin", false);
        CreateBox("Eye_Left", head, new Vector3(-0.06f, 0.03f, 0.135f), new Vector3(0.05f, 0.03f, 0.02f), "Eye", false);
        CreateBox("Eye_Right", head, new Vector3(0.06f, 0.03f, 0.135f), new Vector3(0.05f, 0.03f, 0.02f), "Eye", false);
        if (hasHair)
            CreatePrimitive(PrimitiveType.Sphere, "Hair", head, new Vector3(0f, 0.06f, -0.03f), new Vector3(0.3f, 0.24f, 0.3f), "Hair", false);

        Transform leftArm = CreateArm("Arm_Left", visual, new Vector3(-0.29f, 1.38f, 0f), shirtMaterial);
        Transform rightArm = CreateArm("Arm_Right", visual, new Vector3(0.29f, 1.38f, 0f), shirtMaterial);

        var worker = root.gameObject.AddComponent<WorkerCharacter>();
        SetFields(worker, ("_visual", visual), ("_head", head), ("_leftArm", leftArm), ("_rightArm", rightArm));
        return worker;
    }

    private static Transform CreateArm(string armName, Transform parent, Vector3 shoulder, string material)
    {
        Transform pivot = CreateGroup(armName, parent);
        pivot.localPosition = shoulder;
        CreateBox("Arm", pivot, new Vector3(0f, -0.28f, 0f), new Vector3(0.1f, 0.56f, 0.1f), material, false);
        CreateBox("Hand", pivot, new Vector3(0f, -0.6f, 0f), new Vector3(0.09f, 0.1f, 0.09f), "Skin", false);
        return pivot;
    }

    // 시작 지점 앞 천장에 매달린 코드 화면(앞뒤 양면, 사무실 안쪽 시야를 가리지 않도록 높게)
    private static CodeMonitorView BuildCodeScreen(Transform office)
    {
        Transform screen = CreateGroup("CodeScreen", office);
        screen.localPosition = new Vector3(0f, 2.5f, 3.2f);
        CreateBox("Panel", screen, Vector3.zero, new Vector3(2.8f, 0.9f, 0.08f), "CodeScreen", false);
        CreateBox("Hanger_Left", screen, new Vector3(-1.2f, 0.48f, 0f), new Vector3(0.03f, 0.06f, 0.03f), "MonitorBody", false);
        CreateBox("Hanger_Right", screen, new Vector3(1.2f, 0.48f, 0f), new Vector3(0.03f, 0.06f, 0.03f), "MonitorBody", false);

        Color codeColor = new(0.86f, 0.86f, 0.86f);
        Text front = CreateWorldText("Code_Front", screen, new Vector3(0f, 0f, -0.045f), Quaternion.identity, new Vector2(2.7f, 0.85f), 44, TextAnchor.UpperLeft, codeColor, string.Empty);
        Text back = CreateWorldText("Code_Back", screen, new Vector3(0f, 0f, 0.045f), Quaternion.Euler(0f, 180f, 0f), new Vector2(2.7f, 0.85f), 44, TextAnchor.UpperLeft, codeColor, string.Empty);

        var view = screen.gameObject.AddComponent<CodeMonitorView>();
        SetArray(view, "_codeTexts", new List<Text> { front, back });
        return view;
    }

    // 북쪽 벽 왼편 시계(진행도 표시)
    private static WallClock BuildClock(Transform office)
    {
        Transform clockRoot = CreateGroup("WallClock", office);
        clockRoot.localPosition = new Vector3(-3f, 2.15f, 15.98f);
        CreatePrimitive(PrimitiveType.Cylinder, "Face", clockRoot, Vector3.zero, new Vector3(0.8f, 0.01f, 0.8f), "ClockFace", false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        const int TickCount = 12;
        for (int i = 0; i < TickCount; i++)
        {
            float angle = i * 360f / TickCount;
            Vector3 tickPosition = Quaternion.Euler(0f, 0f, -angle) * new Vector3(0f, 0.34f, -0.012f);
            CreateBox($"Tick_{i}", clockRoot, tickPosition, new Vector3(0.02f, 0.06f, 0.005f), "ClockHand", false).transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        Transform hourHand = CreateGroup("HourHand", clockRoot);
        hourHand.localPosition = new Vector3(0f, 0f, -0.02f);
        CreateBox("Hand", hourHand, new Vector3(0f, 0.1f, 0f), new Vector3(0.035f, 0.2f, 0.01f), "ClockHand", false);

        Transform minuteHand = CreateGroup("MinuteHand", clockRoot);
        minuteHand.localPosition = new Vector3(0f, 0f, -0.03f);
        CreateBox("Hand", minuteHand, new Vector3(0f, 0.16f, 0f), new Vector3(0.022f, 0.32f, 0.01f), "ClockHand", false);

        Text digital = CreateWorldText("Digital", clockRoot, new Vector3(0f, -0.62f, -0.01f), Quaternion.identity, new Vector2(0.9f, 0.3f), 110, TextAnchor.MiddleCenter, new Color(1f, 0.7f, 0.25f), "22:00");

        var clock = clockRoot.gameObject.AddComponent<WallClock>();
        SetFields(clock, ("_hourHand", hourHand), ("_minuteHand", minuteHand), ("_digitalText", digital));
        return clock;
    }

    // 남쪽 벽(시작 지점 뒤) 수정 요청 버튼과 닫힌 문
    private static SubmitButton BuildSubmitButton(Transform office)
    {
        Transform back = CreateGroup("BackDoor", office);
        CreateBox("Door", back, new Vector3(0f, 1.2f, 0.02f), new Vector3(1.6f, 2.4f, 0.04f), "DoorPanel", false);
        CreateWorldText("DoorLabel", back, new Vector3(0f, 2.65f, 0.01f), Quaternion.Euler(0f, 180f, 0f), new Vector2(2.4f, 0.4f), 80, TextAnchor.MiddleCenter, new Color(1f, 0.45f, 0.45f), "REQUEST CHANGES");

        Transform buttonRoot = CreateGroup("SubmitButton", back);
        buttonRoot.localPosition = new Vector3(1.5f, 1.25f, 0f);
        CreateBox("Plate", buttonRoot, new Vector3(0f, 0f, 0.02f), new Vector3(0.32f, 0.42f, 0.04f), "DoorPanel");
        GameObject cap = CreateBox("Cap", buttonRoot, new Vector3(0f, 0f, 0.07f), new Vector3(0.16f, 0.16f, 0.06f), "ButtonRed");
        CreateWorldText("Label", buttonRoot, new Vector3(0f, 0.36f, 0.045f), Quaternion.Euler(0f, 180f, 0f), new Vector2(0.8f, 0.2f), 50, TextAnchor.MiddleCenter, Color.white, "수정 요청");

        var button = buttonRoot.gameObject.AddComponent<SubmitButton>();
        SetFields(button, ("_buttonCap", cap.transform));
        return button;
    }

    // 시작 지점 왼편의 버그 리포트 컴퓨터(화면이 방 안쪽 +X를 향함)
    private static BugReportTerminal BuildBugReportTerminal(Transform office)
    {
        Transform terminal = CreateGroup("BugReportTerminal", office);
        terminal.SetLocalPositionAndRotation(new Vector3(-4.8f, 0f, 2.6f), Quaternion.Euler(0f, -90f, 0f));
        CreateBox("Top", terminal, new Vector3(0f, 0.74f, 0f), new Vector3(0.9f, 0.05f, 0.6f), "Desk");
        CreateBox("Side_Left", terminal, new Vector3(-0.42f, 0.36f, 0f), new Vector3(0.05f, 0.72f, 0.56f), "DeskPanel");
        CreateBox("Side_Right", terminal, new Vector3(0.42f, 0.36f, 0f), new Vector3(0.05f, 0.72f, 0.56f), "DeskPanel");
        CreateBox("Frame", terminal, new Vector3(0f, 1.15f, 0.12f), new Vector3(0.66f, 0.44f, 0.04f), "MonitorBody");
        CreateBox("Stand", terminal, new Vector3(0f, 0.86f, 0.14f), new Vector3(0.08f, 0.2f, 0.06f), "MonitorBody", false);
        CreateBox("Screen", terminal, new Vector3(0f, 1.15f, 0.098f), new Vector3(0.62f, 0.4f, 0.005f), "CodeScreen", false);
        Text screenText = CreateWorldText("ScreenText", terminal, new Vector3(0f, 1.15f, 0.094f), Quaternion.identity, new Vector2(0.6f, 0.38f), 18, TextAnchor.UpperLeft, new Color(0.85f, 0.95f, 0.85f), string.Empty);

        var bugReport = terminal.gameObject.AddComponent<BugReportTerminal>();
        SetFields(bugReport, ("_screenText", screenText));
        return bugReport;
    }

    private static Transform BuildPoints(out Transform playerStart, out Transform seniorStand, out List<Transform> leavePath, out Transform elevatorInside)
    {
        Transform points = new GameObject("Points").transform;
        playerStart = CreateGroup("PlayerStart", points);
        playerStart.localPosition = new Vector3(0f, 0f, 1.2f);

        seniorStand = CreateGroup("SeniorStand", points);
        seniorStand.localPosition = new Vector3(-1.5f, 0f, 7.4f);

        leavePath = new List<Transform>();
        Vector3[] leavePositions = { new(0f, 0f, 13f), new(5f, 0f, 13f), new(7.6f, 0f, 13f) };
        for (int i = 0; i < leavePositions.Length; i++)
        {
            Transform point = CreateGroup($"LeavePath_{i}", points);
            point.localPosition = leavePositions[i];
            leavePath.Add(point);
        }

        // 엘리베이터 안에서 사무실(서쪽)을 바라봄
        elevatorInside = CreateGroup("ElevatorInside", points);
        elevatorInside.SetLocalPositionAndRotation(new Vector3(7.5f, 0f, 13f), Quaternion.Euler(0f, -90f, 0f));
        return points;
    }

    // 플레이어와 UI

    private static FirstPersonController BuildPlayer(Transform start, ScreenOverlayView overlay, out Transform playerCamera)
    {
        var playerObject = new GameObject("Player") { tag = "Player" };
        playerObject.transform.position = start.position;

        var characterController = playerObject.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.3f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.stepOffset = 0.3f;

        var cameraObject = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
        cameraObject.transform.SetParent(playerObject.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.fieldOfView = 70f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        playerCamera = cameraObject.transform;

        var controller = playerObject.AddComponent<FirstPersonController>();
        SetFields(controller, ("_moveAction", FindActionReference("Move")), ("_lookAction", FindActionReference("Look")), ("_cameraPivot", playerCamera));

        var interactor = playerObject.AddComponent<PlayerInteractor>();
        SetFields(interactor, ("_interactAction", FindActionReference("Interact")), ("_rayOrigin", playerCamera), ("_controller", controller), ("_overlay", overlay));
        return controller;
    }

    private static ScreenOverlayView BuildOverlay()
    {
        var canvasObject = new GameObject("ScreenOverlay", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Transform root = canvasObject.transform;
        Text crosshair = CreateScreenText("Crosshair", root, Vector2.zero, new Vector2(60f, 60f), 30, new Color(1f, 1f, 1f, 0.6f), "+");
        Text prompt = CreateScreenText("Prompt", root, new Vector2(0f, -90f), new Vector2(900f, 60f), 30, Color.white, string.Empty);
        Text hint = CreateScreenText("Hint", root, new Vector2(0f, -500f), new Vector2(1700f, 50f), 24, new Color(0.85f, 0.85f, 0.85f, 0.85f), string.Empty);
        Text message = CreateScreenText("Message", root, new Vector2(0f, 330f), new Vector2(1700f, 120f), 34, Color.white, string.Empty);

        var fadeObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fadeObject.transform.SetParent(root, false);
        StretchToParent((RectTransform)fadeObject.transform);
        var fade = fadeObject.GetComponent<Image>();
        fade.color = Color.black;
        fade.raycastTarget = false;

        Text center = CreateScreenText("Center", root, new Vector2(0f, 40f), new Vector2(1700f, 500f), 72, Color.white, string.Empty);
        Text sub = CreateScreenText("Sub", root, new Vector2(0f, -230f), new Vector2(1500f, 100f), 32, new Color(0.8f, 0.8f, 0.8f), string.Empty);

        var overlay = canvasObject.AddComponent<ScreenOverlayView>();
        SetFields(overlay, ("_fadeImage", fade), ("_centerText", center), ("_subText", sub), ("_messageText", message), ("_hintText", hint), ("_promptText", prompt), ("_crosshair", crosshair.gameObject));
        return overlay;
    }

    // 공용 생성 도구

    private static Transform CreateGroup(string groupName, Transform parent)
    {
        Transform group = new GameObject(groupName).transform;
        group.SetParent(parent, false);
        return group;
    }

    private static GameObject CreateBox(string boxName, Transform parent, Vector3 localPosition, Vector3 localScale, string material, bool hasCollider = true)
        => CreatePrimitive(PrimitiveType.Cube, boxName, parent, localPosition, localScale, material, hasCollider);

    private static GameObject CreatePrimitive(PrimitiveType type, string primitiveName, Transform parent, Vector3 localPosition, Vector3 localScale, string material, bool hasCollider = true)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = primitiveName;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localScale = localScale;
        primitive.GetComponent<Renderer>().sharedMaterial = s_materials[material];

        if (!hasCollider)
            Object.DestroyImmediate(primitive.GetComponent<Collider>());

        return primitive;
    }

    private static Light CreateLight(string lightName, Transform parent, Vector3 localPosition, Color color, float intensity, float range)
    {
        var lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        return light;
    }

    private static T CreateTrigger<T>(string triggerName, Transform parent, Vector3 localPosition, Vector3 size) where T : Component
    {
        var triggerObject = new GameObject(triggerName);
        triggerObject.transform.SetParent(parent, false);
        triggerObject.transform.localPosition = localPosition;
        var box = triggerObject.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;
        return triggerObject.AddComponent<T>();
    }

    // 월드 공간 글자(캔버스 정면 +Z, 보는 사람은 -Z 쪽)
    private static Text CreateWorldText(string textName, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector2 sizeInMeters, int fontSize, TextAnchor alignment, Color color, string content)
    {
        var canvasObject = new GameObject(textName, typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(parent, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        var canvasRect = (RectTransform)canvasObject.transform;
        canvasRect.sizeDelta = sizeInMeters / WorldCanvasScale;
        canvasRect.localPosition = localPosition;
        canvasRect.localRotation = localRotation;
        canvasRect.localScale = Vector3.one * WorldCanvasScale;

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);
        var textRect = (RectTransform)textObject.transform;
        StretchToParent(textRect);
        textRect.offsetMin = new Vector2(WorldTextPadding, WorldTextPadding);
        textRect.offsetMax = new Vector2(-WorldTextPadding, -WorldTextPadding);

        var text = textObject.GetComponent<Text>();
        ConfigureText(text, fontSize, alignment, color, content);
        return text;
    }

    private static Text CreateScreenText(string textName, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, string content)
    {
        var textObject = new GameObject(textName, typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(parent, false);
        var rect = (RectTransform)textObject.transform;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var text = textObject.GetComponent<Text>();
        ConfigureText(text, fontSize, TextAnchor.MiddleCenter, color, content);
        textObject.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);
        return text;
    }

    private static void ConfigureText(Text text, int fontSize, TextAnchor alignment, Color color, string content)
    {
        text.font = s_font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = content;
        text.supportRichText = true;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // 참조 연결

    private static void SetFields(Object target, params (string Name, Object Value)[] fields)
    {
        var serializedObject = new SerializedObject(target);
        foreach ((string name, Object value) in fields)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            if (property == null)
            {
                Debug.LogError($"[8번 코딩] {target.GetType().Name}에 {name} 필드가 없습니다.");
                continue;
            }

            property.objectReferenceValue = value;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray<T>(Object target, string fieldName, List<T> values) where T : Object
    {
        var serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static InputActionReference FindActionReference(string actionName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(InputActionsPath))
        {
            if (asset is InputActionReference reference && reference.action != null
                && reference.action.actionMap.name == PlayerMapName && reference.action.name == actionName)
            {
                return reference;
            }
        }

        Debug.LogError($"[8번 코딩] 입력 액션을 찾지 못했습니다 : {PlayerMapName}/{actionName}");
        return null;
    }

    private static void EnsureFolder(string parentFolder, string folderName)
    {
        if (!AssetDatabase.IsValidFolder($"{parentFolder}/{folderName}"))
            AssetDatabase.CreateFolder(parentFolder, folderName);
    }

    // 빌드 목록 맨 앞에 씬 등록
    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene => scene.path == ScenePath || string.IsNullOrEmpty(scene.path) || !System.IO.File.Exists(scene.path));
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
