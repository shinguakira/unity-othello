using UnityEngine;
using UnityEngine.UI;

// 4 visual themes for Othello UI. Built around procedural decorative sprites
// (halftone, grid, glow, piece-pattern, seal) — not just colored rectangles.
// Logical GameObject names (Btn_vs_ai etc.) are stable across themes so
// PlayMode E2E tests find them regardless of which theme is active.
public partial class OthelloUIManager
{
    // ── Shared utilities ──────────────────────────────────────────────

    GameObject MakeRect(GameObject parent, string name, Color color,
        float xMin, float yMin, float xMax, float yMax)
    {
        var go = MakePanel(parent, name);
        SetAnchor(go, xMin, yMin, xMax, yMax);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    GameObject MakeSpriteRect(GameObject parent, string name, Sprite sprite, Color color,
        float xMin, float yMin, float xMax, float yMax,
        Image.Type type = Image.Type.Simple)
    {
        var go = MakePanel(parent, name);
        SetAnchor(go, xMin, yMin, xMax, yMax);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = type;
        img.raycastTarget = false;
        return go;
    }

    GameObject MakeTiledSprite(GameObject parent, string name, Sprite sprite, Color color,
        float xMin, float yMin, float xMax, float yMax)
        => MakeSpriteRect(parent, name, sprite, color, xMin, yMin, xMax, yMax, Image.Type.Tiled);

    Text MakeLabelAt(GameObject parent, string name, string content,
        int fontSize, Color color, TextAnchor anchor,
        float xMin, float yMin, float xMax, float yMax,
        FontStyle style = FontStyle.Normal, bool autoSize = false)
    {
        var go = MakePanel(parent, name);
        SetAnchor(go, xMin, yMin, xMax, yMax);
        var t = MakeText(go, content, fontSize, color, anchor, autoSize);
        t.fontStyle = style;
        return t;
    }

    static GameObject FindByNameInChild(GameObject root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }

    void StartGame(bool vsAI)
    {
        _modeSelectPanel.SetActive(false);
        _gamePanel.SetActive(true);
        _homeBtnGO.SetActive(true);
        if (_settingsBtnGO != null) _settingsBtnGO.SetActive(false);
        EventBus.Publish(new GameModeSelectedEvent { vsAI = vsAI });
    }

    // ── Theme picker overlay (shared, theme-neutral) ───────────────────

    GameObject BuildThemePicker(GameObject parent)
    {
        var panel = MakePanel(parent, "ThemePickerPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        var dim = panel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.78f);
        dim.raycastTarget = true;

        // Tap dim background to close
        var dimBtn = panel.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(() => _themePickerPanel.SetActive(false));

        // Card
        var card = MakePanel(panel, "Card");
        SetAnchor(card, 0.08f, 0.20f, 0.92f, 0.80f);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.094f, 0.110f, 0.137f, 1f);
        cardImg.raycastTarget = true;
        // Block click-through on the card itself.
        var cardBtn = card.AddComponent<Button>();
        cardBtn.transition = Selectable.Transition.None;

        // Border
        MakeRect(card, "BorderT", new Color(0.949f, 0.769f, 0.282f),
            0f, 0.985f, 1f, 1f);
        MakeRect(card, "BorderB", new Color(0.949f, 0.769f, 0.282f),
            0f, 0f, 1f, 0.015f);

        MakeLabelAt(card, "Title", "SETTINGS",
            56, new Color(1f, 1f, 1f, 1f), TextAnchor.MiddleCenter,
            0.05f, 0.87f, 0.95f, 0.96f, FontStyle.Bold);
        MakeLabelAt(card, "Sub", "—  design theme  —",
            22, new Color(0.949f, 0.769f, 0.282f), TextAnchor.MiddleCenter,
            0.05f, 0.81f, 0.95f, 0.86f, FontStyle.Italic);

        // Hairline
        MakeRect(card, "Rule1",
            new Color(1f, 1f, 1f, 0.18f),
            0.10f, 0.795f, 0.90f, 0.798f);

        // Theme rows (4)
        BuildThemeRow(card, ThemeKind.Pieces, "Pieces",
            "board-forward · green felt · cream + gold",
            0.05f, 0.62f, 0.95f, 0.78f);
        BuildThemeRow(card, ThemeKind.Riso,   "Riso",
            "2-color risograph · halftone · pink misregistration",
            0.05f, 0.46f, 0.95f, 0.62f);
        BuildThemeRow(card, ThemeKind.Wabi,   "Wabi",
            "Japanese minimal · vermillion seal · vertical kanji",
            0.05f, 0.30f, 0.95f, 0.46f);
        BuildThemeRow(card, ThemeKind.Neon,   "Neon",
            "synthwave · pink + cyan glow · arcade",
            0.05f, 0.14f, 0.95f, 0.30f);

        // Close button
        var closeBtn = MakeButton(card, "close",
            new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.11f),
            new Color(0.16f, 0.18f, 0.22f), () => _themePickerPanel.SetActive(false));
        var closeText = closeBtn.GetComponentInChildren<Text>();
        closeText.text = "✕  CLOSE";
        closeText.fontSize = 28;
        closeText.fontStyle = FontStyle.Bold;
        closeText.color = new Color(1f, 1f, 1f, 0.85f);

        panel.SetActive(false);
        return panel;
    }

    void BuildThemeRow(GameObject parent, ThemeKind kind, string title, string desc,
        float xMin, float yMin, float xMax, float yMax)
    {
        bool active = OthelloTheme.Active == kind;
        var go = new GameObject("Btn_theme_" + kind);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = active
            ? new Color(0.949f, 0.769f, 0.282f, 0.16f)
            : new Color(1f, 1f, 1f, 0.04f);
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(() => OthelloTheme.SetActive(kind));

        // Active marker (gold left bar)
        if (active)
        {
            MakeRect(go, "Marker",
                new Color(0.949f, 0.769f, 0.282f, 1f),
                0f, 0f, 0.012f, 1f);
        }

        // Theme name
        var titleColor = active ? new Color(0.949f, 0.769f, 0.282f) : new Color(1f, 1f, 1f, 0.95f);
        MakeLabelAt(go, "Name", title.ToUpperInvariant(),
            44, titleColor, TextAnchor.MiddleLeft,
            0.04f, 0.50f, 0.85f, 0.95f, FontStyle.Bold);

        // Description
        MakeLabelAt(go, "Desc", desc,
            22, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleLeft,
            0.04f, 0.05f, 0.85f, 0.50f, FontStyle.Italic);

        // Active checkmark
        if (active)
        {
            MakeLabelAt(go, "Check", "✓",
                64, new Color(0.949f, 0.769f, 0.282f), TextAnchor.MiddleCenter,
                0.85f, 0.20f, 0.97f, 0.80f, FontStyle.Bold);
        }
        else
        {
            MakeLabelAt(go, "Arrow", "→",
                48, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter,
                0.85f, 0.20f, 0.97f, 0.80f, FontStyle.Bold);
        }
    }

    void OpenThemePicker() => _themePickerPanel.SetActive(true);

    // ═══════════════════════════════════════════════════════════════════
    // RISO — risograph zine print
    //
    // 2 spot colors (deep ink + fluorescent pink). Halftone dot field as
    // background (procedurally generated). Each major element rendered TWICE
    // with a 6-8px offset; the back copy is in the spot color at 60% alpha
    // (ink-overprint misregistration). Hairline page frame. Folio + plate
    // marks at corners. Section dividers are dotted lines.
    // ═══════════════════════════════════════════════════════════════════

    static readonly Color RisoPaper = new Color(0.961f, 0.949f, 0.910f);
    static readonly Color RisoInk   = new Color(0.090f, 0.105f, 0.137f);
    static readonly Color RisoPink  = new Color(0.949f, 0.290f, 0.580f);
    static readonly Color RisoTeal  = new Color(0.169f, 0.557f, 0.541f);

