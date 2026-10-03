using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string MusicPath = "Assets/Audio/Music/MenuTheme.wav";

    private const string AccentHex = "#FF8A2A";
    private const string AccentDimHex = "#B05A14";
    private const string TextMainHex = "#F5F7FB";
    private const string TextDimHex = "#8A93A6";

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private static TMP_FontAsset _font;

    [MenuItem("Tools/Rebuild MainMenu Scene")]
    public static void Build()
    {
        LoadAssets();

        NewEmptyScene();

        ComposeCamera();
        ComposeEventSystem();
        ComposeCanvas();
        ComposeMusic();

        SaveAndReport();
    }

    // ─── Assets ────────────────────────────────────────────

    private static void LoadAssets()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (_font == null)
            Debug.LogError("Font not found: " + FontPath);
    }

    // ─── Scene plumbing ────────────────────────────────────

    private static void NewEmptyScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static void SaveAndReport()
    {
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        Debug.Log("MainMenu scene rebuilt: " + ScenePath);
    }

    // ─── Camera ────────────────────────────────────────────

    private static void ComposeCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";

        var camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("#0A0E16");
        camera.transform.position = new Vector3(0f, 0f, -10f);

        go.AddComponent<AudioListener>();
    }

    // ─── Event system ──────────────────────────────────────

    private static void ComposeEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    // ─── Canvas ────────────────────────────────────────────

    private static void ComposeCanvas()
    {
        var canvas = CreateCanvas();
        var root = (RectTransform)canvas.transform;

        Backdrop(root);
        Crosshair(root);
        Title(root);
        Buttons(root, canvas);
        MenuAnimator(canvas);
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return canvas;
    }

    private static void Backdrop(RectTransform root)
    {
        var panel = NewImage(root, "Backdrop", Hex("#121826"));
        panel.StretchFull();
        panel.raycastTarget = false;

        var glow = NewImage(panel.rectTransform, "Glow", new Color(1f, 0.54f, 0.16f, 0.06f));
        glow.rectTransform.sizeDelta = new Vector2(1500f, 1500f);
        glow.rectTransform.anchoredPosition = new Vector2(0f, 140f);
        glow.raycastTarget = false;
    }

    private static void Crosshair(RectTransform root)
    {
        var holder = NewImage(root, "Crosshair", new Color(1f, 1f, 1f, 0f));
        holder.rectTransform.sizeDelta = new Vector2(560f, 560f);
        holder.rectTransform.anchoredPosition = new Vector2(0f, 40f);
        holder.raycastTarget = false;

        Line(holder.rectTransform, "LineH", new Vector2(560f, 3f), new Color(1f, 0.6f, 0.2f, 0.55f));
        Line(holder.rectTransform, "LineV", new Vector2(3f, 560f), new Color(1f, 0.6f, 0.2f, 0.55f));

        Dot(holder.rectTransform, "TopL", new Vector2(-120f, 120f), new Color(1f, 0.78f, 0.45f, 0.9f));
        Dot(holder.rectTransform, "TopR", new Vector2(120f, 120f), new Color(1f, 0.78f, 0.45f, 0.9f));
        Dot(holder.rectTransform, "BotL", new Vector2(-120f, -120f), new Color(1f, 0.78f, 0.45f, 0.9f));
        Dot(holder.rectTransform, "BotR", new Vector2(120f, -120f), new Color(1f, 0.78f, 0.45f, 0.9f));
    }

    private static void Title(RectTransform root)
    {
        Label(root, "Title", "<color=#FF8A2A>AIM</color> TRAINER", 108, TextMainHex, FontStyles.Bold)
            .PlaceAt(new Vector2(0f, 250f), new Vector2(1000f, 160f));
        Label(root, "Subtitle", "\u0422\u0440\u0435\u043D\u0438\u0440\u0443\u0439 \u0442\u043E\u0447\u043D\u043E\u0441\u0442\u044C \u0438 \u0440\u0435\u0430\u043A\u0446\u0438\u044E",
                28, TextDimHex, FontStyles.Normal)
            .PlaceAt(new Vector2(0f, 176f), new Vector2(900f, 40f));

        AccentRule(root, new Vector2(0f, 130f));
    }

    private static void Buttons(RectTransform root, Canvas canvas)
    {
        var controller = CreateMenuController(canvas);

        var play = AccentButton(root, "PlayButton", "\u0418\u0413\u0420\u0410\u0422\u042C",
            new Vector2(0f, -40f), 30);
        UnityEventTools.AddPersistentListener(play.onClick, controller.Play);

        Divider(root, new Vector2(0f, -108f));
        VersionFooter(root);

        var quit = QuietButton(root, "QuitButton", "\u0412\u042B\u0419\u0422\u0418",
            new Vector2(0f, -140f), 28);
        UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);
    }

    private static void MenuAnimator(Canvas canvas)
    {
        var holder = new GameObject("MenuAnimator");
        holder.transform.SetParent(canvas.transform, false);

        var animator = holder.AddComponent<UI.MenuAnimator>();

        var serialized = new SerializedObject(animator);
        serialized.FindProperty("crosshair").objectReferenceValue = canvas.transform.Find("Crosshair");
        serialized.FindProperty("title").objectReferenceValue = canvas.transform.Find("Title");
        serialized.FindProperty("subtitle").objectReferenceValue = canvas.transform.Find("Subtitle");
        serialized.FindProperty("accentLine").objectReferenceValue =
            canvas.transform.Find("AccentLine").GetComponent<Image>();
        serialized.ApplyModifiedProperties();
    }

    private static UI.MainMenuController CreateMenuController(Canvas canvas)
    {
        var go = new GameObject("MainMenuController");
        go.AddComponent<RectTransform>().SetParent(canvas.transform, false);
        return go.AddComponent<UI.MainMenuController>();
    }

    // ─── Music ─────────────────────────────────────────────

    private static void ComposeMusic()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        if (clip == null)
        {
            Debug.LogError("Music clip not found: " + MusicPath);
            return;
        }

        var go = new GameObject("MenuMusic");
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        go.AddComponent<UI.MenuMusicController>();
    }

    // ─── Element factories ─────────────────────────────────

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

    private static Button AccentButton(RectTransform parent, string name, string label, Vector2 position, int fontSize)
    {
        return ButtonWith(parent, name, label, fontSize, AccentPalette, position);
    }

    private static Button QuietButton(RectTransform parent, string name, string label, Vector2 position, int fontSize)
    {
        return ButtonWith(parent, name, label, fontSize, QuietPalette, position);
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

    private static void AccentRule(RectTransform parent, Vector2 position)
    {
        Line(parent, "AccentLine", new Vector2(420f, 3f), new Color(1f, 0.54f, 0.16f, 0.9f)).PlaceAt(position);
        Dot(parent, "AccentDot", new Vector2(11f, 11f), Hex(TextMainHex)).PlaceAt(position);
    }

    private static void Divider(RectTransform parent, Vector2 position)
    {
        Line(parent, "QuitUnderline", new Vector2(340f, 1f), new Color(1f, 0.54f, 0.16f, 0.25f)).PlaceAt(position);
    }

    private static void VersionFooter(RectTransform parent)
    {
        var version = Label(parent, "Version", "AIM TRAINER  \u00B7  v0.1.0", 20, TextDimHex, FontStyles.Normal);
        var rt = version.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 28f);
        version.alignment = TextAlignmentOptions.Bottom;
    }

    private static Image Line(RectTransform parent, string name, Vector2 size, Color color)
    {
        var line = NewImage(parent, name, color);
        line.rectTransform.sizeDelta = size;
        line.raycastTarget = false;
        return line;
    }

    private static Image Dot(RectTransform parent, string name, Vector2 position, Color color)
    {
        var dot = NewImage(parent, name, color);
        dot.rectTransform.sizeDelta = new Vector2(2f, 2f);
        dot.rectTransform.anchoredPosition = position;
        dot.raycastTarget = false;
        return dot;
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