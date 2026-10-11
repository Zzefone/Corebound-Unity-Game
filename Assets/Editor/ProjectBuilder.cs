using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using CoreRunner;

public static class ProjectBuilder
{
    static Material deck, edge, teal, orange, metal, white, red;
    static Font font;
    static readonly Color Ink = new Color(.025f, .045f, .085f, .96f);
    static readonly Color Muted = new Color(.57f, .67f, .77f);
    static readonly Color Cyan = new Color(.2f, .92f, .9f);

    [MenuItem("Corebound/Apply Game Name")]
    public static void ApplyBrandName()
    {
        foreach (var path in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" })
        {
            if (!File.Exists(path)) throw new Exception("Missing saved scene: " + path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var label in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                label.text = label.text.Replace("CORE\nRUNNER", "CORE\nBOUND")
                    .Replace("CORE RUNNER", "COREBOUND").Replace("Core Runner", "Corebound");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        PlayerSettings.companyName = "Corebound";
        PlayerSettings.productName = "Corebound";
        AssetDatabase.SaveAssets();
        Debug.Log("COREBOUND_BRAND_APPLIED: saved scene geometry preserved.");
    }

    [MenuItem("Corebound/Generate Project Scenes")]
    public static void Generate()
    {
        foreach (string dir in new[] { "Scenes", "Materials", "Prefabs", "Textures" })
            Directory.CreateDirectory("Assets/" + dir);
        AssetDatabase.Refresh();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        deck = Mat("Deck", new Color(.10f, .17f, .23f), .35f);
        metal = Mat("Metal", new Color(.2f, .28f, .34f), .7f);
        edge = Mat("Edge", new Color(.03f, .08f, .12f), .5f);
        teal = Mat("Energy_Cyan", new Color(.06f, .75f, .8f), .4f, new Color(.08f, .9f, 1));
        orange = Mat("Reactor_Orange", new Color(1, .36f, .09f), .3f, new Color(1, .3f, .06f));
        white = Mat("Player", new Color(.78f, .9f, .95f), .65f);
        red = Mat("Hazard_Red", new Color(.9f, .12f, .16f), .3f, new Color(1, .09f, .1f));
        MakePlayerTexture();
        var core = MakeCore();
        var burst = MakeBurst();
        MakeGame(core, burst);
        MakeMenu();
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };
        PlayerSettings.companyName = "Corebound";
        PlayerSettings.productName = "Corebound";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        QualitySettings.vSyncCount = 1;
        QualitySettings.antiAliasing = 4;
        AssetDatabase.SaveAssets();
        Debug.Log("CORE_RUNNER_GENERATED: MainMenu, Game, prefabs, materials and build settings.");
    }

    [MenuItem("Corebound/Build Windows")]
    public static void BuildWindows()
    {
        if (!File.Exists("Assets/Scenes/Game.unity")) Generate();
        Directory.CreateDirectory("Builds/CoreboundWindows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" },
            locationPathName = "Builds/CoreboundWindows/Corebound.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows build failed: " + report.summary.result);
        Debug.Log("CORE_RUNNER_BUILD_SUCCESS: " + report.summary.totalSize + " bytes");
    }

    static Material Mat(string name, Color color, float metallic, Color? emission = null)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
        m.color = color;
        m.SetFloat("_Metallic", metallic); m.SetFloat("_Glossiness", .5f);
        if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value * 1.5f); }
        EditorUtility.SetDirty(m);
        return m;
    }
    static void MakePlayerTexture()
    {
        var t = new Texture2D(256, 128, TextureFormat.RGB24, false);
        for (int y = 0; y < t.height; y++) for (int x = 0; x < t.width; x++)
        {
            bool band = y > 51 && y < 77;
            bool rib = x % 64 < 4;
            t.SetPixel(x, y, band ? new Color(.05f, .65f, .75f) : rib ? new Color(.18f, .3f, .4f) : new Color(.85f, .93f, .96f));
        }
        t.Apply(); File.WriteAllBytes("Assets/Textures/PlayerBands.png", t.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(t); AssetDatabase.Refresh();
        white.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PlayerBands.png");
    }
    static GameObject Shape(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material material, Transform parent = null, bool collider = true)
    {
        var obj = GameObject.CreatePrimitive(type); obj.name = name;
        obj.transform.SetParent(parent, false); obj.transform.localPosition = pos; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
    static GameObject Box(string name, Vector3 pos, Vector3 scale, Material material, Transform parent = null, bool collider = true)
        => Shape(name, PrimitiveType.Cube, pos, scale, material, parent, collider);

    static GameObject MakeCore()
    {
        var root = new GameObject("EnergyCore");
        var trigger = root.AddComponent<SphereCollider>(); trigger.radius = .85f; trigger.isTrigger = true;
        var pickup = root.AddComponent<CorePickup>();
        var visual = Box("Crystal", Vector3.zero, Vector3.one * .65f, teal, root.transform, false);
        visual.transform.localRotation = Quaternion.Euler(35, 0, 45); pickup.visual = visual.transform;
        Shape("Halo", PrimitiveType.Cylinder, new Vector3(0, -.45f, 0), new Vector3(1.15f, .025f, 1.15f), metal, root.transform, false);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/EnergyCore.prefab");
        UnityEngine.Object.DestroyImmediate(root); return prefab;
    }
    static ParticleSystem MakeBurst()
    {
        var root = new GameObject("EnergyBurst"); var ps = root.AddComponent<ParticleSystem>();
        var main = ps.main; main.duration = .6f; main.loop = false; main.playOnAwake = false;
        main.startLifetime = .55f; main.startSpeed = 3; main.startSize = .13f; main.gravityModifier = .2f;
        var emission = ps.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 24) });
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .25f;
        var particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Particles.mat");
        if (!particleMat) { particleMat = new Material(Shader.Find("Particles/Standard Unlit")); AssetDatabase.CreateAsset(particleMat, "Assets/Materials/Particles.mat"); }
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMat;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/EnergyBurst.prefab");
        UnityEngine.Object.DestroyImmediate(root); return prefab.GetComponent<ParticleSystem>();
    }
    static Camera Environment()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.35f, .44f, .58f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(.025f, .04f, .075f); RenderSettings.fogDensity = .007f;
        RenderSettings.skybox = null;
        var sun = new GameObject("Station key light").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.65f; sun.color = new Color(.7f, .84f, 1);
        sun.transform.rotation = Quaternion.Euler(45, -30, 0); sun.shadows = LightShadows.Soft;
        var fill = new GameObject("Warm rim light").AddComponent<Light>();
        fill.type = LightType.Directional; fill.intensity = .6f; fill.color = new Color(1, .5f, .2f);
        fill.transform.rotation = Quaternion.Euler(15, 140, 0);
        var cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor;
        cam.fieldOfView = 52; cam.nearClipPlane = .1f; cam.farClipPlane = 250;
        cam.gameObject.AddComponent<AudioListener>();
        var stars = new GameObject("Distant station lights");
        var random = new System.Random(83);
        for (int i = 0; i < 110; i++)
        {
            float x = (float)random.NextDouble() * 170 - 85;
            float y = (float)random.NextDouble() * 20 - 14;
            float z = (float)random.NextDouble() * 160 - 20;
            if (Mathf.Abs(x) < 15) x += 25;
            Box("Signal light", new Vector3(x, y, z), Vector3.one * (.06f + (float)random.NextDouble() * .15f), teal, stars.transform, false);
        }
        return cam;
    }

    static void Platform(string name, float center, float length, float width = 12)
    {
        var parent = new GameObject(name).transform;
        Box("Deck", new Vector3(0, -.4f, center), new Vector3(width, .8f, length), deck, parent);
        Box("Foundation", new Vector3(0, -.95f, center), new Vector3(width + .35f, .35f, length + .3f), edge, parent);
        foreach (float x in new[] { -width / 2 + .15f, width / 2 - .15f })
            Box("Cyan edge strip", new Vector3(x, .02f, center), new Vector3(.12f, .05f, length), teal, parent, false);
        for (float z = center - length / 2 + 1; z < center + length / 2; z += 2)
            Box("Deck seam", new Vector3(0, .006f, z), new Vector3(width - .5f, .012f, .045f), metal, parent, false);
        // Railings on the outer lanes still leave the forward gaps open.
        foreach (float x in new[] { -width / 2, width / 2 })
        {
            Box("Guard rail", new Vector3(x, .55f, center), new Vector3(.18f, 1.1f, length), metal, parent);
            Box("Rail light", new Vector3(x, 1.13f, center), new Vector3(.12f, .04f, length), teal, parent, false);
        }
    }
    static void WorldLabel(string text, Vector3 pos, Color color, float size = .22f)
    {
        // A world-space Canvas uses scene depth. Legacy TextMesh's font shader can
        // draw through foreground UI, which would put floor lettering over modal cards.
        var obj = new GameObject(text, typeof(RectTransform), typeof(Canvas));
        obj.transform.position = pos; obj.transform.rotation = Quaternion.Euler(90, 0, 0);
        obj.transform.localScale = Vector3.one * size * .068f;
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(1600, 170);
        obj.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        Text(text, obj.transform, 0, 0, 1, 1, 70, color, TextAnchor.MiddleCenter);
    }
    static void MakeCheckpoint(float z)
    {
        var obj = new GameObject("Checkpoint " + z); obj.transform.position = new Vector3(0, .7f, z);
        var col = obj.AddComponent<BoxCollider>(); col.isTrigger = true; col.size = new Vector3(11, 2, 1.5f);
        var cp = obj.AddComponent<Checkpoint>(); cp.spawnPosition = new Vector3(0, .65f, z + 1.5f);
        cp.beacon = Box("Checkpoint stripe", new Vector3(0, -.65f, 0), new Vector3(11.5f, .025f, .6f), orange, obj.transform, false).GetComponent<Renderer>();
        cp.activatedMaterial = teal;
        WorldLabel("CHECKPOINT", new Vector3(0, .025f, z + .65f), Cyan, .13f);
    }
    static void MakeHazard(float z, float phase)
    {
        var obj = Box("Moving scanner", new Vector3(0, .7f, z), new Vector3(2.2f, 1.4f, .65f), red);
        var rb = obj.AddComponent<Rigidbody>(); rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        var hz = obj.AddComponent<MovingHazard>(); hz.phase = phase;
        Box("Scanner track", new Vector3(0, .025f, z), new Vector3(11.5f, .045f, .08f), orange, null, false);
    }
    static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ".wav");
    static void MakeGame(GameObject core, ParticleSystem burst)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = Environment();
        Platform("01 Arrival", 8, 24); // -4 to 20
        Platform("02 Gauntlet", 33, 20); // 23 to 43
        Platform("03 Reactor approach", 57, 20); // 47 to 67
        Platform("Reactor bridge", 70, 6, 5);
        Platform("Reactor platform", 77, 8);
        WorldLabel("01  ARRIVAL", new Vector3(0, .03f, 1.5f), Cyan);
        WorldLabel("SPACE  /  BOOST", new Vector3(0, .03f, 17), new Color(1, .6f, .2f), .17f);
        WorldLabel("02  GAUNTLET", new Vector3(0, .03f, 25), Cyan, .17f);
        WorldLabel("BOOST ACROSS", new Vector3(0, .03f, 40), new Color(1, .6f, .2f), .16f);
        WorldLabel("03  REACTOR", new Vector3(0, .03f, 50), Cyan, .17f);
        // Low staggered walls make turning and collisions visible without blocking the route.
        Box("Left baffle", new Vector3(-3.2f, .7f, 10), new Vector3(5.2f, 1.4f, .55f), metal);
        Box("Right baffle", new Vector3(3.2f, .7f, 14), new Vector3(5.2f, 1.4f, .55f), metal);
        MakeHazard(30, 0); MakeHazard(36, 2.2f); MakeHazard(58, 1.2f);
        MakeCheckpoint(24); MakeCheckpoint(48);
        var playerObj = Shape("Player", PrimitiveType.Sphere, new Vector3(0, .65f, 0), Vector3.one * 1.1f, white);
        playerObj.layer = 2; // Ignore Raycast: the ground probe must not hit its own sphere.
        var rb = playerObj.AddComponent<Rigidbody>(); rb.mass = 1; rb.linearDamping = .08f;
        rb.angularDamping = .3f; rb.maxAngularVelocity = 30;
        rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var player = playerObj.AddComponent<PlayerController>();
        var follower = camera.gameObject.AddComponent<FollowCamera>(); follower.target = player.transform;
        camera.transform.position = player.transform.position + follower.offset; camera.transform.rotation = Quaternion.Euler(48, 0, 0);
        var manager = new GameObject("Game Manager").AddComponent<GameManager>(); manager.player = player; manager.burstPrefab = burst;
        manager.gameObject.AddComponent<AudioSource>().spatialBlend = 0;
        manager.collectSound = Clip("Collect"); manager.boostSound = Clip("Boost"); manager.fallSound = Clip("Fall");
        manager.checkpointSound = Clip("Checkpoint"); manager.winSound = Clip("Win");
        var music = new GameObject("Station ambience").AddComponent<AudioSource>(); music.clip = Clip("Ambience"); music.loop = true; music.volume = .16f; music.playOnAwake = true;
        var spawner = new GameObject("Core Spawner").AddComponent<CoreSpawner>(); spawner.corePrefab = core;
        spawner.positions = new[] {
            new Vector3(-3, .95f, 5), new Vector3(3, .95f, 8), new Vector3(-3, .95f, 16),
            new Vector3(-3, .95f, 27), new Vector3(3, .95f, 33), new Vector3(-3, .95f, 39),
            new Vector3(3, .95f, 53), new Vector3(-3, .95f, 63)
        };
        var gate = new GameObject("Reactor exit"); gate.transform.position = new Vector3(0, 1, 77);
        var gateCollider = gate.AddComponent<BoxCollider>(); gateCollider.isTrigger = true; gateCollider.size = new Vector3(4.5f, 3, 2);
        gate.AddComponent<FinishGate>();
        foreach (float x in new[] { -2.5f, 2.5f })
            Box("Reactor pillar", new Vector3(x, .6f, 0), new Vector3(.6f, 3.2f, .6f), orange, gate.transform);
        Box("Reactor lintel", new Vector3(0, 2.15f, 0), new Vector3(5.6f, .4f, .6f), orange, gate.transform);
        WorldLabel("RESTORE THE REACTOR", new Vector3(0, .03f, 75), new Color(1, .6f, .2f), .15f);
        manager.hud = MakeHUD(manager);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/Scenes/Game.unity");
    }

    static Canvas CanvasRoot()
    {
        var obj = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return canvas;
    }
    static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(name, typeof(RectTransform)); var r = obj.GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }
    static Image Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
    {
        var r = Rect(name, parent, new Vector2(x, y), new Vector2(x + w, y + h));
        var image = r.gameObject.AddComponent<Image>(); image.color = color; return image;
    }
    static Text Text(string content, Transform parent, float x, float y, float w, float h, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var r = Rect(content.Length > 32 ? content.Substring(0, 32) : content, parent, new Vector2(x, y), new Vector2(x + w, y + h));
        var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = content; t.fontSize = size; t.color = color;
        t.alignment = align; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate; return t;
    }
    static Button Button(string label, Transform parent, float x, float y, float w, float h, bool primary, UnityEngine.Events.UnityAction action)
    {
        var panel = Panel(label, parent, x, y, w, h, primary ? new Color(.08f, .72f, .75f) : new Color(.13f, .2f, .27f));
        var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel;
        var colors = button.colors; colors.highlightedColor = new Color(.76f, .96f, 1); colors.pressedColor = new Color(.5f, .73f, .8f); button.colors = colors;
        Text(label, panel.transform, .05f, .05f, .9f, .9f, 22, primary ? Ink : Color.white, TextAnchor.MiddleCenter);
        UnityEventTools.AddPersistentListener(button.onClick, action); return button;
    }
    static GameHUD MakeHUD(GameManager manager)
    {
        var canvas = CanvasRoot(); var root = canvas.transform; var hud = canvas.gameObject.AddComponent<GameHUD>();
        var top = Panel("Status strip", root, .025f, .875f, .95f, .1f, Ink).transform;
        hud.stage = Text("01 / ARRIVAL", top, .02f, .44f, .29f, .45f, 19, Cyan);
        Text("COREBOUND", top, .02f, .08f, .29f, .35f, 16, Muted);
        hud.cores = Text("CORES  0 / 8", top, .35f, .16f, .2f, .7f, 25, Color.white);
        hud.timer = Text("00:00", top, .6f, .16f, .12f, .7f, 28, Color.white);
        hud.falls = Text("RESETS  0", top, .76f, .16f, .2f, .7f, 19, Muted);
        hud.message = Text("", root, .12f, .775f, .76f, .065f, 23, Color.white, TextAnchor.MiddleCenter);
        var bottom = Panel("Controls", root, .025f, .025f, .58f, .09f, Ink).transform;
        Text("WASD / ARROWS  Move     SPACE  Boost     R  Reset     ESC  Pause", bottom, .025f, .05f, .95f, .9f, 18, Muted);
        var boost = Panel("Boost", root, .63f, .025f, .225f, .09f, Ink).transform;
        hud.boostLabel = Text("SPACE  /  BOOST READY", boost, .06f, .36f, .88f, .55f, 17, Cyan);
        Panel("Boost track", boost, .06f, .16f, .88f, .09f, new Color(.12f, .22f, .28f));
        hud.boostFill = Panel("Boost charge", boost, .06f, .16f, .88f, .09f, Cyan);
        var pause = Panel("Paused", root, .21f, .23f, .58f, .49f, Ink).transform;
        Text("SIGNAL ON HOLD", pause, .08f, .67f, .84f, .18f, 38, Color.white, TextAnchor.MiddleCenter);
        Text("Take your time. The station will wait.", pause, .08f, .53f, .84f, .12f, 21, Muted, TextAnchor.MiddleCenter);
        Button("RESUME", pause, .25f, .32f, .5f, .14f, true, manager.TogglePause);
        Button("RESTART", pause, .12f, .1f, .35f, .13f, false, manager.Restart);
        Button("MAIN MENU", pause, .53f, .1f, .35f, .13f, false, manager.Menu);
        hud.pausePanel = pause.gameObject; pause.gameObject.SetActive(false);
        var victory = Panel("Victory", root, .18f, .22f, .64f, .51f, Ink).transform;
        Text("STATION RESTORED", victory, .06f, .7f, .88f, .17f, 42, Cyan, TextAnchor.MiddleCenter);
        Text("Every core counts. You brought the lights back.", victory, .06f, .53f, .88f, .12f, 22, Color.white, TextAnchor.MiddleCenter);
        hud.victoryStats = Text("", victory, .04f, .36f, .92f, .12f, 21, Muted, TextAnchor.MiddleCenter);
        Button("RUN AGAIN", victory, .12f, .12f, .35f, .14f, true, manager.Restart);
        Button("MAIN MENU", victory, .53f, .12f, .35f, .14f, false, manager.Menu);
        hud.victoryPanel = victory.gameObject; victory.gameObject.SetActive(false);
        // Created last so modal panels never cover the always-accessible quit button.
        Button("QUIT", root, .88f, .025f, .095f, .09f, false, manager.Quit);
        return hud;
    }
    static void MakeMenu()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = Environment();
        Platform("Station exhibit", 7, 19, 10);
        Shape("Exhibit sphere", PrimitiveType.Sphere, new Vector3(0, 1.8f, 5), Vector3.one * 3.5f, white, null, false);
        foreach (float x in new[] { -4.1f, 4.1f })
            Box("Signal tower", new Vector3(x, 2, 12), new Vector3(.5f, 4, .5f), orange, null, false);
        camera.transform.position = new Vector3(14, 13, -13); camera.transform.LookAt(new Vector3(-5, 0, 6));
        var source = new GameObject("Menu ambience").AddComponent<AudioSource>(); source.clip = Clip("Ambience"); source.loop = true; source.volume = .18f;
        var menu = new GameObject("Menu Controller").AddComponent<MenuController>();
        var canvas = CanvasRoot(); var root = canvas.transform;
        Panel("Left shade", root, 0, 0, .49f, 1, new Color(.022f, .038f, .065f, .94f));
        Panel("Accent", root, .06f, .8f, .045f, .006f, Cyan);
        Text("ORBITAL RECOVERY / SECTOR 08", root, .06f, .825f, .42f, .045f, 18, Cyan);
        Text("CORE\nBOUND", root, .06f, .55f, .42f, .26f, 82, Color.white);
        Text("A little momentum. A second chance.\nBring a silent station back to life.", root, .065f, .425f, .385f, .1f, 24, Muted);
        menu.startButton = Button("START RECOVERY", root, .065f, .285f, .33f, .08f, true, menu.StartGame);
        menu.loadingText = menu.startButton.GetComponentInChildren<Text>();
        Text("MOVE  WASD / ARROWS\nBOOST  SPACE     RESET  R     PAUSE  ESC", root, .065f, .145f, .39f, .1f, 18, Muted);
        Text("8 CORES  /  3 SECTORS  /  1 REACTOR", root, .57f, .085f, .36f, .06f, 21, Cyan, TextAnchor.MiddleRight);
        Text("Collect every cyan core. Boost across the gaps.\nAvoid the red scanners; checkpoints keep your progress.", root, .54f, .015f, .39f, .075f, 17, Muted, TextAnchor.MiddleRight);
        Text("UNITY 101  /  COREBOUND", root, .065f, .035f, .34f, .045f, 15, Muted);
        Button("QUIT", root, .88f, .9f, .095f, .065f, false, menu.Quit);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/Scenes/MainMenu.unity");
    }
}