    GameObject BuildModeSelectPanel_Riso(GameObject parent)
    {
        var panel = MakePanel(parent, "ModeSelectPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = RisoPaper;

        // Halftone dot field (sparse) tinted pink — dominates the upper-right.
        MakeTiledSprite(panel, "HalftoneBg", ThemeSprites.HalftoneSparse,
            new Color(RisoPink.r, RisoPink.g, RisoPink.b, 0.55f),
            0.45f, 0.55f, 1.05f, 1.05f);
        // Halftone dense in the lower-left, ink color
        MakeTiledSprite(panel, "HalftoneBg2", ThemeSprites.HalftoneDense,
            new Color(RisoInk.r, RisoInk.g, RisoInk.b, 0.18f),
            -0.05f, -0.05f, 0.55f, 0.45f);

        // Page frame (1px hairline, 24px from edge)
        BuildPageFrame(panel, RisoInk, 0.025f);

        // Plate registration marks at corners (small + signs)
        MakeLabelAt(panel, "Reg1", "+", 32, RisoInk, TextAnchor.MiddleCenter, 0.03f, 0.965f, 0.07f, 0.99f);
        MakeLabelAt(panel, "Reg2", "+", 32, RisoInk, TextAnchor.MiddleCenter, 0.93f, 0.965f, 0.97f, 0.99f);
        MakeLabelAt(panel, "Reg3", "+", 32, RisoInk, TextAnchor.MiddleCenter, 0.03f, 0.01f, 0.07f, 0.035f);
        MakeLabelAt(panel, "Reg4", "+", 32, RisoInk, TextAnchor.MiddleCenter, 0.93f, 0.01f, 0.97f, 0.035f);

        // Top folio metadata
        MakeLabelAt(panel, "Folio1", "Nº 24  /  MMXXVI",
            22, RisoInk, TextAnchor.MiddleLeft, 0.08f, 0.94f, 0.50f, 0.97f, FontStyle.Bold);
        MakeLabelAt(panel, "Folio2", "the strategic gambit",
            22, RisoInk, TextAnchor.MiddleRight, 0.50f, 0.94f, 0.92f, 0.97f, FontStyle.Italic);

        // Hairline rule
        MakeRect(panel, "TopRule", RisoInk, 0.08f, 0.93f, 0.92f, 0.932f);

        // Title — pink offset shadow first, then ink on top.
        var titlePink = MakeLabelAt(panel, "TitleShadow", Loc.Get("title").ToUpperInvariant(),
            240, new Color(RisoPink.r, RisoPink.g, RisoPink.b, 0.85f),
            TextAnchor.UpperLeft, 0.078f, 0.66f, 0.95f, 0.92f, FontStyle.Bold);
        var titleInk = MakeLabelAt(panel, "Title", Loc.Get("title").ToUpperInvariant(),
            240, RisoInk, TextAnchor.UpperLeft, 0.07f, 0.667f, 0.94f, 0.927f, FontStyle.Bold);
        _localizedTexts.Add((titlePink, () => Loc.Get("title").ToUpperInvariant()));
        _localizedTexts.Add((titleInk, () => Loc.Get("title").ToUpperInvariant()));

        // Subtitle as printer-style serial line
        MakeLabelAt(panel, "Sub1", "8 × 8  /  64 cells  /  2 stones",
            26, RisoInk, TextAnchor.MiddleLeft, 0.08f, 0.62f, 0.92f, 0.65f);
        // Decorative seal in upper-right (pink ink stamp)
        MakeSpriteRect(panel, "Seal", ThemeSprites.Seal,
            new Color(RisoPink.r, RisoPink.g, RisoPink.b, 0.85f),
            0.78f, 0.78f, 0.94f, 0.92f);

        // Dotted divider (we approximate with a row of dots)
        BuildDottedRule(panel, RisoInk, 0.08f, 0.605f, 0.92f, 0.610f, 36);

        // Stats line
        MakeLabelAt(panel, "Stats",
            "PLAYED ............ 24    WINS .......... 14    RATE ........ 58%",
            24, RisoInk, TextAnchor.MiddleLeft, 0.08f, 0.56f, 0.92f, 0.59f);

        BuildDottedRule(panel, RisoInk, 0.08f, 0.545f, 0.92f, 0.550f, 36);

        // Mode select label
        MakeLabelAt(panel, "ModeLabel", "PLAY  →",
            34, RisoPink, TextAnchor.MiddleLeft, 0.08f, 0.50f, 0.92f, 0.54f, FontStyle.Bold);

        // Two big mode buttons: ink-fill with pink offset shadow
        BuildRisoButton(panel, "vs_ai",   0.08f, 0.36f, 0.92f, 0.48f, RisoInk, RisoPaper, RisoPink, () => StartGame(true));
        BuildRisoButton(panel, "vs_human",0.08f, 0.22f, 0.92f, 0.34f, RisoInk, RisoPaper, RisoPink, () => StartGame(false));

        BuildDottedRule(panel, RisoInk, 0.08f, 0.205f, 0.92f, 0.210f, 36);

        // Records (text link) + lang toggle
        BuildRisoTextButton(panel, "records",   0.08f, 0.13f, 0.50f, 0.18f, ShowRecords, RisoInk);
        BuildRisoTextButton(panel, "lang_mode", 0.55f, 0.13f, 0.92f, 0.18f, OnLangToggleClicked, RisoTeal);

        // Footer
        MakeRect(panel, "BotRule", RisoInk, 0.08f, 0.07f, 0.92f, 0.072f);
        MakeLabelAt(panel, "Footer1", "OTHELLO  ·  V.1.0  ·  PRINTED IN TOKYO",
            20, RisoInk, TextAnchor.MiddleCenter, 0.08f, 0.04f, 0.92f, 0.07f, FontStyle.Bold);

        return panel;
    }

    void BuildPageFrame(GameObject parent, Color c, float inset)
    {
        // Top
        MakeRect(parent, "FrameT", c, inset, 1f - inset - 0.002f, 1f - inset, 1f - inset);
        // Bottom
        MakeRect(parent, "FrameB", c, inset, inset, 1f - inset, inset + 0.002f);
        // Left
        MakeRect(parent, "FrameL", c, inset, inset, inset + 0.0015f, 1f - inset);
        // Right
        MakeRect(parent, "FrameR", c, 1f - inset - 0.0015f, inset, 1f - inset, 1f - inset);
    }

    void BuildDottedRule(GameObject parent, Color c, float xMin, float yMin, float xMax, float yMax, int dots)
    {
        for (int i = 0; i < dots; i++)
        {
            float t0 = (float)i / dots;
            float t1 = t0 + 0.45f / dots;
            MakeSpriteRect(parent, "dot" + i, ThemeSprites.Circle, c,
                Mathf.Lerp(xMin, xMax, t0), yMin,
                Mathf.Lerp(xMin, xMax, t1), yMax);
        }
    }

    void BuildRisoButton(GameObject parent, string key,
        float xMin, float yMin, float xMax, float yMax,
        Color fill, Color text, Color shadow,
        UnityEngine.Events.UnityAction onClick)
    {
        // Shadow rectangle (offset -0.008, -0.008)
        MakeRect(parent, key + "_shadow", new Color(shadow.r, shadow.g, shadow.b, 0.85f),
            xMin + 0.008f, yMin - 0.008f, xMax + 0.008f, yMax - 0.008f);

        // Foreground button
        var go = new GameObject("Btn_" + key);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = fill;
        var btn = go.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);

        var label = MakeText(go, Loc.Get(key).ToUpperInvariant(), 78, text, TextAnchor.MiddleLeft);
        label.fontStyle = FontStyle.Bold;
        var labelRT = label.GetComponent<RectTransform>();
        labelRT.offsetMin = new Vector2(38, 0);
        labelRT.offsetMax = new Vector2(-90, 0);
        _localizedTexts.Add((label, () => Loc.Get(key).ToUpperInvariant()));

        var arr = MakePanel(go, "Arr");
        SetAnchor(arr, 0.85f, 0.20f, 0.97f, 0.80f);
        MakeText(arr, "▸", 80, text, TextAnchor.MiddleRight);
    }

