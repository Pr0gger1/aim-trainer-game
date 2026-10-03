using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string GroundMaterialPath = "Assets/Materials/Ground.mat";
    private const string GangModelPath = "Assets/Gang members - characters pack/Prefabs/Man_1.prefab";

    private const string AccentHex = "#FF8A2A";
    private const string AccentDimHex = "#B05A14";
    private const string TextMainHex = "#F5F7FB";
    private const string TextDimHex = "#8A93A6";

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private static TMP_FontAsset _font;
    private static Material _ground;
    private static GameObject _gangModel;

    [MenuItem("Tools/Rebuild Game Scene")]
    public static void Build()
    {
        LoadAssets();

        NewEmptyScene();

        ComposeWorld();
        ComposePlayer();
        ComposePauseMenu();
        ComposeEventSystem();

        SaveAndReport();
    }

    // ─── Assets ────────────────────────────────────────────

    private static void LoadAssets()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        _ground = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
        _gangModel = AssetDatabase.LoadAssetAtPath<GameObject>(GangModelPath);

        if (_font == null)
            Debug.LogError("Font not found: " + FontPath);
        if (_gangModel == null)
            Debug.LogError("Gang model not found: " + GangModelPath);
    }

    // ─── Scene plumbing ────────────────────────────────────

    private static void NewEmptyScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void SaveAndReport()
    {
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        Debug.Log("Game scene rebuilt: " + ScenePath);
    }

    // ─── World ─────────────────────────────────────────────

    private static void ComposeWorld()
    {
        PlaceGround();
        PlaceObstacles();
        PlaceSunlight();
    }

    private static void PlaceGround()
    {
        var ground = Cube("Ground", new Vector3(0f, -0.4f, 0f), new Vector3(50f, 0.8f, 50f));
        ground.GetComponent<MeshRenderer>().sharedMaterial = _ground;
    }

    private static void PlaceObstacles()
    {
        Cube("Block_A", new Vector3(3f, 0.5f, -2f), new Vector3(1f, 1f, 1f));
        Cube("Block_B", new Vector3(-4f, 0.75f, -1f), new Vector3(1f, 1.5f, 1f));
        Cube("Block_C", new Vector3(0f, 1.25f, -8f), new Vector3(1.5f, 2.5f, 1.5f));
    }

    private static void PlaceSunlight()
    {
        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95686275f, 0.8392157f);
        light.intensity = 1f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.gameObject.AddComponent<UniversalAdditionalLightData>();
    }

    // ─── Player ────────────────────────────────────────────

    private static void ComposePlayer()
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.1f, 0f);

        var body = AddMovementBody(player);
        var head = AddHeadView(player);
        AddGangModelBody(player);

        var controller = player.AddComponent<FirstPersonController>();
        controller.controller = body;
        controller.playerCamera = head.transform;
    }

    private static CharacterController AddMovementBody(GameObject player)
    {
        var body = player.AddComponent<CharacterController>();
        body.height = 2f;
        body.radius = 0.35f;
        body.center = new Vector3(0f, 1f, 0f);
        body.stepOffset = 0.3f;
        body.skinWidth = 0.08f;
        body.slopeLimit = 45f;
        return body;
    }

    private static GameObject AddHeadView(GameObject player)
    {
        var head = new GameObject("PlayerCamera");
        head.transform.SetParent(player.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        head.tag = "MainCamera";

        var camera = head.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("#0A0E16");
        camera.fieldOfView = 75f;
        camera.nearClipPlane = 0.05f;

        head.AddComponent<AudioListener>();
        head.AddComponent<UniversalAdditionalCameraData>();
        return head;
    }

    private static void AddGangModelBody(GameObject player)
    {
        if (_gangModel == null)
        {
            Debug.LogWarning("Skipping gang model: asset not loaded");
            return;
        }

        var model = (GameObject)PrefabUtility.InstantiatePrefab(_gangModel);
        model.transform.SetParent(player.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        AddWalkAnimation(model);

        AlignFeetToController(model, player.transform);
        HideHeadParts(model);
    }

    private static void AddWalkAnimation(GameObject model)
    {
        var animator = model.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        model.AddComponent<Player.ProceduralWalk>();
    }

    private static void AlignFeetToController(GameObject model, Transform player)
    {
        var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var feetWorldY = bounds.min.y;
        model.transform.position += Vector3.up * (player.position.y - feetWorldY);
    }

    private static void HideHeadParts(GameObject model)
    {
        foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            string name = renderer.gameObject.name;
            if (name == "eye_l" || name == "eye_r" || name == "jaw_1" || name == "jaw_2" ||
                name == "tongue" || name == "glasses")
            {
                renderer.enabled = false;
            }
        }
    }

    // ─── Pause menu ────────────────────────────────────────

    private static void ComposePauseMenu()
    {
        var canvas = CreatePauseCanvas();

        AddDimOverlay(canvas);
        AddPausePanel(canvas);
        WirePauseController(canvas);
    }

    private static Canvas CreatePauseCanvas()
    {
        var go = new GameObject("PauseCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.SetActive(false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return canvas;
    }

    private static void AddDimOverlay(Canvas canvas)
    {
        var dim = NewImage((RectTransform)canvas.transform, "Background",
            new Color(0.04f, 0.05f, 0.09f, 0.72f));
        dim.StretchFull();
        dim.raycastTarget = true;
    }

    private static void AddPausePanel(Canvas canvas)
    {
        var rect = (RectTransform)canvas.transform;

        var panel = NewImage(rect, "Panel", PanelColor());
        panel.rectTransform.sizeDelta = new Vector2(580f, 520f);
        panel.raycastTarget = true;

        Label(panel.rectTransform, "Title", "\u041F\u0410\u0423\u0417\u0410", 64, TextMainHex, FontStyles.Bold)
            .PlaceAt(new Vector2(0f, 160f), new Vector2(500f, 90f));
        Label(panel.rectTransform, "Subtitle", "\u0418\u0433\u0440\u0430 \u043F\u0440\u0438\u043E\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D\u0430",
                26, TextDimHex, FontStyles.Normal)
            .PlaceAt(new Vector2(0f, 118f), new Vector2(500f, 40f));
        AccentRule(panel.rectTransform, new Vector2(0f, 84f));
    }

    private static void WirePauseController(Canvas canvas)
    {
        var pause = EnsurePauseController(canvas);
        var panel = (RectTransform)canvas.transform.Find("Panel");

        var resume = AccentButton(panel, "ResumeButton", "\u041F\u0420\u041E\u0414\u041E\u041B\u0416\u0418\u0422\u042C",
            new Vector2(0f, -20f));
        Divider(panel, new Vector2(0f, 70f));
        var quit = QuietButton(panel, "QuitMenuButton", "\u0412\u042B\u0419\u0422\u0418 \u0412 \u041C\u0415\u041D\u042E",
            new Vector2(0f, -120f));

        UnityEventTools.AddPersistentListener(resume.onClick, pause.Continue);
        UnityEventTools.AddPersistentListener(quit.onClick, pause.ExitMenu);

        Label(panel, "Version", "AIM TRAINER  \u00B7  v0.1.0", 18, TextDimHex, FontStyles.Normal)
            .PlaceAt(new Vector2(0f, -220f), new Vector2(500f, 30f));

        var serialized = new SerializedObject(pause);
        serialized.FindProperty("firstSelectedButton").objectReferenceValue = resume.gameObject;
        serialized.ApplyModifiedProperties();
    }

    private static UI.PauseModeController EnsurePauseController(Canvas canvas)
    {
        var player = GameObject.Find("Player");
        var pause = player.GetComponent<UI.PauseModeController>();
        if (pause == null)
            pause = player.AddComponent<UI.PauseModeController>();

        var serialized = new SerializedObject(pause);
        serialized.FindProperty("pauseMenu").objectReferenceValue = canvas.gameObject;
        serialized.ApplyModifiedProperties();
        return pause;
    }

    // ─── Event system ──────────────────────────────────────

    private static void ComposeEventSystem()
    {
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    // ─── Element factories ─────────────────────────────────

    private static GameObject Cube(string name, Vector3 position, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        return go;
    }

    private static Image NewImage(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Center;
        rt.anchorMax = Center;
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI Label(RectTransform parent, string name, string text,
        int size, string hex, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Center;
        rt.anchorMax = Center;

        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = Hex(hex);
        label.alignment = TextAlignmentOptions.Center;
        label.text = text;
        return label;
    }

    private static void AccentRule(RectTransform parent, Vector2 position)
    {
        Line(parent, "AccentLine", new Vector2(340f, 3f), new Color(1f, 0.54f, 0.16f, 0.9f)).PlaceAt(position);
        Dot(parent, "AccentDot", new Vector2(11f, 11f), Hex(TextMainHex)).PlaceAt(position);
    }

    private static void Divider(RectTransform parent, Vector2 position)
    {
        Line(parent, "QuitSub", new Vector2(340f, 1f), new Color(1f, 0.54f, 0.16f, 0.25f)).PlaceAt(position);
    }

    private static Image Line(RectTransform parent, string name, Vector2 size, Color color)
    {
        var line = NewImage(parent, name, color);
        line.rectTransform.sizeDelta = size;
        line.raycastTarget = false;
        return line;
    }

    private static Image Dot(RectTransform parent, string name, Vector2 size, Color color)
    {
        var dot = NewImage(parent, name, color);
        dot.rectTransform.sizeDelta = size;
        dot.raycastTarget = false;
        return dot;
    }

    private static Button AccentButton(RectTransform parent, string name, string label, Vector2 position)
    {
        return ButtonWith(parent, name, label, 30, AccentPalette, position);
    }

    private static Button QuietButton(RectTransform parent, string name, string label, Vector2 position)
    {
        return ButtonWith(parent, name, label, 28, QuietPalette, position);
    }

    private static Button ButtonWith(RectTransform parent, string name, string label, int fontSize,
        Palette palette, Vector2 position)
    {
        var image = NewImage(parent, name, palette.Normal);
        image.rectTransform.sizeDelta = new Vector2(340f, 72f);
        image.rectTransform.anchoredPosition = position;

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = palette.Normal;
        colors.highlightedColor = palette.Highlighted;
        colors.pressedColor = palette.Pressed;
        colors.selectedColor = palette.Normal;
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        Label(image.rectTransform, "Label", label, fontSize, TextMainHex, FontStyles.Bold).FillParent();

        return button;
    }

    // ─── Fluent placement helpers ──────────────────────────

    private static Image PlaceAt(this Image image, Vector2 position)
    {
        image.rectTransform.anchoredPosition = position;
        return image;
    }

    private static Image PlaceAt(this Image image, Vector2 position, Vector2 size)
    {
        var rt = image.rectTransform;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return image;
    }

    private static TextMeshProUGUI PlaceAt(this TextMeshProUGUI label, Vector2 position, Vector2 size)
    {
        var rt = label.rectTransform;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return label;
    }

    private static void FillParent(this TextMeshProUGUI label)
    {
        var rt = label.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void StretchFull(this Image image)
    {
        var rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ─── Colors ────────────────────────────────────────────

    private static Color PanelColor()
    {
        return Hex("#121826");
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var color);
        return color;
    }

    // ─── Button palettes ───────────────────────────────────

    private class Palette
    {
        public readonly Color Normal;
        public readonly Color Highlighted;
        public readonly Color Pressed;

        public Palette(Color normal, Color highlighted, Color pressed)
        {
            Normal = normal;
            Highlighted = highlighted;
            Pressed = pressed;
        }
    }

    private static readonly Palette AccentPalette = new Palette(
        Hex(AccentHex),
        new Color(1f, 0.68f, 0.35f, 1f),
        Hex(AccentDimHex));

    private static readonly Palette QuietPalette = new Palette(
        new Color(0.09f, 0.12f, 0.17f, 1f),
        new Color(0.17f, 0.2f, 0.28f, 1f),
        Hex(AccentDimHex));
}