using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class OthelloUIManager : MonoBehaviour
{
    public static OthelloUIManager Instance { get; private set; }

    // Panels
    GameObject _modeSelectPanel;
    GameObject _gamePanel;
    GameObject _gameOverPanel;
    GameObject _themePickerPanel;
    GameObject _missionSnackbarPanel;
    Text _missionSnackbarText;

    // Top-bar navigation (shown only during gameplay)
    GameObject _homeBtnGO;
    // Settings (gear) — shown only on title screen, opens theme picker
    GameObject _settingsBtnGO;

    // Game UI
    Text _blackScoreText;
    Text _whiteScoreText;
    Text _turnIndicatorText;

    // Mission panel (shown during gameplay, bottom of screen)
    Text _missionLabelText;   // "Your Mission:"
    Text _missionNameText;    // mission name (localized)
    Text _missionProgressText;// "1/2 → +8pt"

    // Game over UI
    Text _winnerText;
    Text _gameOverBlackText;  // "● 28 + 5 + 8 = 41"
    Text _gameOverWhiteText;  // "○ 24 + 0 + 0 = 24"
    Text _blackMissionReveal; // "● Black Mission: ...\nACHIEVED! +8"
    Text _whiteMissionReveal; // "○ White Mission: ...\nNot achieved"

    // Pass message uses the turn indicator slot, briefly flashed.
    Coroutine _passFlashCoroutine;
    static readonly Color TurnIndicatorNormal = new Color(0.75f, 0.75f, 0.75f);
    static readonly Color TurnIndicatorPass   = new Color(1f, 0.78f, 0.30f);

    // Mission completion celebration: flash the mission bar text + green tint
    Coroutine _missionFlashCoroutine;
    static readonly Color MissionAchieved = new Color(0.42f, 0.86f, 0.42f);
    static readonly Color MissionPending  = new Color(1f, 0.85f, 0.30f);
    bool _blackMissionAchieved;
    bool _whiteMissionAchieved;

    // Localization refresh
    readonly List<(Text t, System.Func<string> getter)> _localizedTexts =
        new List<(Text, System.Func<string>)>();
    int _lastTurnPlayer = 1;
    int _lastWinner = -1;
    GameOverEvent _lastGameOver;
    bool _hasGameOver;
    bool _isVsAI;

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
        yield return null;
        yield return null;
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
        EventBus.Subscribe<GameModeSelectedEvent>(OnGameModeSelectedUI);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Unsubscribe<PassTurnEvent>(OnPassTurn);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        EventBus.Unsubscribe<GameModeSelectedEvent>(OnGameModeSelectedUI);
    }

    // Reset per-game UI state when a new game starts.
    void OnGameModeSelectedUI(GameModeSelectedEvent e)
    {
        _isVsAI = e.vsAI;
        _blackMissionAchieved = false;
        _whiteMissionAchieved = false;
        _hasGameOver = false;
        _missionProgressText.color = MissionPending;
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

        _gamePanel = MakePanel(safePanel, "GamePanel");
        SetStretch(_gamePanel.GetComponent<RectTransform>());
        _gamePanel.SetActive(false);

        BuildMissionPanel(_gamePanel);
        _missionSnackbarPanel = BuildMissionSnackbar(_gamePanel);

        // Settings button — universal, shown only on title screen
        _settingsBtnGO = BuildSettingsButton(safePanel);

        // Theme picker overlay (hidden by default, opened by gear button on
        // any theme's title screen). Built last so it sits on top of all
        // other panels in the canvas hierarchy.
        _themePickerPanel = BuildThemePicker(safePanel);
    }

    // Small unobtrusive settings button on the title screen — opens the
    // settings page (where theme + future toggles live). Same prominence
    // tier as the lang button, NOT a hero CTA.
    GameObject BuildSettingsButton(GameObject parent)
    {
        var go = new GameObject("Btn_settings");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.92f, 0.955f);
        rt.anchorMax = new Vector2(0.98f, 0.99f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // Square chip with thin border, neutral tone so it stays understated
        var img = go.AddComponent<Image>();
        img.color = new Color(0.118f, 0.118f, 0.137f, 0.85f);
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(OpenThemePicker);

        // Inner gold border (1px frame)
        MakeRect(go, "FT", new Color(0.949f, 0.769f, 0.282f, 0.85f), 0f, 0.92f, 1f, 1f);
        MakeRect(go, "FB", new Color(0.949f, 0.769f, 0.282f, 0.85f), 0f, 0f, 1f, 0.08f);
        MakeRect(go, "FL", new Color(0.949f, 0.769f, 0.282f, 0.85f), 0f, 0f, 0.04f, 1f);
        MakeRect(go, "FR", new Color(0.949f, 0.769f, 0.282f, 0.85f), 0.96f, 0f, 1f, 1f);

        var label = MakeText(go, "···",
            44, new Color(0.949f, 0.769f, 0.282f, 1f), TextAnchor.MiddleCenter);
        label.fontStyle = FontStyle.Bold;
        return go;
    }

    // Top bar — two stacked rows so each line breathes:
    //   row 1 (upper): HOME button (alone, taller)
    //   row 2 (lower): [ ● score | turn indicator | ○ score ]
    // Total height 240px. Language toggle lives only on the title screen.
    const float TopBarHeight = 240f;

    void BuildTopBar(GameObject parent)
    {
        var bar = MakePanel(parent, "TopBar");
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -TopBarHeight);
        rt.offsetMax = Vector2.zero;

        var bg = bar.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.97f);
        bg.raycastTarget = false;

        // Hairline divider between the two rows
        var divider = MakePanel(bar, "RowDivider");
        var drt = divider.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.04f, 0.495f);
        drt.anchorMax = new Vector2(0.96f, 0.50f);
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
        divider.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.20f, 1f);

        // ── Upper row: HOME button (Y 0.50 – 0.96)
        _homeBtnGO = MakeButton(bar, "title_btn",
            new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.95f),
            new Color(0.72f, 0.50f, 0.08f), OnTitleButtonClicked);
        _homeBtnGO.GetComponentInChildren<Text>().fontSize = 36;
        _localizedTexts.Add((_homeBtnGO.GetComponentInChildren<Text>(), () => Loc.Get("title_btn")));
        _homeBtnGO.SetActive(false);

        // ── Lower row: scores + turn indicator (Y 0.05 – 0.48)
        const float rowYMin = 0.05f;
        const float rowYMax = 0.48f;

        // Black score (X 0.04 – 0.32)
        var blackGO = MakePanel(bar, "BlackScore");
        var brt = blackGO.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.04f, rowYMin);
        brt.anchorMax = new Vector2(0.32f, rowYMax);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        blackGO.AddComponent<Image>().color = Color.clear;
        _blackScoreText = MakeText(blackGO, "2", 56, Color.white, TextAnchor.MiddleCenter);
        AddColorDot(blackGO, Color.black, new Vector2(0.12f, 0.5f));

        // Turn indicator (X 0.32 – 0.68)
        var turnGO = MakePanel(bar, "TurnIndicator");
        var trt = turnGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.32f, rowYMin);
        trt.anchorMax = new Vector2(0.68f, rowYMax);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        _turnIndicatorText = MakeText(turnGO, Loc.Get("black_turn"), 32,
            new Color(0.75f, 0.75f, 0.75f), TextAnchor.MiddleCenter);

        // White score (X 0.68 – 0.96)
        var whiteGO = MakePanel(bar, "WhiteScore");
        var wrt = whiteGO.GetComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0.68f, rowYMin);
        wrt.anchorMax = new Vector2(0.96f, rowYMax);
        wrt.offsetMin = Vector2.zero;
        wrt.offsetMax = Vector2.zero;
        _whiteScoreText = MakeText(whiteGO, "2", 56, new Color(0.9f, 0.9f, 0.9f), TextAnchor.MiddleCenter);
        AddColorDot(whiteGO, Color.white, new Vector2(0.88f, 0.5f));
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

    // Mission bar — sits just below the TopBar at the top of the play area
    // so the board can occupy the lower half of the screen (thumb reach).
    // Shows the current player's mission + progress; opponent shown as "???"
    void BuildMissionPanel(GameObject parent)
    {
        var panel = MakePanel(parent, "MissionPanel");
        var rt = panel.GetComponent<RectTransform>();
        // TopBar now 240px tall = 1.000 - 240/1920 = 0.875 to 1.000.
        // Mission slots into 0.79 - 0.875 (~84px band right under TopBar).
        rt.anchorMin = new Vector2(0f, 0.790f);
        rt.anchorMax = new Vector2(1f, 0.875f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.80f);
        bg.raycastTarget = false;

        // Label: "Your Mission:" — left 30%
        var labelGO = MakePanel(panel, "MissionLabel");
        labelGO.GetComponent<RectTransform>().anchorMin = new Vector2(0.02f, 0f);
        labelGO.GetComponent<RectTransform>().anchorMax = new Vector2(0.30f, 1f);
        labelGO.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        labelGO.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        _missionLabelText = MakeText(labelGO, Loc.Get("your_mission"),
            26, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleLeft);
        _localizedTexts.Add((_missionLabelText, () => Loc.Get("your_mission")));

        // Mission name — center 42%
        var nameGO = MakePanel(panel, "MissionName");
        nameGO.GetComponent<RectTransform>().anchorMin = new Vector2(0.30f, 0f);
        nameGO.GetComponent<RectTransform>().anchorMax = new Vector2(0.72f, 1f);
        nameGO.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        nameGO.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        _missionNameText = MakeText(nameGO, "---", 28, Color.white, TextAnchor.MiddleCenter, true);

        // Progress + bonus — right 28%
        var progGO = MakePanel(panel, "MissionProgress");
        progGO.GetComponent<RectTransform>().anchorMin = new Vector2(0.72f, 0f);
        progGO.GetComponent<RectTransform>().anchorMax = new Vector2(1.00f, 1f);
        progGO.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        progGO.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        _missionProgressText = MakeText(progGO, "", 26,
            new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
    }

    GameObject BuildModeSelectPanel(GameObject parent)
    {
        return OthelloTheme.Active switch
        {
            ThemeKind.Wabi   => BuildModeSelectPanel_Wabi(parent),
            ThemeKind.Neon   => BuildModeSelectPanel_Neon(parent),
            ThemeKind.Pieces => BuildModeSelectPanel_Pieces(parent),
            _                => BuildModeSelectPanel_Riso(parent),
        };
    }

    GameObject BuildModeSelectPanel_Modern(GameObject parent)
    {
        var panel = MakePanel(parent, "ModeSelectPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.16f, 0.08f, 0.97f);
        bg.raycastTarget = false;

        var titleGO = MakePanel(panel, "Title");
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.1f, 0.65f);
        titleRT.anchorMax = new Vector2(0.9f, 0.85f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        var titleText = MakeText(titleGO, Loc.Get("title"), 96, Color.white, TextAnchor.MiddleCenter, true);
        titleText.fontStyle = FontStyle.Bold;
        _localizedTexts.Add((titleText, () => Loc.Get("title")));

        var subGO = MakePanel(panel, "Sub");
        var subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.1f, 0.57f);
        subRT.anchorMax = new Vector2(0.9f, 0.65f);
        subRT.offsetMin = Vector2.zero;
        subRT.offsetMax = Vector2.zero;
        var subText = MakeText(subGO, Loc.Get("select_mode"), 42,
            new Color(0.8f, 0.9f, 0.8f), TextAnchor.MiddleCenter, true);
        _localizedTexts.Add((subText, () => Loc.Get("select_mode")));

        var vsAIBtn = MakeButton(panel, "vs_ai",
            new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.52f),
            new Color(0.16f, 0.60f, 0.26f), () =>
            {
                _modeSelectPanel.SetActive(false);
                _gamePanel.SetActive(true);
                _homeBtnGO.SetActive(true);
                EventBus.Publish(new GameModeSelectedEvent { vsAI = true });
            });
        _localizedTexts.Add((vsAIBtn.GetComponentInChildren<Text>(), () => Loc.Get("vs_ai")));

        var vsHumanBtn = MakeButton(panel, "vs_human",
            new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.36f),
            new Color(0.20f, 0.42f, 0.68f), () =>
            {
                _modeSelectPanel.SetActive(false);
                _gamePanel.SetActive(true);
                _homeBtnGO.SetActive(true);
                EventBus.Publish(new GameModeSelectedEvent { vsAI = false });
            });
        _localizedTexts.Add((vsHumanBtn.GetComponentInChildren<Text>(), () => Loc.Get("vs_human")));

        var recBtn = MakeButton(panel, "records",
            new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.18f),
            new Color(0.28f, 0.28f, 0.30f), ShowRecords);
        _localizedTexts.Add((recBtn.GetComponentInChildren<Text>(), () => Loc.Get("records")));

        var langBtn = MakeButton(panel, "lang_mode",
            new Vector2(0.70f, 0.91f), new Vector2(0.99f, 0.99f),
            new Color(0.12f, 0.26f, 0.52f), OnLangToggleClicked);
        var langText = langBtn.GetComponentInChildren<Text>();
        langText.fontSize = 32;
        langText.text = Loc.Get("lang_btn");
        _localizedTexts.Add((langBtn.GetComponentInChildren<Text>(), () => Loc.Get("lang_btn")));

        return panel;
    }

    // Game over panel — sectioned layout: Winner / Score / Mission / Buttons.
    // Uses a consistent visual hierarchy (84pt → 32pt header → 44pt content)
    // so the eye doesn't trip over font-size jumps.
    GameObject BuildGameOverPanel(GameObject parent)
    {
        return OthelloTheme.Active switch
        {
            ThemeKind.Wabi   => BuildGameOverPanel_Wabi(parent),
            ThemeKind.Neon   => BuildGameOverPanel_Neon(parent),
            ThemeKind.Pieces => BuildGameOverPanel_Pieces(parent),
            _                => BuildGameOverPanel_Riso(parent),
        };
    }

    GameObject BuildGameOverPanel_Modern(GameObject parent)
    {
        var panel = MakePanel(parent, "GameOverPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        var dimBg = panel.AddComponent<Image>();
        dimBg.color = new Color(0f, 0f, 0f, 0.88f);
        dimBg.raycastTarget = false;

        var cardGO = MakePanel(panel, "Card");
        SetAnchor(cardGO, 0.04f, 0.06f, 0.96f, 0.94f);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(0.06f, 0.18f, 0.08f, 1f);
        cardImg.raycastTarget = false;

        // Winner banner — top 14%
        var winnerGO = MakePanel(cardGO, "Winner");
        SetAnchor(winnerGO, 0f, 0.86f, 1f, 1.00f);
        _winnerText = MakeText(winnerGO, "", 84, Color.white, TextAnchor.MiddleCenter, true);
        _winnerText.fontStyle = FontStyle.Bold;

        // "Score" section header (small green, like a divider label)
        var scoreHdr = MakePanel(cardGO, "ScoreHeader");
        SetAnchor(scoreHdr, 0.05f, 0.78f, 0.95f, 0.84f);
        var scoreHdrText = MakeText(scoreHdr, "Score  —  stones + tiles + mission",
            28, new Color(0.55f, 0.85f, 0.55f), TextAnchor.MiddleCenter);
        scoreHdrText.fontStyle = FontStyle.Italic;

        // Black score breakdown
        var blackScoreGO = MakePanel(cardGO, "BlackScore");
        SetAnchor(blackScoreGO, 0.04f, 0.68f, 0.96f, 0.78f);
        _gameOverBlackText = MakeText(blackScoreGO, "", 44,
            Color.white, TextAnchor.MiddleCenter, true);

        // White score breakdown
        var whiteScoreGO = MakePanel(cardGO, "WhiteScore");
        SetAnchor(whiteScoreGO, 0.04f, 0.58f, 0.96f, 0.68f);
        _gameOverWhiteText = MakeText(whiteScoreGO, "", 44,
            new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleCenter, true);

        // "Missions Revealed" section header
        var missHdr = MakePanel(cardGO, "MissionsHeader");
        SetAnchor(missHdr, 0.05f, 0.50f, 0.95f, 0.56f);
        var missHdrText = MakeText(missHdr, Loc.Get("missions_revealed"), 28,
            new Color(0.55f, 0.85f, 0.55f), TextAnchor.MiddleCenter);
        missHdrText.fontStyle = FontStyle.Italic;
        _localizedTexts.Add((missHdrText, () => Loc.Get("missions_revealed")));

        // Black mission reveal card
        var blackMissGO = MakePanel(cardGO, "BlackMission");
        SetAnchor(blackMissGO, 0.04f, 0.34f, 0.96f, 0.48f);
        var blackMissBg = blackMissGO.AddComponent<Image>();
        blackMissBg.color = new Color(0f, 0f, 0f, 0.30f);
        blackMissBg.raycastTarget = false;
        _blackMissionReveal = MakeText(blackMissGO, "", 36,
            Color.white, TextAnchor.MiddleCenter, true);

        // White mission reveal card
        var whiteMissGO = MakePanel(cardGO, "WhiteMission");
        SetAnchor(whiteMissGO, 0.04f, 0.18f, 0.96f, 0.32f);
        var whiteMissBg = whiteMissGO.AddComponent<Image>();
        whiteMissBg.color = new Color(0f, 0f, 0f, 0.30f);
        whiteMissBg.raycastTarget = false;
        _whiteMissionReveal = MakeText(whiteMissGO, "", 36,
            new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleCenter, true);

        // Buttons — bottom 14%
        var replayBtn = MakeButton(cardGO, "play_again",
            new Vector2(0.05f, 0.02f), new Vector2(0.48f, 0.14f),
            new Color(0.16f, 0.60f, 0.26f), OnReplayClicked);
        _localizedTexts.Add((replayBtn.GetComponentInChildren<Text>(), () => Loc.Get("play_again")));

        var menuBtn = MakeButton(cardGO, "menu",
            new Vector2(0.52f, 0.02f), new Vector2(0.95f, 0.14f),
            new Color(0.28f, 0.28f, 0.30f), OnMainMenuClicked);
        _localizedTexts.Add((menuBtn.GetComponentInChildren<Text>(), () => Loc.Get("menu")));

        panel.SetActive(false);
        return panel;
    }

    static void SetAnchor(GameObject go, float xMin, float yMin, float xMax, float yMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ── Event Handlers ───────────────────────────────────────────────────────

    void OnTurnChanged(TurnChangedEvent e)
    {
        _lastTurnPlayer = e.playerColor;
        _blackScoreText.text = e.blackCount.ToString();
        _whiteScoreText.text = e.whiteCount.ToString();
        // Don't clobber the "X passes!" message if a pass flash is currently
        // running — FlashPassMessage will restore the turn label using the
        // updated _lastTurnPlayer value when its 1-second wait finishes.
        if (_passFlashCoroutine == null)
        {
            _turnIndicatorText.text = e.playerColor == 1
                ? Loc.Get("black_turn") : Loc.Get("white_turn");
        }

        // Mission bar shows whoever's mission the event reports (in vs-AI
        // this is always the human player). Always show the actual mission
        // — never replace with "???"  — so the player's status is visible
        // continuously, including during the AI's turn.
        _missionNameText.text     = Loc.Get(e.missionLocKey);
        string achievedMark       = e.missionAchieved ? "  ✓" : "";
        _missionProgressText.text = e.missionProgress + "  +" + e.missionBonus + "pt" + achievedMark;
        _missionProgressText.color = e.missionAchieved ? MissionAchieved : MissionPending;

        // Mission completion celebration: detect false → true transition for
        // THE DISPLAYED mission's owner (missionPlayerColor). Show the
        // dedicated mission-complete snackbar overlay.
        bool wasAchieved = e.missionPlayerColor == 1
            ? _blackMissionAchieved
            : _whiteMissionAchieved;
        if (e.missionAchieved && !wasAchieved)
        {
            if (e.missionPlayerColor == 1) _blackMissionAchieved = true;
            else                           _whiteMissionAchieved = true;

            if (_missionFlashCoroutine != null) StopCoroutine(_missionFlashCoroutine);
            _missionFlashCoroutine = StartCoroutine(ShowMissionSnackbar(e.missionBonus));
        }
        else if (!e.missionAchieved)
        {
            // Mission can de-achieve if opponent flips the relevant pieces.
            if (e.missionPlayerColor == 1) _blackMissionAchieved = false;
            else                           _whiteMissionAchieved = false;
        }
    }

    GameObject BuildMissionSnackbar(GameObject parent)
    {
        // Floats above the board area, just below the mission bar. Visible
        // for ~2.0s when a mission flips from incomplete to complete.
        var panel = MakePanel(parent, "MissionSnackbar");
        var rt = panel.GetComponent<RectTransform>();
        // Floats just below the new mission strip (which sits at
        // 0.79–0.875 because TopBar grew to 240px).
        rt.anchorMin = new Vector2(0.06f, 0.700f);
        rt.anchorMax = new Vector2(0.94f, 0.780f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.45f, 0.20f, 0.96f);
        bg.raycastTarget = false;

        // Bright top + bottom borders for emphasis
        var topB = MakePanel(panel, "TopBorder");
        var trt = topB.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.93f); trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        topB.AddComponent<Image>().color = new Color(0.45f, 0.90f, 0.55f, 1f);

        var botB = MakePanel(panel, "BotBorder");
        var brt = botB.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(1f, 0.07f);
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        botB.AddComponent<Image>().color = new Color(0.45f, 0.90f, 0.55f, 1f);

        // Left accent bar
        var accent = MakePanel(panel, "Accent");
        var art = accent.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0f, 0f); art.anchorMax = new Vector2(0.025f, 1f);
        art.offsetMin = Vector2.zero; art.offsetMax = Vector2.zero;
        accent.AddComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 1f);

        // ✓ icon (left)
        var iconGO = MakePanel(panel, "Icon");
        var irt = iconGO.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.05f, 0.15f); irt.anchorMax = new Vector2(0.18f, 0.85f);
        irt.offsetMin = Vector2.zero; irt.offsetMax = Vector2.zero;
        var icon = MakeText(iconGO, "✓", 56, new Color(0.45f, 0.95f, 0.55f), TextAnchor.MiddleCenter);
        icon.fontStyle = FontStyle.Bold;

        // Title text
        var titleGO = MakePanel(panel, "Title");
        var ttt = titleGO.GetComponent<RectTransform>();
        ttt.anchorMin = new Vector2(0.20f, 0.45f); ttt.anchorMax = new Vector2(0.78f, 0.95f);
        ttt.offsetMin = Vector2.zero; ttt.offsetMax = Vector2.zero;
        var title = MakeText(titleGO, Loc.Get("mission_complete"), 36, Color.white, TextAnchor.MiddleLeft, true);
        title.fontStyle = FontStyle.Bold;
        _localizedTexts.Add((title, () => Loc.Get("mission_complete")));

        // Sub line (bonus)
        var subGO = MakePanel(panel, "Sub");
        var srt = subGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.20f, 0.10f); srt.anchorMax = new Vector2(0.78f, 0.50f);
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        _missionSnackbarText = MakeText(subGO, "", 26, new Color(0.85f, 1f, 0.85f), TextAnchor.MiddleLeft);

        panel.SetActive(false);
        return panel;
    }

    IEnumerator ShowMissionSnackbar(int bonus)
    {
        _missionSnackbarText.text = "+" + bonus + " pt   ·   keep it that way";
        _missionSnackbarPanel.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        _missionSnackbarPanel.SetActive(false);
        _missionFlashCoroutine = null;
    }

    // Pass is shown by recoloring + relabeling the turn indicator briefly
    // (no separate toast popup that could overlap the game-over panel).
    // Set text/color synchronously here so the message is visible even if
    // a TurnChangedEvent fires immediately after pass; the OnTurnChanged
    // guard skips the label update while _passFlashCoroutine is non-null.
    void OnPassTurn(PassTurnEvent e)
    {
        if (_passFlashCoroutine != null) StopCoroutine(_passFlashCoroutine);

        string key = e.playerColor == 1 ? "black_passes" : "white_passes";
        _turnIndicatorText.text = Loc.Get(key);
        _turnIndicatorText.color = TurnIndicatorPass;

        _passFlashCoroutine = StartCoroutine(RestoreTurnIndicatorAfterDelay());
    }

    IEnumerator RestoreTurnIndicatorAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);
        _turnIndicatorText.color = TurnIndicatorNormal;
        _turnIndicatorText.text = _lastTurnPlayer == 1
            ? Loc.Get("black_turn") : Loc.Get("white_turn");
        _passFlashCoroutine = null;
    }

    void OnGameOver(GameOverEvent e)
    {
        // Cancel any in-flight pass flash so it doesn't overwrite the
        // restored turn indicator behind the dimmed game-over panel.
        if (_passFlashCoroutine != null)
        {
            StopCoroutine(_passFlashCoroutine);
            _passFlashCoroutine = null;
        }
        _turnIndicatorText.color = TurnIndicatorNormal;
        _turnIndicatorText.text  = _lastTurnPlayer == 1
            ? Loc.Get("black_turn") : Loc.Get("white_turn");

        _lastWinner  = e.winner;
        _hasGameOver = true;
        _lastGameOver = e;

        // Run the result-reveal animation FIRST: clear the board, then place
        // each stone one-by-one while ticking the score counters from 0.
        // The game-over panel itself appears once the reveal completes.
        StartCoroutine(GameOverRevealSequence(e));
    }

    // Tunable from tests if needed — set short to skip the show.
    public static float RevealStonePlaceDelay = 0.04f;
    public static float RevealPhaseGap        = 0.40f;
    public static float RevealClearPause      = 0.30f;

    IEnumerator GameOverRevealSequence(GameOverEvent e)
    {
        // Keep the in-game UI visible during the reveal — the board and the
        // top bar are part of the show. Mission bar stays as-is.
        _gameOverPanel.SetActive(false);

        // Reset score displays to 0 so the count-up reads cleanly.
        _blackScoreText.text = "0";
        _whiteScoreText.text = "0";
        _turnIndicatorText.color = new Color(0.949f, 0.769f, 0.282f);
        _turnIndicatorText.text  = "FINAL  RESULT";

        // Clear the board completely.
        EventBus.Publish(new BoardClearAllEvent());
        if (RevealClearPause > 0f) yield return new WaitForSeconds(RevealClearPause);

        var board = e.finalBoard;
        if (board != null)
        {
            // Phase 1: place all black stones (row-by-row), tick black score.
            int blackTicker = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (board[r, c] == 1)
                    {
                        EventBus.Publish(new PiecePlacedEvent { row = r, col = c, playerColor = 1 });
                        blackTicker++;
                        _blackScoreText.text = blackTicker.ToString();
                        if (RevealStonePlaceDelay > 0f)
                            yield return new WaitForSeconds(RevealStonePlaceDelay);
                    }
            if (RevealPhaseGap > 0f) yield return new WaitForSeconds(RevealPhaseGap);

            // Phase 2: place all white stones, tick white score.
            int whiteTicker = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (board[r, c] == 2)
                    {
                        EventBus.Publish(new PiecePlacedEvent { row = r, col = c, playerColor = 2 });
                        whiteTicker++;
                        _whiteScoreText.text = whiteTicker.ToString();
                        if (RevealStonePlaceDelay > 0f)
                            yield return new WaitForSeconds(RevealStonePlaceDelay);
                    }
            if (RevealPhaseGap > 0f) yield return new WaitForSeconds(RevealPhaseGap);
        }

        // Reveal complete: hide in-game UI under the game-over overlay and
        // show the breakdown panel (tile bonus + mission reveal + buttons).
        _gamePanel.SetActive(false);
        _gameOverPanel.SetActive(true);
        _homeBtnGO.SetActive(false);
        UpdateGameOverTexts(e);
    }

    void UpdateGameOverTexts(GameOverEvent e)
    {
        // In vs-AI mode the player is always Black (player 1). Show
        // perspective text ("You Win!" / "You Lose") so they don't have to
        // remember which side they were. In vs-Human mode keep the literal
        // "Black/White Wins!" since neither player is "you".
        string result;
        if (e.winner == 0)
            result = Loc.Get("draw");
        else if (_isVsAI)
            result = e.winner == 1 ? Loc.Get("you_win") : Loc.Get("you_lose");
        else
            result = e.winner == 1 ? Loc.Get("black_wins") : Loc.Get("white_wins");

        _winnerText.text = result;
        if (_winnerShadowText != null)     _winnerShadowText.text = result;
        if (_neonWinShadowPink != null)    _neonWinShadowPink.text = result;
        if (_neonWinShadowCyan != null)    _neonWinShadowCyan.text = result;

        int blackMissionPts = e.blackMissionAchieved ? e.blackMission.Bonus : 0;
        int whiteMissionPts = e.whiteMissionAchieved ? e.whiteMission.Bonus : 0;
        int blackTotal = e.blackCount + e.blackTileBonus + blackMissionPts;
        int whiteTotal = e.whiteCount + e.whiteTileBonus + whiteMissionPts;

        _gameOverBlackText.text = $"● {e.blackCount} + {e.blackTileBonus} + {blackMissionPts} = {blackTotal}";
        _gameOverWhiteText.text = $"○ {e.whiteCount} + {e.whiteTileBonus} + {whiteMissionPts} = {whiteTotal}";

        string blackAchievedStr = e.blackMissionAchieved
            ? Loc.Get("mission_achieved") + " +" + e.blackMission.Bonus
            : Loc.Get("mission_failed");
        _blackMissionReveal.text =
            $"●  {Loc.Get(e.blackMission.GetLocKey())}\n{blackAchievedStr}";

        string whiteAchievedStr = e.whiteMissionAchieved
            ? Loc.Get("mission_achieved") + " +" + e.whiteMission.Bonus
            : Loc.Get("mission_failed");
        _whiteMissionReveal.text =
            $"○  {Loc.Get(e.whiteMission.GetLocKey())}\n{whiteAchievedStr}";
    }


    void OnReplayClicked()
    {
        _gameOverPanel.SetActive(false);
        _gamePanel.SetActive(false);
        _homeBtnGO.SetActive(false);
        _hasGameOver = false;
        ShowModeSelect();
    }

    void OnMainMenuClicked()
    {
        _gameOverPanel.SetActive(false);
        _gamePanel.SetActive(false);
        _homeBtnGO.SetActive(false);
        _hasGameOver = false;
        ShowModeSelect();
    }

    void OnTitleButtonClicked()
    {
        _gamePanel.SetActive(false);
        _homeBtnGO.SetActive(false);
        _hasGameOver = false;
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

        if (_hasGameOver)
            UpdateGameOverTexts(_lastGameOver);
    }

    void ShowModeSelect()
    {
        _modeSelectPanel.SetActive(true);
        if (_settingsBtnGO != null) _settingsBtnGO.SetActive(true);
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

    Text MakeText(GameObject parent, string content, int fontSize, Color color, TextAnchor alignment,
                  bool autoSize = false)
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
        text.resizeTextForBestFit = autoSize;
        if (autoSize)
        {
            text.resizeTextMinSize = Mathf.Max(12, fontSize / 3);
            text.resizeTextMaxSize = fontSize;
        }
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

        MakeText(go, Loc.Get(key), 48, Color.white, TextAnchor.MiddleCenter, true);
        return go;
    }
}