    void BuildRisoTextButton(GameObject parent, string key,
        float xMin, float yMin, float xMax, float yMax,
        UnityEngine.Events.UnityAction onClick, Color color)
    {
        var go = new GameObject("Btn_" + key);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var btn = go.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);

        var label = MakeText(go, Loc.Get(key).ToUpperInvariant(), 32, color, TextAnchor.MiddleLeft);
        label.fontStyle = FontStyle.Bold;
        _localizedTexts.Add((label, () => Loc.Get(key).ToUpperInvariant()));

        var ul = MakePanel(go, "Underline");
        SetAnchor(ul, 0f, 0.10f, 1f, 0.13f);
        ul.AddComponent<Image>().color = color;
    }

    GameObject BuildGameOverPanel_Riso(GameObject parent)
    {
        var panel = MakePanel(parent, "GameOverPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = RisoPaper;

        MakeTiledSprite(panel, "HalftoneBg", ThemeSprites.HalftoneSparse,
            new Color(RisoPink.r, RisoPink.g, RisoPink.b, 0.40f),
            0f, 0.55f, 1f, 1f);

        BuildPageFrame(panel, RisoInk, 0.025f);

        MakeLabelAt(panel, "Folio", "RESULT  ·  Nº 24  ·  MMXXVI",
            22, RisoInk, TextAnchor.MiddleCenter, 0.08f, 0.94f, 0.92f, 0.97f, FontStyle.Bold);
        MakeRect(panel, "TopRule", RisoInk, 0.08f, 0.93f, 0.92f, 0.932f);

        // Winner — offset shadow + ink
        var winS = MakeLabelAt(panel, "WinnerShadow", "",
            150, new Color(RisoPink.r, RisoPink.g, RisoPink.b, 0.85f),
            TextAnchor.MiddleCenter, 0.058f, 0.74f, 0.94f, 0.92f, FontStyle.Bold, autoSize: true);
        _winnerText = MakeLabelAt(panel, "Winner", "",
            150, RisoInk, TextAnchor.MiddleCenter, 0.05f, 0.747f, 0.95f, 0.927f, FontStyle.Bold, autoSize: true);
        // Mirror the winner text into shadow whenever updated.
        _winnerShadowText = winS;

        BuildDottedRule(panel, RisoInk, 0.08f, 0.71f, 0.92f, 0.715f, 36);

        // Score breakdown
        MakeLabelAt(panel, "ScoreLabel", "SCORE  ·  stones + tiles + mission",
            26, RisoTeal, TextAnchor.MiddleLeft, 0.08f, 0.66f, 0.92f, 0.69f, FontStyle.Bold);

        _gameOverBlackText = MakeLabelAt(panel, "BlackScore", "",
            44, RisoInk, TextAnchor.MiddleLeft, 0.10f, 0.58f, 0.90f, 0.65f,
            FontStyle.Bold, autoSize: true);
        _gameOverWhiteText = MakeLabelAt(panel, "WhiteScore", "",
            44, RisoInk, TextAnchor.MiddleLeft, 0.10f, 0.50f, 0.90f, 0.57f,
            FontStyle.Bold, autoSize: true);

        BuildDottedRule(panel, RisoInk, 0.08f, 0.475f, 0.92f, 0.480f, 36);

        // Mission reveal
        MakeLabelAt(panel, "MissionsHeader", "MISSIONS REVEALED",
            26, RisoPink, TextAnchor.MiddleLeft, 0.08f, 0.42f, 0.92f, 0.46f, FontStyle.Bold);
        _localizedTexts.Add((
            FindByNameInChild(panel, "MissionsHeader").GetComponentInChildren<Text>(),
            () => "MISSIONS REVEALED"));

        _blackMissionReveal = MakeLabelAt(panel, "BlackMission", "",
            34, RisoInk, TextAnchor.MiddleLeft, 0.10f, 0.34f, 0.90f, 0.41f, FontStyle.Normal, autoSize: true);
        _whiteMissionReveal = MakeLabelAt(panel, "WhiteMission", "",
            34, RisoInk, TextAnchor.MiddleLeft, 0.10f, 0.26f, 0.90f, 0.33f, FontStyle.Normal, autoSize: true);

        BuildDottedRule(panel, RisoInk, 0.08f, 0.235f, 0.92f, 0.240f, 36);

        // Buttons
        BuildRisoButton(panel, "play_again", 0.08f, 0.10f, 0.50f, 0.21f, RisoInk, RisoPaper, RisoPink, OnReplayClicked);
        BuildRisoButton(panel, "menu",       0.52f, 0.10f, 0.92f, 0.21f, RisoTeal, RisoPaper, RisoPink, OnMainMenuClicked);

        MakeRect(panel, "BotRule", RisoInk, 0.08f, 0.07f, 0.92f, 0.072f);
        MakeLabelAt(panel, "Footer", "PRINTED ON RISO  ·  END OF MATCH  ·  THANK YOU",
            20, RisoInk, TextAnchor.MiddleCenter, 0.08f, 0.04f, 0.92f, 0.07f, FontStyle.Bold);

        panel.SetActive(false);
        return panel;
    }

    Text _winnerShadowText; // mirror for Riso winner shadow

    // ═══════════════════════════════════════════════════════════════════
    // WABI — Japanese minimalism
    //
    // Off-white paper. Vertical orientation. Single 朱 (vermillion) seal
    // stamp as accent. Sparse hairline rules. Tracking-out title set in 2
    // lines — 一行目 ENGLISH + 二行目 漢字. Asymmetric placement (rule of
    // thirds, weighted bottom-left). 漢数字 numerals where possible.
    // ═══════════════════════════════════════════════════════════════════

    static readonly Color WabiPaper  = new Color(0.969f, 0.957f, 0.929f);
    static readonly Color WabiInk    = new Color(0.094f, 0.078f, 0.063f);
    static readonly Color WabiSumi   = new Color(0.235f, 0.220f, 0.196f);
    static readonly Color WabiSeal   = new Color(0.722f, 0.157f, 0.137f);
    static readonly Color WabiLine   = new Color(0.792f, 0.749f, 0.690f);

    GameObject BuildModeSelectPanel_Wabi(GameObject parent)
    {
        var panel = MakePanel(parent, "ModeSelectPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = WabiPaper;

        // Subtle paper texture: very faint halftone
        MakeTiledSprite(panel, "PaperTex", ThemeSprites.HalftoneDense,
            new Color(WabiInk.r, WabiInk.g, WabiInk.b, 0.025f),
            0f, 0f, 1f, 1f);

        // Single vertical hairline left of golden ratio (0.382 from right => 0.618)
        MakeRect(panel, "VRule", WabiLine, 0.618f, 0.10f, 0.620f, 0.90f);

        // 朱印 seal at top-right
        MakeSpriteRect(panel, "Seal", ThemeSprites.Seal,
            new Color(WabiSeal.r, WabiSeal.g, WabiSeal.b, 0.95f),
            0.78f, 0.83f, 0.92f, 0.92f);
        MakeLabelAt(panel, "SealKanji", "印",
            38, WabiSeal, TextAnchor.MiddleCenter, 0.78f, 0.83f, 0.92f, 0.92f, FontStyle.Bold);

        // English line: tracking-out, restrained
        MakeLabelAt(panel, "TitleEn", "O   T   H   E   L   L   O",
            72, WabiSumi, TextAnchor.MiddleLeft, 0.08f, 0.78f, 0.62f, 0.84f);

        // Kanji line: bigger, primary
        MakeLabelAt(panel, "TitleJp", "オ  セ  ロ",
            220, WabiInk, TextAnchor.MiddleLeft, 0.08f, 0.56f, 0.62f, 0.76f, FontStyle.Bold);

        // Caption beneath, italic
        MakeLabelAt(panel, "Caption", "─    八  ×  八    ・    秘  匿  指  令    ─",
            32, WabiSumi, TextAnchor.MiddleLeft, 0.08f, 0.50f, 0.62f, 0.55f);

        // Mode rows on the right column past the vertical rule
        BuildWabiRow(panel, "vs_ai",    0.65f, 0.66f, 0.92f, 0.78f, "一", () => StartGame(true));
        MakeRect(panel, "WabiRule1", WabiLine, 0.65f, 0.65f, 0.92f, 0.652f);
        BuildWabiRow(panel, "vs_human", 0.65f, 0.51f, 0.92f, 0.63f, "二", () => StartGame(false));
        MakeRect(panel, "WabiRule2", WabiLine, 0.65f, 0.50f, 0.92f, 0.502f);
        BuildWabiRow(panel, "records",  0.65f, 0.36f, 0.92f, 0.48f, "三", ShowRecords);

        // Stats: vertical block beneath title
        MakeRect(panel, "StatsRule", WabiLine, 0.08f, 0.45f, 0.55f, 0.452f);
        MakeLabelAt(panel, "Stats1", "対局   二十四回",
            34, WabiSumi, TextAnchor.MiddleLeft, 0.08f, 0.39f, 0.62f, 0.43f);
        MakeLabelAt(panel, "Stats2", "勝利   十四回",
            34, WabiSumi, TextAnchor.MiddleLeft, 0.08f, 0.34f, 0.62f, 0.38f);
        MakeLabelAt(panel, "Stats3", "勝率   五割八分",
            34, WabiSeal, TextAnchor.MiddleLeft, 0.08f, 0.29f, 0.62f, 0.33f);

        // Bottom hairline
        MakeRect(panel, "BotRule", WabiLine, 0.08f, 0.18f, 0.92f, 0.182f);

        // Lang toggle: small kanji at bottom right
        BuildWabiTextButton(panel, "lang_mode", 0.78f, 0.10f, 0.92f, 0.16f, OnLangToggleClicked);

        // Date column, far left
        MakeLabelAt(panel, "Date", "令和 八年 五月",
            22, WabiSumi, TextAnchor.MiddleLeft, 0.08f, 0.10f, 0.50f, 0.16f);

        // Footer
        MakeLabelAt(panel, "Footer", "─  オ セ ロ  V.壱  ─",
            22, WabiLine, TextAnchor.MiddleCenter, 0.08f, 0.04f, 0.92f, 0.08f);

        return panel;
    }

    void BuildWabiRow(GameObject parent, string key,
        float xMin, float yMin, float xMax, float yMax,
        string num, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + key);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var btn = go.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);

        // Index kanji on the left edge (its own panel)
        var numGO = MakePanel(go, "Num");
        SetAnchor(numGO, 0f, 0.30f, 0.18f, 0.85f);
        var n = MakeText(numGO, num, 56, WabiSeal, TextAnchor.MiddleCenter);
        n.fontStyle = FontStyle.Bold;

        // Mode name on the right
        var nameGO = MakePanel(go, "Name");
        SetAnchor(nameGO, 0.20f, 0.30f, 0.92f, 0.85f);
        var name = MakeText(nameGO, Loc.Get(key), 48, WabiInk, TextAnchor.MiddleLeft);
        name.fontStyle = FontStyle.Bold;
        _localizedTexts.Add((name, () => Loc.Get(key)));

        // Subtle subtitle at bottom
        var descGO = MakePanel(go, "Desc");
        SetAnchor(descGO, 0.20f, 0.05f, 0.92f, 0.30f);
        MakeText(descGO,
            key == "vs_ai" ? "機械と対局する" :
            key == "vs_human" ? "二人で対局する" :
            "過去の記録を見る",
            22, WabiSumi, TextAnchor.MiddleLeft);
    }

    void BuildWabiTextButton(GameObject parent, string key,
        float xMin, float yMin, float xMax, float yMax,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + key);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var btn = go.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);

        var label = MakeText(go, Loc.Get(key), 28, WabiInk, TextAnchor.MiddleCenter);
        label.fontStyle = FontStyle.Bold;
        _localizedTexts.Add((label, () => Loc.Get(key)));

        var ul = MakePanel(go, "UL");
        SetAnchor(ul, 0.10f, 0.10f, 0.90f, 0.13f);
        ul.AddComponent<Image>().color = WabiSeal;
    }

    GameObject BuildGameOverPanel_Wabi(GameObject parent)
    {
        var panel = MakePanel(parent, "GameOverPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = WabiPaper;

        MakeTiledSprite(panel, "PaperTex", ThemeSprites.HalftoneDense,
            new Color(WabiInk.r, WabiInk.g, WabiInk.b, 0.025f),
            0f, 0f, 1f, 1f);

        // Top hairline + caption
        MakeLabelAt(panel, "TopCap", "─    終     局    ─",
            34, WabiSumi, TextAnchor.MiddleCenter, 0.10f, 0.93f, 0.90f, 0.97f);
        MakeRect(panel, "TopRule", WabiLine, 0.20f, 0.92f, 0.80f, 0.922f);

        // Seal
        MakeSpriteRect(panel, "Seal", ThemeSprites.Seal,
            new Color(WabiSeal.r, WabiSeal.g, WabiSeal.b, 0.95f),
            0.79f, 0.84f, 0.92f, 0.91f);
        MakeLabelAt(panel, "SealKanji", "終",
            34, WabiSeal, TextAnchor.MiddleCenter, 0.79f, 0.84f, 0.92f, 0.91f, FontStyle.Bold);

        // Winner
        _winnerText = MakeLabelAt(panel, "Winner", "",
            150, WabiInk, TextAnchor.MiddleCenter, 0.08f, 0.74f, 0.92f, 0.88f, FontStyle.Bold, autoSize: true);
        MakeLabelAt(panel, "WinnerSub", "─  勝者  ─",
            28, WabiSeal, TextAnchor.MiddleCenter, 0.30f, 0.69f, 0.70f, 0.73f);

        // Hairline
        MakeRect(panel, "Rule1", WabiLine, 0.20f, 0.685f, 0.80f, 0.687f);

        // Score
        MakeLabelAt(panel, "ScoreLabel", "得    点",
            26, WabiSumi, TextAnchor.MiddleCenter, 0.10f, 0.62f, 0.90f, 0.66f, FontStyle.Bold);

        _gameOverBlackText = MakeLabelAt(panel, "BlackScore", "",
            36, WabiInk, TextAnchor.MiddleCenter, 0.15f, 0.55f, 0.85f, 0.61f, FontStyle.Normal, autoSize: true);
        MakeLabelAt(panel, "VsKanji", "対",
            32, WabiSeal, TextAnchor.MiddleCenter, 0.45f, 0.50f, 0.55f, 0.54f, FontStyle.Bold);
        _gameOverWhiteText = MakeLabelAt(panel, "WhiteScore", "",
            36, WabiInk, TextAnchor.MiddleCenter, 0.15f, 0.42f, 0.85f, 0.48f, FontStyle.Normal, autoSize: true);

        // Hairline
        MakeRect(panel, "Rule2", WabiLine, 0.20f, 0.39f, 0.80f, 0.392f);

        // Mission reveal
        MakeLabelAt(panel, "MissionsHeader", "─  秘 匿 指 令  ─",
            26, WabiSeal, TextAnchor.MiddleCenter, 0.10f, 0.34f, 0.90f, 0.38f, FontStyle.Bold);
        _localizedTexts.Add((
            FindByNameInChild(panel, "MissionsHeader").GetComponentInChildren<Text>(),
            () => "─  秘 匿 指 令  ─"));

        _blackMissionReveal = MakeLabelAt(panel, "BlackMission", "",
            30, WabiInk, TextAnchor.MiddleCenter, 0.08f, 0.27f, 0.92f, 0.33f, FontStyle.Normal, autoSize: true);
        MakeRect(panel, "MidRule", WabiLine, 0.40f, 0.255f, 0.60f, 0.257f);
        _whiteMissionReveal = MakeLabelAt(panel, "WhiteMission", "",
            30, WabiInk, TextAnchor.MiddleCenter, 0.08f, 0.19f, 0.92f, 0.25f, FontStyle.Normal, autoSize: true);

        MakeRect(panel, "Rule3", WabiLine, 0.20f, 0.16f, 0.80f, 0.162f);

        // Two text actions
        BuildWabiTextButton(panel, "play_again", 0.08f, 0.08f, 0.49f, 0.14f, OnReplayClicked);
        MakeLabelAt(panel, "MidDot", "・",
            48, WabiSeal, TextAnchor.MiddleCenter, 0.49f, 0.08f, 0.51f, 0.14f);
        BuildWabiTextButton(panel, "menu",       0.51f, 0.08f, 0.92f, 0.14f, OnMainMenuClicked);

        MakeLabelAt(panel, "Footer", "─  令和 八年  ─",
            20, WabiLine, TextAnchor.MiddleCenter, 0.10f, 0.04f, 0.90f, 0.07f);

        panel.SetActive(false);
        return panel;
    }

    // ═══════════════════════════════════════════════════════════════════
    // NEON — synthwave / arcade
    //
    // Deep navy bg with stacked radial glows in pink + cyan. Bottom horizon
    // (receding stripes implying perspective). Title rendered with chrome
    // effect: 3 stacked copies in pink/cyan/white at different offsets.
    // Concentric ring decoration. Bold geometric.
    // ═══════════════════════════════════════════════════════════════════

    static readonly Color NeonBg     = new Color(0.043f, 0.027f, 0.114f);
    static readonly Color NeonBgDeep = new Color(0.078f, 0.055f, 0.180f);
    static readonly Color NeonPink   = new Color(1.000f, 0.235f, 0.706f);
    static readonly Color NeonCyan   = new Color(0.235f, 0.961f, 1.000f);
    static readonly Color NeonGold   = new Color(1.000f, 0.851f, 0.353f);
    static readonly Color NeonText   = new Color(0.980f, 0.973f, 1.000f);

    GameObject BuildModeSelectPanel_Neon(GameObject parent)
    {
        var panel = MakePanel(parent, "ModeSelectPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = NeonBg;

        // Big radial glow upper-right (pink) and upper-left (cyan)
        MakeSpriteRect(panel, "GlowPink", ThemeSprites.RadialGlow,
            new Color(NeonPink.r, NeonPink.g, NeonPink.b, 0.45f),
            0.40f, 0.50f, 1.20f, 1.30f);
        MakeSpriteRect(panel, "GlowCyan", ThemeSprites.RadialGlow,
            new Color(NeonCyan.r, NeonCyan.g, NeonCyan.b, 0.30f),
            -0.20f, 0.50f, 0.50f, 1.30f);

        // Horizon stripes: receding lines at bottom
        BuildNeonHorizon(panel, 0f, 0f, 1f, 0.25f);

        // Big concentric ring backdrop behind title
        MakeSpriteRect(panel, "Concentric", ThemeSprites.Concentric,
            new Color(NeonCyan.r, NeonCyan.g, NeonCyan.b, 0.12f),
            0.10f, 0.55f, 0.90f, 0.95f);

        // Title — 3 stacked copies for chromatic-aberration glow
        var title1 = MakeLabelAt(panel, "TitleA", Loc.Get("title").ToUpperInvariant(),
            220, NeonPink, TextAnchor.MiddleCenter, 0.05f, 0.66f, 0.95f, 0.88f, FontStyle.Bold, autoSize: true);
        var title2 = MakeLabelAt(panel, "TitleB", Loc.Get("title").ToUpperInvariant(),
            220, NeonCyan, TextAnchor.MiddleCenter, 0.06f, 0.66f, 0.96f, 0.88f, FontStyle.Bold, autoSize: true);
        var title3 = MakeLabelAt(panel, "TitleC", Loc.Get("title").ToUpperInvariant(),
            220, NeonText, TextAnchor.MiddleCenter, 0.055f, 0.665f, 0.955f, 0.885f, FontStyle.Bold, autoSize: true);
        _localizedTexts.Add((title1, () => Loc.Get("title").ToUpperInvariant()));
        _localizedTexts.Add((title2, () => Loc.Get("title").ToUpperInvariant()));
        _localizedTexts.Add((title3, () => Loc.Get("title").ToUpperInvariant()));

        // Subtitle
        MakeLabelAt(panel, "Sub", "▸  STRATEGIC  ·  REVERSAL  ·  2026  ◂",
            28, NeonCyan, TextAnchor.MiddleCenter, 0.10f, 0.62f, 0.90f, 0.66f, FontStyle.Bold);

        // Stats badge
        MakeRect(panel, "StatBg", new Color(NeonBgDeep.r, NeonBgDeep.g, NeonBgDeep.b, 0.85f),
            0.20f, 0.55f, 0.80f, 0.605f);
        MakeRect(panel, "StatTop", NeonPink, 0.20f, 0.605f, 0.80f, 0.609f);
        MakeRect(panel, "StatBot", NeonCyan, 0.20f, 0.546f, 0.80f, 0.550f);
        MakeLabelAt(panel, "Stats", "GAMES 24    WINS 14    RATE 58%",
            28, NeonText, TextAnchor.MiddleCenter, 0.20f, 0.55f, 0.80f, 0.60f, FontStyle.Bold);

        // Mode buttons — chrome neon
        BuildNeonButton(panel, "vs_ai",    0.10f, 0.40f, 0.90f, 0.51f, NeonPink, () => StartGame(true));
        BuildNeonButton(panel, "vs_human", 0.10f, 0.27f, 0.90f, 0.38f, NeonCyan, () => StartGame(false));
        BuildNeonButton(panel, "records",  0.30f, 0.18f, 0.70f, 0.25f, NeonGold, ShowRecords);

        // Lang toggle — small chip top right
        BuildNeonChip(panel, "lang_mode", 0.78f, 0.93f, 0.95f, 0.98f, NeonPink, OnLangToggleClicked);

        // Footer
        MakeLabelAt(panel, "Footer", "▸▸  PRESS START  ◂◂",
            24, NeonGold, TextAnchor.MiddleCenter, 0.10f, 0.04f, 0.90f, 0.08f, FontStyle.Bold);

        return panel;
    }

    void BuildNeonHorizon(GameObject parent, float xMin, float yMin, float xMax, float yMax)
    {
        // Vertical lines fanning to a vanishing point (perspective)
        int verticals = 12;
        for (int i = 0; i <= verticals; i++)
        {
            float t = (float)i / verticals;
            float bottomX = Mathf.Lerp(xMin - 0.2f, xMax + 0.2f, t);
            float topX    = Mathf.Lerp(xMin + 0.42f, xMax - 0.42f, t);
            BuildPerspectiveLine(parent, bottomX, yMin, topX, yMax, NeonPink);
        }
        // Horizontal receding stripes
        int horiz = 6;
        for (int i = 0; i < horiz; i++)
        {
            float t = (float)i / horiz;
            float y = yMin + t * (yMax - yMin) * 0.95f;
            float thickness = Mathf.Lerp(0.005f, 0.001f, t); // thicker near bottom
            float xPad = Mathf.Lerp(0f, 0.42f, t);
            MakeRect(parent, "horiz" + i, new Color(NeonCyan.r, NeonCyan.g, NeonCyan.b, 0.55f),
                xMin + xPad, y, xMax - xPad, y + thickness);
        }
        // Horizon line itself
        MakeRect(parent, "horizon", NeonPink, xMin, yMax, xMax, yMax + 0.003f);
    }

    void BuildPerspectiveLine(GameObject parent, float x0, float y0, float x1, float y1, Color color)
    {
        // Approximate a slanted line using a thin rectangle that we rotate.
        var go = MakePanel(parent, "pline");
        var rt = go.GetComponent<RectTransform>();
        // Convert anchor coords to a "centered" rect.
        float cx = (x0 + x1) * 0.5f;
        float cy = (y0 + y1) * 0.5f;
        float dx = x1 - x0;
        float dy = y1 - y0;
        float lenAnchor = Mathf.Sqrt(dx * dx + dy * dy);
        rt.anchorMin = new Vector2(cx, cy);
        rt.anchorMax = new Vector2(cx, cy);
        // Reference resolution 1080x1920; convert anchor length to pixels.
        float pixLen = lenAnchor * 1920f; // ~portrait height
        rt.sizeDelta = new Vector2(pixLen, 2f);
        float angle = Mathf.Atan2(dy * 1920f, dx * 1080f) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0, 0, angle);
        var img = go.AddComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0.45f);
        img.raycastTarget = false;
    }

    void BuildNeonButton(GameObject parent, string key,
        float xMin, float yMin, float xMax, float yMax,
        Color accent, UnityEngine.Events.UnityAction onClick)
    {
        // Glow halo behind
        MakeSpriteRect(parent, key + "_glow", ThemeSprites.RadialGlow,
            new Color(accent.r, accent.g, accent.b, 0.35f),
            xMin - 0.04f, yMin - 0.05f, xMax + 0.04f, yMax + 0.05f);

        // Frame (neon border): outer ring rect of 2px lines around the inner
        var frame = MakeRect(parent, key + "_frame", accent, xMin, yMin, xMax, yMax);

        // Inner darker rect
        var go = new GameObject("Btn_" + key);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin + 0.005f, yMin + 0.008f);
        rt.anchorMax = new Vector2(xMax - 0.005f, yMax - 0.008f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(NeonBgDeep.r, NeonBgDeep.g, NeonBgDeep.b, 0.95f);
        var btn = go.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);

        var label = MakeText(go, Loc.Get(key).ToUpperInvariant(), 64, NeonText, TextAnchor.MiddleCenter);
        label.fontStyle = FontStyle.Bold;
        _localizedTexts.Add((label, () => Loc.Get(key).ToUpperInvariant()));

        // Decorative side chevrons
        MakeLabelAt(go, "ChevL", "▸",
            48, accent, TextAnchor.MiddleLeft, 0.02f, 0.20f, 0.12f, 0.80f, FontStyle.Bold);
        MakeLabelAt(go, "ChevR", "◂",
            48, accent, TextAnchor.MiddleRight, 0.88f, 0.20f, 0.98f, 0.80f, FontStyle.Bold);
    }

    void BuildNeonChip(GameObject parent, string key,
        float xMin, float yMin, float xMax, float yMax,
        Color accent, UnityEngine.Events.UnityAction onClick)
    {
        MakeRect(parent, key + "_frame", accent, xMin, yMin, xMax, yMax);
        var go = new GameObject("Btn_" + key);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin + 0.004f, yMin + 0.005f);
        rt.anchorMax = new Vector2(xMax - 0.004f, yMax - 0.005f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(NeonBg.r, NeonBg.g, NeonBg.b, 0.95f);
        var btn = go.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);
        var label = MakeText(go, Loc.Get(key), 28, accent, TextAnchor.MiddleCenter);
        label.fontStyle = FontStyle.Bold;
        _localizedTexts.Add((label, () => Loc.Get(key)));
    }

    GameObject BuildGameOverPanel_Neon(GameObject parent)
    {
        var panel = MakePanel(parent, "GameOverPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = NeonBg;

        // Glows
        MakeSpriteRect(panel, "GlowPink", ThemeSprites.RadialGlow,
            new Color(NeonPink.r, NeonPink.g, NeonPink.b, 0.55f),
            0.30f, 0.55f, 1.20f, 1.30f);
        MakeSpriteRect(panel, "GlowCyan", ThemeSprites.RadialGlow,
            new Color(NeonCyan.r, NeonCyan.g, NeonCyan.b, 0.30f),
            -0.20f, 0.30f, 0.55f, 1.05f);

        BuildNeonHorizon(panel, 0f, 0f, 1f, 0.20f);

        // Big concentric backdrop
        MakeSpriteRect(panel, "Concentric", ThemeSprites.Concentric,
            new Color(NeonGold.r, NeonGold.g, NeonGold.b, 0.10f),
            0.05f, 0.65f, 0.95f, 0.97f);

        // Header text
        MakeLabelAt(panel, "Hdr", "▸▸  GAME OVER  ◂◂",
            34, NeonGold, TextAnchor.MiddleCenter, 0.10f, 0.93f, 0.90f, 0.97f, FontStyle.Bold);

        // Winner — 3 stacked copies
        MakeLabelAt(panel, "WinA", "WINNER",
            32, NeonCyan, TextAnchor.MiddleCenter, 0.10f, 0.86f, 0.90f, 0.90f, FontStyle.Bold);
        var w1 = MakeLabelAt(panel, "WinShadowP", "", 160, NeonPink,
            TextAnchor.MiddleCenter, 0.045f, 0.71f, 0.945f, 0.85f, FontStyle.Bold, autoSize: true);
        var w2 = MakeLabelAt(panel, "WinShadowC", "", 160, NeonCyan,
            TextAnchor.MiddleCenter, 0.055f, 0.71f, 0.955f, 0.85f, FontStyle.Bold, autoSize: true);
        _winnerText = MakeLabelAt(panel, "Winner", "", 160, NeonText,
            TextAnchor.MiddleCenter, 0.05f, 0.715f, 0.95f, 0.855f, FontStyle.Bold, autoSize: true);
        _neonWinShadowPink = w1; _neonWinShadowCyan = w2;

        // Score panel
        MakeRect(panel, "ScoreFrameTop", NeonPink, 0.06f, 0.66f, 0.94f, 0.665f);
        MakeRect(panel, "ScoreFrameBot", NeonCyan, 0.06f, 0.49f, 0.94f, 0.495f);
        MakeRect(panel, "ScoreBg", new Color(NeonBgDeep.r, NeonBgDeep.g, NeonBgDeep.b, 0.85f),
            0.06f, 0.495f, 0.94f, 0.66f);
        MakeLabelAt(panel, "ScoreLabel", "▸  SCORE  ·  STONES + TILES + MISSION  ◂",
            22, NeonGold, TextAnchor.MiddleCenter, 0.06f, 0.625f, 0.94f, 0.66f, FontStyle.Bold);

        _gameOverBlackText = MakeLabelAt(panel, "BlackScore", "",
            38, NeonText, TextAnchor.MiddleCenter, 0.08f, 0.56f, 0.92f, 0.62f, FontStyle.Bold, autoSize: true);
        _gameOverWhiteText = MakeLabelAt(panel, "WhiteScore", "",
            38, NeonText, TextAnchor.MiddleCenter, 0.08f, 0.50f, 0.92f, 0.56f, FontStyle.Bold, autoSize: true);

        // Mission section
        MakeLabelAt(panel, "MissionsHeader", "▸  MISSION  REVEAL  ◂",
            26, NeonPink, TextAnchor.MiddleCenter, 0.06f, 0.43f, 0.94f, 0.47f, FontStyle.Bold);
        _localizedTexts.Add((
            FindByNameInChild(panel, "MissionsHeader").GetComponentInChildren<Text>(),
            () => "▸  MISSION  REVEAL  ◂"));

        var bm = MakeRect(panel, "BlackMission", new Color(NeonBgDeep.r, NeonBgDeep.g, NeonBgDeep.b, 0.7f),
            0.06f, 0.34f, 0.94f, 0.42f);
        MakeRect(bm, "bmL", NeonPink, 0f, 0f, 0.005f, 1f);
        _blackMissionReveal = MakeText(bm, "", 30, NeonText, TextAnchor.MiddleCenter, true);
        _blackMissionReveal.fontStyle = FontStyle.Bold;

        var wm = MakeRect(panel, "WhiteMission", new Color(NeonBgDeep.r, NeonBgDeep.g, NeonBgDeep.b, 0.7f),
            0.06f, 0.25f, 0.94f, 0.33f);
        MakeRect(wm, "wmL", NeonCyan, 0f, 0f, 0.005f, 1f);
        _whiteMissionReveal = MakeText(wm, "", 30, NeonText, TextAnchor.MiddleCenter, true);
        _whiteMissionReveal.fontStyle = FontStyle.Bold;

        BuildNeonButton(panel, "play_again", 0.06f, 0.10f, 0.49f, 0.20f, NeonPink, OnReplayClicked);
        BuildNeonButton(panel, "menu",       0.51f, 0.10f, 0.94f, 0.20f, NeonCyan, OnMainMenuClicked);

        MakeLabelAt(panel, "Footer", "▸▸  CONTINUE  ?  ◂◂",
            24, NeonGold, TextAnchor.MiddleCenter, 0.10f, 0.04f, 0.90f, 0.08f, FontStyle.Bold);

        panel.SetActive(false);
        return panel;
    }

    Text _neonWinShadowPink, _neonWinShadowCyan;

    // ═══════════════════════════════════════════════════════════════════
    // PIECES — board-forward identity
    //
    // The board's own visual language is the design: 8×8 piece-pattern tile
    // as background watermark, mode buttons styled as game cells, dark
    // green felt palette with cream + gold accents. UI is the GAME.
    // ═══════════════════════════════════════════════════════════════════

    static readonly Color PFelt    = new Color(0.043f, 0.157f, 0.090f);
    static readonly Color PFeltLt  = new Color(0.090f, 0.220f, 0.137f);
    static readonly Color PCream   = new Color(0.984f, 0.965f, 0.910f);
    static readonly Color PInk     = new Color(0.043f, 0.043f, 0.043f);
    static readonly Color PGold    = new Color(0.949f, 0.769f, 0.282f);
    static readonly Color PWhitePc = new Color(0.965f, 0.949f, 0.929f);
    static readonly Color PRed     = new Color(0.706f, 0.157f, 0.137f);

    GameObject BuildModeSelectPanel_Pieces(GameObject parent)
    {
        var panel = MakePanel(parent, "ModeSelectPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = PFelt;

        // Piece pattern wallpaper, dim
        MakeTiledSprite(panel, "PieceBg", ThemeSprites.PiecePattern,
            new Color(1f, 1f, 1f, 0.18f), 0f, 0f, 1f, 1f);
        // Vignette
        MakeSpriteRect(panel, "Vignette", ThemeSprites.Vignette,
            new Color(0f, 0f, 0f, 0.7f), -0.1f, -0.1f, 1.1f, 1.1f);

        // Cream "card" with gold border
        MakeRect(panel, "CardBorder", PGold,    0.05f, 0.06f, 0.95f, 0.94f);
        var card = MakeRect(panel, "Card", PCream, 0.058f, 0.067f, 0.942f, 0.933f);

        // Title piece: huge black piece glyph (left), white piece (right),
        // pulled into corners so the title fits cleanly between them.
        MakeSpriteRect(card, "PieceBlack", ThemeSprites.Circle, PInk,
            0.04f, 0.78f, 0.20f, 0.92f);
        MakeSpriteRect(card, "PieceBlackHl", ThemeSprites.Circle,
            new Color(1f, 1f, 1f, 0.18f),
            0.07f, 0.84f, 0.13f, 0.90f);
        MakeSpriteRect(card, "PieceWhite", ThemeSprites.Circle, PWhitePc,
            0.80f, 0.78f, 0.96f, 0.92f);
        MakeSpriteRect(card, "PieceRing",  ThemeSprites.RingThin,
            new Color(PInk.r, PInk.g, PInk.b, 0.5f),
            0.80f, 0.78f, 0.96f, 0.92f);

        // Title between the two pieces — proper margin so glyphs don't clip
        MakeLabelAt(card, "Title", Loc.Get("title").ToUpperInvariant(),
            120, PInk, TextAnchor.MiddleCenter, 0.22f, 0.76f, 0.78f, 0.94f, FontStyle.Bold, autoSize: true);
        _localizedTexts.Add((
            FindByNameInChild(card, "Title").GetComponentInChildren<Text>(),
            () => Loc.Get("title").ToUpperInvariant()));
        MakeLabelAt(card, "Sub", "·  SECRET  MISSIONS  ·",
            26, PRed, TextAnchor.MiddleCenter, 0.10f, 0.69f, 0.90f, 0.73f, FontStyle.Italic);

        // Hairline rule
        MakeRect(card, "Rule1", PGold, 0.10f, 0.673f, 0.90f, 0.677f);

        // Mode buttons styled as game cells (colored cell + a piece icon)
        BuildPiecesCellButton(card, "vs_ai",   0.10f, 0.55f, 0.90f, 0.66f, "▶", "play against the engine", PInk,    PCream, () => StartGame(true));
        BuildPiecesCellButton(card, "vs_human",0.10f, 0.43f, 0.90f, 0.54f, "👥", "two-player local",         PRed,    PCream, () => StartGame(false));
        BuildPiecesCellButton(card, "records", 0.10f, 0.31f, 0.90f, 0.42f, "▦", "history of past matches",   PFelt,   PCream, ShowRecords);

        // Stats strip
        MakeRect(card, "StatsBg", PFeltLt, 0.10f, 0.21f, 0.90f, 0.30f);
        MakeRect(card, "StatsEdge", PGold, 0.10f, 0.30f, 0.90f, 0.302f);
        MakeLabelAt(card, "Stats", "GAMES  24    ·    WINS  14    ·    RATE  58 %",
            28, PCream, TextAnchor.MiddleCenter, 0.10f, 0.21f, 0.90f, 0.30f, FontStyle.Bold);

        // Lang toggle (small) + version
        BuildPiecesTextButton(card, "lang_mode", 0.65f, 0.10f, 0.90f, 0.18f, OnLangToggleClicked, PRed);
        MakeLabelAt(card, "Version", "OTHELLO  ·  V.1.0",
            22, PFelt, TextAnchor.MiddleLeft, 0.10f, 0.10f, 0.55f, 0.18f, FontStyle.Bold);

        // Bottom hairline
        MakeRect(card, "Rule2", PGold, 0.10f, 0.082f, 0.90f, 0.085f);
        MakeLabelAt(card, "Footer", "·  PRESS  ANY  CELL  TO  BEGIN  ·",
            22, PFelt, TextAnchor.MiddleCenter, 0.10f, 0.04f, 0.90f, 0.08f, FontStyle.Italic);

        return panel;
    }

    void BuildPiecesCellButton(GameObject parent, string key,
        float xMin, float yMin, float xMax, float yMax,
        string icon, string desc, Color accent, Color text,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + key);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = accent;
        var btn = go.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);

        // Inner cell border (cream)
        MakeRect(go, "ibT", text, 0.02f, 0.94f, 0.98f, 0.97f);
        MakeRect(go, "ibB", text, 0.02f, 0.03f, 0.98f, 0.06f);

        // Icon disc
        var ic = MakePanel(go, "Ico");
        SetAnchor(ic, 0.04f, 0.20f, 0.18f, 0.80f);
        var icBg = ic.AddComponent<Image>();
        icBg.color = new Color(text.r, text.g, text.b, 0.18f);
        icBg.sprite = ThemeSprites.Circle;
        MakeText(ic, icon, 56, text, TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;

        // Mode name
        var nameGO = MakePanel(go, "Name");
        SetAnchor(nameGO, 0.20f, 0.40f, 0.85f, 0.95f);
        var name = MakeText(nameGO, Loc.Get(key).ToUpperInvariant(), 50, text, TextAnchor.MiddleLeft);
        name.fontStyle = FontStyle.Bold;
        _localizedTexts.Add((name, () => Loc.Get(key).ToUpperInvariant()));

        // Description
        var dGO = MakePanel(go, "Desc");
        SetAnchor(dGO, 0.20f, 0.05f, 0.85f, 0.40f);
        MakeText(dGO, desc, 22, new Color(text.r, text.g, text.b, 0.7f), TextAnchor.MiddleLeft);

        // Right arrow
        var arrGO = MakePanel(go, "Arr");
        SetAnchor(arrGO, 0.85f, 0.20f, 0.97f, 0.80f);
        MakeText(arrGO, "→", 56, text, TextAnchor.MiddleRight);
    }

    void BuildPiecesTextButton(GameObject parent, string key,
        float xMin, float yMin, float xMax, float yMax,
        UnityEngine.Events.UnityAction onClick, Color color)
    {
        var go = new GameObject("Btn_" + key);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var btn = go.AddComponent<Button>();
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);
        var label = MakeText(go, Loc.Get(key), 28, color, TextAnchor.MiddleRight);
        label.fontStyle = FontStyle.Bold;
        _localizedTexts.Add((label, () => Loc.Get(key)));
    }

    GameObject BuildGameOverPanel_Pieces(GameObject parent)
    {
        var panel = MakePanel(parent, "GameOverPanel");
        SetStretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = PFelt;

        MakeTiledSprite(panel, "PieceBg", ThemeSprites.PiecePattern,
            new Color(1f, 1f, 1f, 0.18f), 0f, 0f, 1f, 1f);
        MakeSpriteRect(panel, "Vignette", ThemeSprites.Vignette,
            new Color(0f, 0f, 0f, 0.75f), -0.1f, -0.1f, 1.1f, 1.1f);

        MakeRect(panel, "CardBorder", PGold,  0.04f, 0.05f, 0.96f, 0.95f);
        var card = MakeRect(panel, "Card", PCream, 0.048f, 0.057f, 0.952f, 0.943f);

        // Decorative trophy disc
        MakeSpriteRect(card, "TrophyGlow", ThemeSprites.RadialGlow,
            new Color(PGold.r, PGold.g, PGold.b, 0.55f),
            0.30f, 0.84f, 0.70f, 0.99f);
        MakeSpriteRect(card, "TrophyDisc", ThemeSprites.Circle, PGold,
            0.42f, 0.86f, 0.58f, 0.95f);
        MakeLabelAt(card, "TrophyKanji", "勝",
            58, PInk, TextAnchor.MiddleCenter, 0.42f, 0.86f, 0.58f, 0.95f, FontStyle.Bold);

        _winnerText = MakeLabelAt(card, "Winner", "",
            120, PInk, TextAnchor.MiddleCenter, 0.05f, 0.74f, 0.95f, 0.85f, FontStyle.Bold, autoSize: true);
        MakeLabelAt(card, "WinSub", "—  THE  MATCH  IS  CONCLUDED  —",
            22, PRed, TextAnchor.MiddleCenter, 0.10f, 0.71f, 0.90f, 0.74f, FontStyle.Italic);

        MakeRect(card, "Rule1", PGold, 0.10f, 0.695f, 0.90f, 0.698f);

        // Score panel — two cell-styled rows
        BuildPiecesScoreRow(card, "BlackScoreRow", true,  0.08f, 0.58f, 0.92f, 0.69f, out _gameOverBlackText);
        BuildPiecesScoreRow(card, "WhiteScoreRow", false, 0.08f, 0.46f, 0.92f, 0.57f, out _gameOverWhiteText);

        MakeRect(card, "Rule2", PGold, 0.10f, 0.435f, 0.90f, 0.438f);

        MakeLabelAt(card, "MissionsHeader", "·  HIDDEN  MISSIONS  ·",
            24, PRed, TextAnchor.MiddleCenter, 0.10f, 0.39f, 0.90f, 0.43f, FontStyle.Bold);
        _localizedTexts.Add((
            FindByNameInChild(card, "MissionsHeader").GetComponentInChildren<Text>(),
            () => "·  HIDDEN  MISSIONS  ·"));

        var bm = MakeRect(card, "BlackMission", PFeltLt, 0.08f, 0.30f, 0.92f, 0.38f);
        MakeRect(bm, "bmL", PGold, 0f, 0f, 0.012f, 1f);
        _blackMissionReveal = MakeText(bm, "", 28, PCream, TextAnchor.MiddleCenter, true);

        var wm = MakeRect(card, "WhiteMission", PFeltLt, 0.08f, 0.21f, 0.92f, 0.29f);
        MakeRect(wm, "wmL", PRed, 0f, 0f, 0.012f, 1f);
        _whiteMissionReveal = MakeText(wm, "", 28, PCream, TextAnchor.MiddleCenter, true);

        // Buttons styled as cells
        BuildPiecesCellButton(card, "play_again", 0.08f, 0.10f, 0.49f, 0.19f, "↻", "another match", PRed, PCream, OnReplayClicked);
        BuildPiecesCellButton(card, "menu",       0.51f, 0.10f, 0.92f, 0.19f, "⌂", "back to title",  PFelt, PCream, OnMainMenuClicked);

        MakeRect(card, "Rule3", PGold, 0.10f, 0.082f, 0.90f, 0.085f);
        MakeLabelAt(card, "Footer", "OTHELLO  ·  THE  STRATEGIC  GAMBIT",
            22, PFelt, TextAnchor.MiddleCenter, 0.10f, 0.04f, 0.90f, 0.08f, FontStyle.Bold);

        panel.SetActive(false);
        return panel;
    }

    void BuildPiecesScoreRow(GameObject parent, string name, bool isBlack,
        float xMin, float yMin, float xMax, float yMax, out Text textOut)
    {
        var row = MakeRect(parent, name, isBlack ? PInk : PWhitePc, xMin, yMin, xMax, yMax);
        // Piece glyph at left
        var p = MakeSpriteRect(row, "Piece", ThemeSprites.Circle,
            isBlack ? PWhitePc : PInk,
            0.02f, 0.20f, 0.18f, 0.80f);
        MakeSpriteRect(p, "Hl", ThemeSprites.Circle,
            new Color(isBlack ? PWhitePc.r : 1f, isBlack ? PWhitePc.g : 1f, isBlack ? PWhitePc.b : 1f, 0.18f),
            0.15f, 0.55f, 0.45f, 0.85f);

        // Label "BLACK" / "WHITE"
        MakeLabelAt(row, "Lbl", isBlack ? "BLACK" : "WHITE",
            24, isBlack ? PWhitePc : PInk, TextAnchor.MiddleLeft,
            0.20f, 0.55f, 0.50f, 0.95f, FontStyle.Bold);

        // Score text spans the row
        textOut = MakeLabelAt(row, "Txt", "",
            34, isBlack ? PWhitePc : PInk, TextAnchor.MiddleLeft,
            0.20f, 0.05f, 0.95f, 0.55f, FontStyle.Bold, autoSize: true);
    }
}
