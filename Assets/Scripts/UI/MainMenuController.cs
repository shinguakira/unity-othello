using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    Font _font;

    void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
        CreateUI();
    }

    void CreateUI()
    {
        var canvasGO = new GameObject("MainMenuCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var menuBg = canvasGO.AddComponent<Image>();
        menuBg.color = new Color(0.12f, 0.35f, 0.15f);
        menuBg.raycastTarget = false;

        // Title
        var titleGO = MakePanel(canvasGO, "Title");
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.05f, 0.7f);
        titleRT.anchorMax = new Vector2(0.95f, 0.9f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        var titleText = MakeText(titleGO, Loc.Get("title"), 108, Color.white, TextAnchor.MiddleCenter);
        titleText.fontStyle = FontStyle.Bold;

        // Subtitle
        var subGO = MakePanel(canvasGO, "Subtitle");
        var subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.1f, 0.62f);
        subRT.anchorMax = new Vector2(0.9f, 0.72f);
        subRT.offsetMin = Vector2.zero;
        subRT.offsetMax = Vector2.zero;
        MakeText(subGO, Loc.Get("subtitle"), 44, new Color(0.75f, 0.9f, 0.75f), TextAnchor.MiddleCenter);

        // vs AI button
        MakeButton(canvasGO, "vs_ai", new Vector2(0.15f, 0.45f), new Vector2(0.85f, 0.58f),
            new Color(0.2f, 0.6f, 0.3f), OnVsAIClicked);

        // vs Human button
        MakeButton(canvasGO, "vs_human", new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.43f),
            new Color(0.25f, 0.45f, 0.65f), OnVsHumanClicked);

        // Records button
        MakeButton(canvasGO, "records", new Vector2(0.25f, 0.16f), new Vector2(0.75f, 0.27f),
            new Color(0.35f, 0.35f, 0.35f), OnRecordsClicked);

        // Language toggle (top-right corner)
        MakeButton(canvasGO, "lang_btn", new Vector2(0.72f, 0.92f), new Vector2(0.98f, 0.99f),
            new Color(0.2f, 0.35f, 0.55f), OnLangToggleClicked);

        // Version label
        var verGO = MakePanel(canvasGO, "Version");
        var verRT = verGO.GetComponent<RectTransform>();
        verRT.anchorMin = new Vector2(0f, 0f);
        verRT.anchorMax = new Vector2(1f, 0.06f);
        verRT.offsetMin = Vector2.zero;
        verRT.offsetMax = Vector2.zero;
        MakeText(verGO, Loc.Get("version"), 32, new Color(0.5f, 0.7f, 0.5f), TextAnchor.MiddleCenter);

        Canvas.ForceUpdateCanvases();
    }

    void OnVsAIClicked()
    {
        PlayerPrefs.SetInt("GameMode", 1);
        if (SceneLoader.Instance != null) SceneLoader.Instance.Load("Game");
    }

    void OnVsHumanClicked()
    {
        PlayerPrefs.SetInt("GameMode", 0);
        if (SceneLoader.Instance != null) SceneLoader.Instance.Load("Game");
    }

    void OnLangToggleClicked()
    {
        Loc.Toggle();
        // Reload the menu scene to reflect the new language
        if (SceneLoader.Instance != null) SceneLoader.Instance.Load("MainMenu");
    }

    void OnRecordsClicked()
    {
        var canvas = GetComponentInChildren<Canvas>();
        var overlay = MakePanel(canvas.gameObject, "RecordsOverlay");

        var rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var overlayBg = overlay.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.92f);
        overlayBg.raycastTarget = false;

        var textGO = MakePanel(overlay, "Stats");
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.05f, 0.35f);
        textRT.anchorMax = new Vector2(0.95f, 0.75f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        string stats = $"{Loc.Get("stat_games")}{OthelloSaveSystem.GetTotalGames()}\n" +
                       $"{Loc.Get("stat_black")}{OthelloSaveSystem.GetBlackWins()}\n" +
                       $"{Loc.Get("stat_white")}{OthelloSaveSystem.GetWhiteWins()}\n" +
                       $"{Loc.Get("stat_high")}{OthelloSaveSystem.GetHighScore()}";

        MakeText(textGO, stats, 52, Color.white, TextAnchor.MiddleCenter);

        MakeButton(overlay, "back", new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.3f),
            new Color(0.4f, 0.4f, 0.4f), () => Destroy(overlay));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static GameObject MakePanel(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    Text MakeText(GameObject parent, string content, int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = _font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

    GameObject MakeButton(GameObject parent, string key,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + key);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.AddComponent<Image>().color = bgColor;

        var btn = go.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);

        MakeText(go, Loc.Get(key), 52, Color.white, TextAnchor.MiddleCenter);
        return go;
    }
}
