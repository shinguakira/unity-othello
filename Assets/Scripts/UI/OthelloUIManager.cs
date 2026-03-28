using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OthelloUIManager : MonoBehaviour
{
    public static OthelloUIManager Instance { get; private set; }

    // Panels
    GameObject _modeSelectPanel;
    GameObject _gamePanel;
    GameObject _gameOverPanel;
    GameObject _passToastPanel;

    // Top-bar navigation (shown only during gameplay)
    GameObject _homeBtnGO;

    // Game UI
    Text _blackScoreText;
    Text _whiteScoreText;
    Text _turnIndicatorText;

    // Game over UI
    Text _winnerText;
    Text _gameOverScoreText;

    // Pass toast
    Text _passToastText;
    Coroutine _passToastCoroutine;

    // Localization refresh
    readonly List<(Text t, System.Func<string> getter)> _localizedTexts =
        new List<(Text, System.Func<string>)>();
    int _lastTurnPlayer = 1;
    int _lastWinner = -1;

    Font _font;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
    }

    void Start()
    {
        StartCoroutine(InitUI());
    }

    IEnumerator InitUI()
    {
        CreateUI();
        yield return null; // frame 1: StandaloneInputModule runs first Update()
        yield return null; // frame 2: pointer state fully settled
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        ShowModeSelect();
        Canvas.ForceUpdateCanvases();
    }

    void OnEnable()
    {
        EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Subscribe<PassTurnEvent>(OnPassTurn);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Unsubscribe<PassTurnEvent>(OnPassTurn);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
    }

    // ── UI Construction ─────────────────────────────────────────────────────

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    void CreateUI()
    {
        var canvasGO = new GameObject("OthelloCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var safePanel = MakePanel(canvasGO, "SafeArea");
        ApplySafeArea(safePanel.GetComponent<RectTransform>());

        BuildTopBar(safePanel);
        _modeSelectPanel = BuildModeSelectPanel(safePanel);
        _gameOverPanel   = BuildGameOverPanel(safePanel);
        _passToastPanel  = BuildPassToast(safePanel);

        _gamePanel = MakePanel(safePanel, "GamePanel");
        SetStretch(_gamePanel.GetComponent<RectTransform>());
        _gamePanel.SetActive(false);

    }

    // Top bar: [HOME btn | ● score | turn | ○ score | LANG btn]
    // Height 160px. HOME and LANG buttons are always present but HOME is
    // hidden until a game starts — it is the primary navigation affordance.
    void BuildTopBar(GameObject parent)
    {
        var bar = MakePanel(parent, "TopBar");
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -160f);
        rt.offsetMax = Vector2.zero;

        var bg = bar.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        bg.raycastTarget = false;

        // ── HOME button (left 20%) — orange, very visible, hidden until in-game
        _homeBtnGO = MakeButton(bar, "title_btn",
            new Vector2(0.005f, 0.08f), new Vector2(0.195f, 0.92f),
            new Color(0.85f, 0.35f, 0.1f), OnTitleButtonClicked);
        _homeBtnGO.GetComponentInChildren<Text>().fontSize = 30;
        _localizedTexts.Add((_homeBtnGO.GetComponentInChildren<Text>(), () => Loc.Get("title_btn")));
        _homeBtnGO.SetActive(false);

        // ── Black score (20–43%)
        var blackGO = MakePanel(bar, "BlackScore");
        var brt = blackGO.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.20f, 0f);
        brt.anchorMax = new Vector2(0.43f, 1f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        blackGO.AddComponent<Image>().color = Color.clear;
        _blackScoreText = MakeText(blackGO, "2", 56, Color.white, TextAnchor.MiddleCenter);
        AddColorDot(blackGO, Color.black, new Vector2(0.12f, 0.5f));

        // ── Turn indicator (43–57%)
        var turnGO = MakePanel(bar, "TurnIndicator");
        var trt = turnGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.43f, 0f);
        trt.anchorMax = new Vector2(0.57f, 1f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        _turnIndicatorText = MakeText(turnGO, Loc.Get("black_turn"), 28,
            new Color(0.75f, 0.75f, 0.75f), TextAnchor.MiddleCenter);

        // ── White score (57–80%)
        var whiteGO = MakePanel(bar, "WhiteScore");
        var wrt = whiteGO.GetComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0.57f, 0f);
        wrt.anchorMax = new Vector2(0.80f, 1f);
        wrt.offsetMin = Vector2.zero;
        wrt.offsetMax = Vector2.zero;
        _whiteScoreText = MakeText(whiteGO, "2", 56, new Color(0.9f, 0.9f, 0.9f), TextAnchor.MiddleCenter);
        AddColorDot(whiteGO, Color.white, new Vector2(0.88f, 0.5f));

        // ── Language toggle (right 20%) — always visible
        var langBtn = MakeButton(bar, "lang_btn",
            new Vector2(0.805f, 0.08f), new Vector2(0.995f, 0.92f),
            new Color(0.15f, 0.3f, 0.55f), OnLangToggleClicked);
        langBtn.GetComponentInChildren<Text>().fontSize = 30;
        _localizedTexts.Add((langBtn.GetComponentInChildren<Text>(), () => Loc.Get("lang_btn")));
    }

    void AddColorDot(GameObject parent, Color color, Vector2 anchor)
    {
        var dot = new GameObject("Dot");
        dot.transform.SetParent(parent.transform, false);
        var rt = dot.AddComponent<RectTransform>();
        rt.anchorMin = anchor - new Vector2(0.06f, 0.22f);
        rt.anchorMax = anchor + new Vector2(0.06f, 0.22f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = dot.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    GameObject BuildModeSelectPanel(GameObject parent)
    {
        var panel = MakePanel(parent, "ModeSelectPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.35f, 0.15f, 0.97f);
        bg.raycastTarget = false;

        // Title
        var titleGO = MakePanel(panel, "Title");
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.1f, 0.65f);
        titleRT.anchorMax = new Vector2(0.9f, 0.85f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        var titleText = MakeText(titleGO, Loc.Get("title"), 96, Color.white, TextAnchor.MiddleCenter);
        titleText.fontStyle = FontStyle.Bold;
        _localizedTexts.Add((titleText, () => Loc.Get("title")));

        // Subtitle
        var subGO = MakePanel(panel, "Sub");
        var subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.1f, 0.57f);
        subRT.anchorMax = new Vector2(0.9f, 0.65f);
        subRT.offsetMin = Vector2.zero;
        subRT.offsetMax = Vector2.zero;
        var subText = MakeText(subGO, Loc.Get("select_mode"), 42,
            new Color(0.8f, 0.9f, 0.8f), TextAnchor.MiddleCenter);
        _localizedTexts.Add((subText, () => Loc.Get("select_mode")));

        // vs AI
        var vsAIBtn = MakeButton(panel, "vs_ai",
            new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.52f),
            new Color(0.2f, 0.6f, 0.3f), () =>
            {
                _modeSelectPanel.SetActive(false);
                _gamePanel.SetActive(true);
                _homeBtnGO.SetActive(true);
                EventBus.Publish(new GameModeSelectedEvent { vsAI = true });
            });
        _localizedTexts.Add((vsAIBtn.GetComponentInChildren<Text>(), () => Loc.Get("vs_ai")));

        // vs Human
        var vsHumanBtn = MakeButton(panel, "vs_human",
            new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.36f),
            new Color(0.25f, 0.45f, 0.65f), () =>
            {
                _modeSelectPanel.SetActive(false);
                _gamePanel.SetActive(true);
                _homeBtnGO.SetActive(true);
                EventBus.Publish(new GameModeSelectedEvent { vsAI = false });
            });
        _localizedTexts.Add((vsHumanBtn.GetComponentInChildren<Text>(), () => Loc.Get("vs_human")));

        // Records
        var recBtn = MakeButton(panel, "records",
            new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.18f),
            new Color(0.4f, 0.4f, 0.4f), ShowRecords);
        _localizedTexts.Add((recBtn.GetComponentInChildren<Text>(), () => Loc.Get("records")));

        // Language toggle (top-right corner)
        var langBtn = MakeButton(panel, "lang_mode",
            new Vector2(0.70f, 0.91f), new Vector2(0.99f, 0.99f),
            new Color(0.15f, 0.3f, 0.55f), OnLangToggleClicked);
        langBtn.GetComponentInChildren<Text>().fontSize = 32;
        _localizedTexts.Add((langBtn.GetComponentInChildren<Text>(), () => Loc.Get("lang_btn")));

        return panel;
    }

    GameObject BuildGameOverPanel(GameObject parent)
    {
        var panel = MakePanel(parent, "GameOverPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        var dimBg = panel.AddComponent<Image>();
        dimBg.color = new Color(0f, 0f, 0f, 0.82f);
        dimBg.raycastTarget = false;

        var cardGO = MakePanel(panel, "Card");
        var cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.08f, 0.3f);
        cardRT.anchorMax = new Vector2(0.92f, 0.72f);
        cardRT.offsetMin = Vector2.zero;
        cardRT.offsetMax = Vector2.zero;
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(0.12f, 0.35f, 0.15f, 1f);
        cardImg.raycastTarget = false;

        var winnerGO = MakePanel(cardGO, "Winner");
        winnerGO.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.62f);
        winnerGO.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
        winnerGO.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        winnerGO.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        _winnerText = MakeText(winnerGO, "", 72, Color.white, TextAnchor.MiddleCenter);
        _winnerText.fontStyle = FontStyle.Bold;

        var scoreGO = MakePanel(cardGO, "Score");
        scoreGO.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.38f);
        scoreGO.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.62f);
        scoreGO.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        scoreGO.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        _gameOverScoreText = MakeText(scoreGO, "", 52, new Color(0.9f, 0.9f, 0.9f), TextAnchor.MiddleCenter);

        var replayBtn = MakeButton(cardGO, "play_again",
            new Vector2(0.05f, 0.05f), new Vector2(0.48f, 0.33f),
            new Color(0.2f, 0.6f, 0.3f), OnReplayClicked);
        _localizedTexts.Add((replayBtn.GetComponentInChildren<Text>(), () => Loc.Get("play_again")));

        var menuBtn = MakeButton(cardGO, "menu",
            new Vector2(0.52f, 0.05f), new Vector2(0.95f, 0.33f),
            new Color(0.4f, 0.4f, 0.4f), OnMainMenuClicked);
        _localizedTexts.Add((menuBtn.GetComponentInChildren<Text>(), () => Loc.Get("menu")));

        panel.SetActive(false);
        return panel;
    }

    GameObject BuildPassToast(GameObject parent)
    {
        var panel = MakePanel(parent, "PassToast");
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.42f);
        rt.anchorMax = new Vector2(0.9f, 0.52f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var toastBg = panel.AddComponent<Image>();
        toastBg.color = new Color(0f, 0f, 0f, 0.75f);
        toastBg.raycastTarget = false;
        _passToastText = MakeText(panel, "", 44, Color.white, TextAnchor.MiddleCenter);

        panel.SetActive(false);
        return panel;
    }

    // ── Event Handlers ───────────────────────────────────────────────────────

    void OnTurnChanged(TurnChangedEvent e)
    {
        _lastTurnPlayer = e.playerColor;
        _blackScoreText.text = e.blackCount.ToString();
        _whiteScoreText.text = e.whiteCount.ToString();
        _turnIndicatorText.text = e.playerColor == 1 ? Loc.Get("black_turn") : Loc.Get("white_turn");
    }

    void OnPassTurn(PassTurnEvent e)
    {
        string key = e.playerColor == 1 ? "black_passes" : "white_passes";
        if (_passToastCoroutine != null) StopCoroutine(_passToastCoroutine);
        _passToastCoroutine = StartCoroutine(ShowPassToast(Loc.Get(key)));
    }

    void OnGameOver(GameOverEvent e)
    {
        _lastWinner = e.winner;
        _gameOverPanel.SetActive(true);
        _homeBtnGO.SetActive(false);

        string result = e.winner == 0 ? Loc.Get("draw")
                      : e.winner == 1 ? Loc.Get("black_wins")
                      :                 Loc.Get("white_wins");
        _winnerText.text = result;
        _gameOverScoreText.text = $"● {e.blackCount}   ○ {e.whiteCount}";
    }

    IEnumerator ShowPassToast(string message)
    {
        _passToastText.text = message;
        _passToastPanel.SetActive(true);
        yield return new WaitForSeconds(1.2f);
        _passToastPanel.SetActive(false);
    }

    void OnReplayClicked()
    {
        _gameOverPanel.SetActive(false);
        _gamePanel.SetActive(false);
        _homeBtnGO.SetActive(false);
        ShowModeSelect();
    }

    void OnMainMenuClicked()
    {
        _gameOverPanel.SetActive(false);
        _gamePanel.SetActive(false);
        _homeBtnGO.SetActive(false);
        ShowModeSelect();
    }

    void OnTitleButtonClicked()
    {
        _gamePanel.SetActive(false);
        _homeBtnGO.SetActive(false);
        ShowModeSelect();
    }

    void OnLangToggleClicked()
    {
        Loc.Toggle();
        RefreshLocalization();
    }

    void RefreshLocalization()
    {
        foreach (var (t, getter) in _localizedTexts)
            if (t != null) t.text = getter();

        _turnIndicatorText.text = _lastTurnPlayer == 1
            ? Loc.Get("black_turn") : Loc.Get("white_turn");

        if (_lastWinner >= 0)
        {
            _winnerText.text = _lastWinner == 0 ? Loc.Get("draw")
                             : _lastWinner == 1 ? Loc.Get("black_wins")
                             :                    Loc.Get("white_wins");
        }
    }

    void ShowModeSelect()
    {
        _modeSelectPanel.SetActive(true);
    }

    void ShowRecords()
    {
        var overlay = MakePanel(_modeSelectPanel, "RecordsOverlay");
        SetStretch(overlay.GetComponent<RectTransform>());
        var overlayBg = overlay.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.9f);
        overlayBg.raycastTarget = false;

        var textGO = MakePanel(overlay, "StatsText");
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.05f, 0.3f);
        textRT.anchorMax = new Vector2(0.95f, 0.75f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        string stats = $"{Loc.Get("stat_games")}{OthelloSaveSystem.GetTotalGames()}\n" +
                       $"{Loc.Get("stat_black")}{OthelloSaveSystem.GetBlackWins()}\n" +
                       $"{Loc.Get("stat_white")}{OthelloSaveSystem.GetWhiteWins()}\n" +
                       $"{Loc.Get("stat_high")}{OthelloSaveSystem.GetHighScore()}";
        MakeText(textGO, stats, 48, Color.white, TextAnchor.MiddleCenter);

        MakeButton(overlay, "back",
            new Vector2(0.3f, 0.15f), new Vector2(0.7f, 0.27f),
            new Color(0.4f, 0.4f, 0.4f), () => Destroy(overlay));
    }

    // ── UI Helpers ───────────────────────────────────────────────────────────

    void ApplySafeArea(RectTransform rt)
    {
        Rect sa = Screen.safeArea;
        rt.anchorMin = new Vector2(sa.x / Screen.width,          sa.y / Screen.height);
        rt.anchorMax = new Vector2((sa.x + sa.width) / Screen.width, (sa.y + sa.height) / Screen.height);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

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
        SetStretch(rt);

        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = _font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.resizeTextForBestFit = false;
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

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = btn.colors;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        MakeText(go, Loc.Get(key), 48, Color.white, TextAnchor.MiddleCenter);
        return go;
    }
}
