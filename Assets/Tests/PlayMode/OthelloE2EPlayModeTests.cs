using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// PlayMode E2E + functional tests. Drives real UI and real game logic.
// Run via: .\unity.ps1 playmode
public class OthelloE2EPlayModeTests
{
    static readonly string ArtifactsDir =
        Path.Combine(Application.persistentDataPath, "TestArtifacts");

    // Capture latest events for assertions.
    TurnChangedEvent _lastTurn;
    bool _gotTurn;
    PassTurnEvent _lastPass;
    bool _gotPass;
    GameOverEvent _lastGameOver;
    bool _gotGameOver;
    int _passCount;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Directory.CreateDirectory(ArtifactsDir);
        Debug.Log("[E2E] Artifacts dir: " + ArtifactsDir);
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _gotTurn = _gotPass = _gotGameOver = false;
        _passCount = 0;

        EventBus.Subscribe<TurnChangedEvent>(OnTurn);
        EventBus.Subscribe<PassTurnEvent>(OnPass);
        EventBus.Subscribe<GameOverEvent>(OnGameOver);

        // Skip the reveal animation in tests so assertions don't have to
        // wait several seconds for the per-stone count-up.
        OthelloUIManager.RevealStonePlaceDelay = 0f;
        OthelloUIManager.RevealPhaseGap        = 0f;
        OthelloUIManager.RevealClearPause      = 0f;

        SceneManager.LoadScene("Game", LoadSceneMode.Single);
        yield return null;
        for (int i = 0; i < 8; i++) yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        EventBus.Unsubscribe<TurnChangedEvent>(OnTurn);
        EventBus.Unsubscribe<PassTurnEvent>(OnPass);
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);

        yield return null;
        CaptureScreenshot();
    }

    void OnTurn(TurnChangedEvent e) { _lastTurn = e; _gotTurn = true; }
    void OnPass(PassTurnEvent e)    { _lastPass = e; _gotPass = true; _passCount++; }
    void OnGameOver(GameOverEvent e) { _lastGameOver = e; _gotGameOver = true; }

    // ── Helpers ──────────────────────────────────────────────────────────

    static GameObject FindByName(string name) =>
        Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(g => g.name == name && g.scene.IsValid());

    static GameObject[] FindAllByName(string name) =>
        Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(g => g.name == name && g.scene.IsValid()).ToArray();

    static bool ActiveInHierarchy(GameObject go) => go != null && go.activeInHierarchy;

    static void Click(GameObject go) => go.GetComponent<Button>().onClick.Invoke();

    IEnumerator StartVsHuman()
    {
        Click(FindByName("Btn_vs_human"));
        for (int i = 0; i < 6; i++) yield return null;
    }

    IEnumerator StartVsAi()
    {
        Click(FindByName("Btn_vs_ai"));
        for (int i = 0; i < 6; i++) yield return null;
    }

    static OthelloBoard GetBoard()
    {
        var mgr = OthelloGameManager.Instance;
        var fld = typeof(OthelloGameManager).GetField("_board",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (OthelloBoard)fld.GetValue(mgr);
    }

    static int GetCurrentPlayer()
    {
        var fld = typeof(OthelloGameManager).GetField("_currentPlayer",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (int)fld.GetValue(OthelloGameManager.Instance);
    }

    static void SetBoardState(int[,] state)
    {
        var board = GetBoard();
        var fld = typeof(OthelloBoard).GetField("_board",
            BindingFlags.NonPublic | BindingFlags.Instance);
        fld.SetValue(board, state);
    }

    static void Place(int row, int col) =>
        EventBus.Publish(new CellClickedEvent { row = row, col = col });

    static int[,] EmptyBoard()
    {
        var b = new int[8, 8];
        return b;
    }

    void CaptureScreenshot()
    {
        var cam = Camera.main;
        if (cam == null) return;

        const int W = 1080;
        const int H = 1920;
        var canvases = Object.FindObjectsOfType<Canvas>();
        var saved = new (Canvas c, RenderMode m, Camera cam, float plane)[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            saved[i] = (c, c.renderMode, c.worldCamera, c.planeDistance);
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1f;
            }
        }
        Canvas.ForceUpdateCanvases();

        var rt = new RenderTexture(W, H, 24);
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;

        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;
        cam.targetTexture = prevTarget;

        for (int i = 0; i < saved.Length; i++)
        {
            if (saved[i].c == null) continue;
            saved[i].c.renderMode = saved[i].m;
            saved[i].c.worldCamera = saved[i].cam;
            saved[i].c.planeDistance = saved[i].plane;
        }

        var bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);

        var name = TestContext.CurrentContext.Test.MethodName ?? "test";
        File.WriteAllBytes(Path.Combine(ArtifactsDir, $"{name}.png"), bytes);
    }

    // ── UI presence (kept from earlier) ──────────────────────────────────

    [UnityTest]
    public IEnumerator TitleScreen_ShowsLanguageButton()
    {
        var lang = FindByName("Btn_lang_mode");
        Assert.That(lang, Is.Not.Null);
        Assert.That(ActiveInHierarchy(lang), Is.True);

        var text = lang.GetComponentInChildren<Text>();
        Assert.That(text.text, Is.EqualTo("日本語").Or.EqualTo("English"));
        yield break;
    }

    [UnityTest]
    public IEnumerator TopBar_DoesNotContainLanguageButton()
    {
        Assert.That(FindAllByName("Btn_lang_btn"), Is.Empty);
        yield break;
    }

    // ── Functional: piece placement, flips, scores ───────────────────────

    [UnityTest]
    public IEnumerator HumanPlay_PlacesPieceAndFlipsAndUpdatesScore()
    {
        yield return StartVsHuman();
        var b = GetBoard();

        // Initial: 2 black, 2 white.
        Assert.That(b.GetScore(1), Is.EqualTo(2));
        Assert.That(b.GetScore(2), Is.EqualTo(2));
        Assert.That(GetCurrentPlayer(), Is.EqualTo(1));

        // Black plays (2,3) — flips (3,3) (white at (3,3)).
        Place(2, 3);
        yield return null;
        yield return null;

        Assert.That(b.GetScore(1), Is.EqualTo(4),
            "After black plays (2,3), should have 4 black: orig (3,4)+(4,3) + new (2,3) + flipped (3,3).");
        Assert.That(b.GetScore(2), Is.EqualTo(1),
            "White should drop to 1 (only (4,4) left).");
        Assert.That(GetCurrentPlayer(), Is.EqualTo(2),
            "Turn must switch to white.");
        Assert.That(_lastTurn.playerColor, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator InvalidClick_DoesNothing()
    {
        yield return StartVsHuman();
        var b = GetBoard();
        var blackBefore = b.GetScore(1);
        var whiteBefore = b.GetScore(2);
        var playerBefore = GetCurrentPlayer();

        // (0,0) is empty but not a valid opening move.
        Place(0, 0);
        yield return null;
        yield return null;

        Assert.That(b.GetScore(1), Is.EqualTo(blackBefore));
        Assert.That(b.GetScore(2), Is.EqualTo(whiteBefore));
        Assert.That(GetCurrentPlayer(), Is.EqualTo(playerBefore));
    }

    [UnityTest]
    public IEnumerator AlreadyOccupiedCell_IsIgnored()
    {
        yield return StartVsHuman();
        var b = GetBoard();
        var before = b.GetScore(1) + b.GetScore(2);

        Place(3, 3); // already white
        yield return null;
        yield return null;

        Assert.That(b.GetScore(1) + b.GetScore(2), Is.EqualTo(before),
            "Clicking an occupied cell must not change the board.");
    }

    [UnityTest]
    public IEnumerator BoardInvariant_TotalAlwaysAtMost64()
    {
        yield return StartVsHuman();
        var b = GetBoard();

        // Play 10 deterministic moves, check invariant after each.
        var moves = new (int r, int c)[]
        {
            (2,3),(2,2),(2,4),(2,5),(3,5),(4,5),(5,5),(5,4),(5,3),(5,2)
        };
        foreach (var (r, c) in moves)
        {
            var validMoves = b.GetValidMoves(GetCurrentPlayer());
            if (!validMoves.Contains(new Vector2Int(r, c))) continue;
            Place(r, c);
            yield return null;

            int total = b.GetScore(1) + b.GetScore(2);
            int empty = 64 - total;
            Assert.That(total, Is.LessThanOrEqualTo(64));
            Assert.That(empty, Is.GreaterThanOrEqualTo(0));
        }
    }

    // ── Functional: AI turn ──────────────────────────────────────────────

    [UnityTest]
    public IEnumerator VsAi_AiAutoPlaysAfterHumanMove()
    {
        yield return StartVsAi();
        var b = GetBoard();

        // Black plays (2,3) — switches to white = AI.
        Place(2, 3);

        // AI coroutine waits 0.5s then plays. Wait up to 3s for the second
        // BeginTurn (back to black) to fire.
        var deadline = Time.realtimeSinceStartup + 3f;
        bool aiPlayed = false;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            // After AI plays, currentPlayer is back to 1 (black) and total
            // pieces > 5 (initial 4 + black move + AI move = at least 6).
            int total = b.GetScore(1) + b.GetScore(2);
            if (total >= 6 && GetCurrentPlayer() == 1) { aiPlayed = true; break; }
        }
        Assert.That(aiPlayed, Is.True,
            "AI must play within 3s after human move in vs_AI mode.");
    }

    // ── Functional: pass ─────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator Pass_HandlePassPublishesEventAndSwitchesTurn()
    {
        yield return StartVsHuman();
        Assume.That(GetCurrentPlayer(), Is.EqualTo(1), "expect black to start");
        _gotPass = false;
        _gotGameOver = false;

        // Call HandlePass directly. Since we still have the standard opening
        // (4 stones), the opponent (white) will have valid moves after the
        // switch, so HandlePass should not trigger EndGame here.
        var handlePass = typeof(OthelloGameManager).GetMethod("HandlePass",
            BindingFlags.NonPublic | BindingFlags.Instance);
        handlePass.Invoke(OthelloGameManager.Instance, null);

        yield return null;
        yield return null;

        Assert.That(_gotPass, Is.True, "HandlePass must publish PassTurnEvent.");
        Assert.That(_lastPass.playerColor, Is.EqualTo(1),
            "PassTurnEvent should report the player who passed (black).");
        Assert.That(GetCurrentPlayer(), Is.EqualTo(2),
            "Turn must switch to white after pass.");
        Assert.That(_gotGameOver, Is.False,
            "GameOver must not fire when opponent still has valid moves.");
    }

    [UnityTest]
    public IEnumerator BeginTurn_AutoPassesWhenCurrentPlayerHasNoMoves()
    {
        yield return StartVsHuman();
        _gotPass = false;
        _gotGameOver = false;

        // Empty board with only one black stone — neither side has any valid
        // move (each needs to flip an opponent line). Both will pass and the
        // game should end. We're verifying that BeginTurn detects 0-moves and
        // routes through HandlePass → EndGame, with PassTurnEvent fired at
        // least once along the way.
        var s = EmptyBoard();
        s[0, 0] = 1;
        SetBoardState(s);

        var beginTurn = typeof(OthelloGameManager).GetMethod("BeginTurn",
            BindingFlags.NonPublic | BindingFlags.Instance);
        beginTurn.Invoke(OthelloGameManager.Instance, null);

        yield return null;
        yield return null;

        Assert.That(_gotPass, Is.True,
            "BeginTurn with zero moves must route to HandlePass → PassTurnEvent.");
        Assert.That(_gotGameOver, Is.True,
            "When both sides cannot move, the game must end.");
    }

    // ── Functional: game over ────────────────────────────────────────────

    [UnityTest]
    public IEnumerator GameOver_FiresWhenNeitherPlayerHasMoves()
    {
        yield return StartVsHuman();
        _gotGameOver = false;

        // Fill the entire board with stones so neither side has any move.
        var s = EmptyBoard();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                s[r, c] = (r + c) % 2 == 0 ? 1 : 2;
        SetBoardState(s);

        var beginTurn = typeof(OthelloGameManager).GetMethod("BeginTurn",
            BindingFlags.NonPublic | BindingFlags.Instance);
        beginTurn.Invoke(OthelloGameManager.Instance, null);

        yield return null;
        yield return null;

        Assert.That(_gotGameOver, Is.True, "GameOverEvent must fire when neither side can move.");
        Assert.That(_lastGameOver.blackCount, Is.EqualTo(32));
        Assert.That(_lastGameOver.whiteCount, Is.EqualTo(32));
        // winner is decided by total = stones + tile bonus + mission bonus,
        // so 32-32 stones does NOT guarantee a draw — missions are random per
        // game. Just assert winner is one of the three valid values.
        Assert.That(_lastGameOver.winner, Is.InRange(0, 2));
    }

    // ── Functional: winner perspective + mission complete ─────────────────

    static Text WinnerTextOnPanel()
    {
        var go = FindByName("Winner");
        return go == null ? null : go.GetComponentInChildren<Text>();
    }

    [UnityTest]
    public IEnumerator VsAi_GameOver_ShowsYouPerspective()
    {
        yield return StartVsAi();
        _gotGameOver = false;

        // Fill the board entirely with black so winner = 1 (black) is forced
        // and player (always black in vs-AI) wins.
        var s = EmptyBoard();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                s[r, c] = 1;
        SetBoardState(s);

        var beginTurn = typeof(OthelloGameManager).GetMethod("BeginTurn",
            BindingFlags.NonPublic | BindingFlags.Instance);
        beginTurn.Invoke(OthelloGameManager.Instance, null);

        yield return null;
        yield return null;

        Assert.That(_gotGameOver, Is.True);
        Assert.That(_lastGameOver.winner, Is.EqualTo(1));

        var winText = WinnerTextOnPanel();
        Assert.That(winText, Is.Not.Null);
        Assert.That(winText.text, Is.EqualTo(Loc.Get("you_win")),
            "vs-AI mode should show 'You Win!' (player perspective), not the literal 'Black Wins!'.");
    }

    [UnityTest]
    public IEnumerator VsHuman_GameOver_ShowsLiteralColors()
    {
        yield return StartVsHuman();
        _gotGameOver = false;

        // White-dominant fill so winner = 2 is decided.
        var s = EmptyBoard();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                s[r, c] = 2;
        SetBoardState(s);

        var beginTurn = typeof(OthelloGameManager).GetMethod("BeginTurn",
            BindingFlags.NonPublic | BindingFlags.Instance);
        beginTurn.Invoke(OthelloGameManager.Instance, null);

        yield return null;
        yield return null;

        Assert.That(_gotGameOver, Is.True);
        Assert.That(_lastGameOver.winner, Is.EqualTo(2));

        var winText = WinnerTextOnPanel();
        Assert.That(winText, Is.Not.Null);
        Assert.That(winText.text, Is.EqualTo(Loc.Get("white_wins")),
            "vs-Human mode should keep the literal 'White Wins!' wording.");
        Assert.That(winText.text, Is.Not.EqualTo(Loc.Get("you_win")));
        Assert.That(winText.text, Is.Not.EqualTo(Loc.Get("you_lose")));
    }

    static TurnChangedEvent MakeTurn(int player, bool achieved, string progress = "1/2")
    {
        return new TurnChangedEvent
        {
            playerColor        = player,
            validMoves         = new List<Vector2Int> { new Vector2Int(2, 3) },
            blackCount         = 2,
            whiteCount         = 2,
            missionLocKey      = "mission_x",
            missionProgress    = progress,
            missionBonus       = 8,
            missionAchieved    = achieved,
            missionPlayerColor = player,
            vsAI               = false,
        };
    }

    [UnityTest]
    public IEnumerator MissionAchieved_ShowsSnackbarOverlay()
    {
        yield return StartVsHuman();

        // Force the tracked state to "not achieved" first regardless of the
        // randomly assigned mission, then trigger the false→true transition
        // with a synthetic event.
        EventBus.Publish(MakeTurn(player: 1, achieved: false));
        yield return null;
        EventBus.Publish(MakeTurn(player: 1, achieved: true, progress: "2/2"));

        // Give the snackbar a couple of frames to activate.
        yield return null;
        yield return null;

        var snackbar = FindByName("MissionSnackbar");
        Assert.That(snackbar, Is.Not.Null);
        Assert.That(snackbar.activeInHierarchy, Is.True,
            "MissionSnackbar must be active when a mission flips from incomplete to complete.");

        // Snackbar should contain "Mission Complete!" text and the bonus.
        var allTexts = snackbar.GetComponentsInChildren<Text>();
        var combined = string.Join(" ", allTexts.Select(t => t.text));
        Assert.That(combined, Does.Contain(Loc.Get("mission_complete")));
        Assert.That(combined, Does.Contain("+8 pt"));
    }

    [UnityTest]
    public IEnumerator MissionBar_KeepsCurrentStatusDuringSnackbar()
    {
        yield return StartVsHuman();

        EventBus.Publish(MakeTurn(player: 1, achieved: false));
        yield return null;
        EventBus.Publish(MakeTurn(player: 1, achieved: true, progress: "2/2"));
        yield return null;
        yield return null;

        // The mission bar's name text must KEEP showing the actual mission
        // name during the snackbar — not be replaced with the celebration
        // text and not show "???".
        var nameText = FindByName("MissionName").GetComponentInChildren<Text>();
        Assert.That(nameText.text, Is.EqualTo(Loc.Get("mission_x")),
            "Mission bar should keep showing the actual mission name throughout, " +
            "not be replaced by the celebration text.");
        Assert.That(nameText.text, Is.Not.EqualTo("???"));
    }

    [UnityTest]
    public IEnumerator MissionAchieved_PersistsCheckMarkAfterSnackbar()
    {
        yield return StartVsHuman();

        EventBus.Publish(MakeTurn(player: 1, achieved: false));
        yield return null;
        EventBus.Publish(MakeTurn(player: 1, achieved: true, progress: "2/2"));

        // Wait for the 2.0s snackbar to dismiss + a couple more frames.
        yield return new WaitForSeconds(2.2f);

        var progText = FindByName("MissionProgress").GetComponentInChildren<Text>();
        Assert.That(progText.text, Does.Contain("✓"),
            "After the snackbar dismisses, progress should keep a ✓ to mark the achieved state.");

        var snackbar = FindByName("MissionSnackbar");
        Assert.That(snackbar.activeInHierarchy, Is.False,
            "Snackbar must auto-dismiss after its display window.");
    }

    void CapturePngTo(string path)
    {
        var cam = Camera.main;
        if (cam == null) return;

        const int W = 1080, H = 1920;
        var canvases = Object.FindObjectsOfType<Canvas>();
        var saved = new (Canvas c, RenderMode m, Camera cam, float plane)[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            saved[i] = (c, c.renderMode, c.worldCamera, c.planeDistance);
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1f;
            }
        }
        Canvas.ForceUpdateCanvases();

        var rt = new RenderTexture(W, H, 24);
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;

        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;
        cam.targetTexture = prevTarget;

        for (int i = 0; i < saved.Length; i++)
        {
            if (saved[i].c == null) continue;
            saved[i].c.renderMode = saved[i].m;
            saved[i].c.worldCamera = saved[i].cam;
            saved[i].c.planeDistance = saved[i].plane;
        }

        var bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        File.WriteAllBytes(path, bytes);
    }

    [UnityTest]
    public IEnumerator GameOverReveal_RecordFrameSequence()
    {
        // Enable real reveal timing so the recorded frames span the
        // animation. Output goes to TestArtifacts/reveal_frames/ as a
        // numbered PNG sequence — combined into a GIF by an external script.
        OthelloUIManager.RevealClearPause      = 0.30f;
        OthelloUIManager.RevealStonePlaceDelay = 0.04f;
        OthelloUIManager.RevealPhaseGap        = 0.40f;

        yield return StartVsHuman();

        var s = EmptyBoard();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                s[r, c] = (r + c) % 2 == 0 ? 1 : 2;
        SetBoardState(s);

        var beginTurn = typeof(OthelloGameManager).GetMethod("BeginTurn",
            BindingFlags.NonPublic | BindingFlags.Instance);
        beginTurn.Invoke(OthelloGameManager.Instance, null);

        var framesDir = Path.Combine(ArtifactsDir, "reveal_frames");
        Directory.CreateDirectory(framesDir);
        // Wipe any old frames so the next combine doesn't pick up stale ones.
        foreach (var f in Directory.GetFiles(framesDir, "frame_*.png"))
            File.Delete(f);

        // Capture ~30 frames at 150ms intervals (covers the full ~4s reveal
        // plus 0.5s of the breakdown panel).
        const int frameCount = 30;
        for (int i = 0; i < frameCount; i++)
        {
            var path = Path.Combine(framesDir, $"frame_{i:D3}.png");
            CapturePngTo(path);
            yield return new WaitForSeconds(0.15f);
        }
    }

    [UnityTest]
    public IEnumerator GameOverReveal_MidAnimation_ShowsPartialRefill()
    {
        // Re-enable the per-stone reveal timing for this test so the
        // screenshot taken in TearDown captures the mid-refill state
        // (some stones placed, score ticked partway up).
        OthelloUIManager.RevealClearPause      = 0.30f;
        OthelloUIManager.RevealStonePlaceDelay = 0.04f;
        OthelloUIManager.RevealPhaseGap        = 0.40f;

        yield return StartVsHuman();
        _gotGameOver = false;

        // Set up a checker board so 32 of each color exist — the reveal
        // will tick from 0→32 for black, then 0→32 for white.
        var s = EmptyBoard();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                s[r, c] = (r + c) % 2 == 0 ? 1 : 2;
        SetBoardState(s);

        var beginTurn = typeof(OthelloGameManager).GetMethod("BeginTurn",
            BindingFlags.NonPublic | BindingFlags.Instance);
        beginTurn.Invoke(OthelloGameManager.Instance, null);

        // Let the clear phase complete and ~14 black stones get placed.
        // Total black-phase length: 0.30 (clear) + 32 × 0.04 = 1.58s.
        // Sample at 0.95s — board should have ~16 black stones, no whites.
        yield return new WaitForSeconds(0.95f);

        Assert.That(_gotGameOver, Is.True, "GameOver event must fire.");
        // The score counter should be in the middle of the count-up,
        // proving the reveal animation is actually running.
        var blackText = FindByName("BlackScore").GetComponentInChildren<Text>();
        int blackTick;
        Assert.That(int.TryParse(blackText.text, out blackTick), Is.True);
        Assert.That(blackTick, Is.GreaterThan(0).And.LessThanOrEqualTo(32),
            "Black score should be in the middle of the count-up during the reveal.");

        // Turn indicator should read "FINAL RESULT" during the reveal.
        var turnText = FindByName("TurnIndicator").GetComponentInChildren<Text>();
        Assert.That(turnText.text, Is.EqualTo("FINAL  RESULT"));

        // Game-over panel must NOT yet be visible.
        var gameOverPanel = FindByName("GameOverPanel");
        Assert.That(gameOverPanel.activeInHierarchy, Is.False,
            "Game-over breakdown panel should not appear until the reveal finishes.");
    }

    [UnityTest]
    public IEnumerator TopBar_HomeRowAboveScoreRow()
    {
        // Verify the new 2-row TopBar layout: HOME button sits above the
        // score/turn row.
        yield return StartVsHuman();

        var home = FindByName("Btn_title_btn");
        var blackScore = FindByName("BlackScore");
        Assert.That(home, Is.Not.Null);
        Assert.That(blackScore, Is.Not.Null);

        var homeRT = home.GetComponent<RectTransform>();
        var scoreRT = blackScore.GetComponent<RectTransform>();

        Assert.That(homeRT.anchorMin.y, Is.GreaterThan(scoreRT.anchorMax.y),
            "HOME button must be anchored above the score row in TopBar.");
    }

    [UnityTest]
    public IEnumerator SettingsButton_VisibleOnTitle_HiddenInGame()
    {
        // On the title screen the settings chip is visible…
        var settings = FindByName("Btn_settings");
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.activeInHierarchy, Is.True,
            "Settings chip should be visible on the title screen.");

        // …and hidden once a game starts.
        yield return StartVsHuman();
        Assert.That(settings.activeInHierarchy, Is.False,
            "Settings chip must be hidden during gameplay.");
    }

    [UnityTest]
    public IEnumerator ThemePicker_OpensFromSettingsAndListsAllThemes()
    {
        var picker = FindByName("ThemePickerPanel");
        Assert.That(picker, Is.Not.Null);
        Assert.That(picker.activeInHierarchy, Is.False,
            "Picker must be hidden until the user opens it.");

        var settings = FindByName("Btn_settings");
        settings.GetComponent<Button>().onClick.Invoke();
        yield return null;
        yield return null;

        Assert.That(picker.activeInHierarchy, Is.True,
            "Picker should open when the settings chip is tapped.");

        // All four theme options must be reachable as buttons.
        foreach (var kind in new[] { "Pieces", "Riso", "Wabi", "Neon" })
        {
            var row = FindByName("Btn_theme_" + kind);
            Assert.That(row, Is.Not.Null,
                $"Theme picker is missing a row for {kind}.");
            Assert.That(row.GetComponent<Button>(), Is.Not.Null,
                $"Theme row {kind} must be clickable.");
        }
    }

    [UnityTest]
    public IEnumerator GameOverReveal_PlacesBlackBottomWhiteTop()
    {
        // Capture every PiecePlacedEvent fired DURING the reveal sequence to
        // verify the new layout: black stones in rows 0–3, white in rows 4–7.
        yield return StartVsHuman();

        var placements = new List<(int row, int col, int color)>();
        System.Action<PiecePlacedEvent> handler = ev =>
            placements.Add((ev.row, ev.col, ev.playerColor));
        EventBus.Subscribe<PiecePlacedEvent>(handler);

        try
        {
            // Set up a 32 + 32 checker state and force GameOver via BeginTurn.
            var s = EmptyBoard();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    s[r, c] = (r + c) % 2 == 0 ? 1 : 2;
            SetBoardState(s);

            placements.Clear(); // ignore anything before the reveal

            var beginTurn = typeof(OthelloGameManager).GetMethod("BeginTurn",
                BindingFlags.NonPublic | BindingFlags.Instance);
            beginTurn.Invoke(OthelloGameManager.Instance, null);

            // Reveal delays are 0 in tests; a few frames is enough.
            for (int i = 0; i < 6; i++) yield return null;
        }
        finally
        {
            EventBus.Unsubscribe<PiecePlacedEvent>(handler);
        }

        var blacks = placements.Where(p => p.color == 1).ToList();
        var whites = placements.Where(p => p.color == 2).ToList();

        Assert.That(blacks.Count, Is.EqualTo(32), "Should place all 32 black stones.");
        Assert.That(whites.Count, Is.EqualTo(32), "Should place all 32 white stones.");
        Assert.That(blacks.All(p => p.row >= 0 && p.row <= 3), Is.True,
            "Black stones must all land in the bottom 4 rows (player's side).");
        Assert.That(whites.All(p => p.row >= 4 && p.row <= 7), Is.True,
            "White stones must all land in the top 4 rows (enemy's side).");
    }

    [UnityTest]
    public IEnumerator VsAi_AiTurn_DoesNotShowQuestionMarks()
    {
        yield return StartVsAi();

        // After StartVsAi, an initial TurnChangedEvent fired for player 1
        // (black, the human). Mission name should be a real mission name.
        var nameText = FindByName("MissionName").GetComponentInChildren<Text>();
        Assert.That(nameText.text, Is.Not.EqualTo("???"),
            "vs-AI mission bar must always show the player's mission, never '???'.");

        // Place to trigger AI turn.
        Place(2, 3);

        // Wait for AI to move and publish its TurnChangedEvent.
        var deadline = Time.realtimeSinceStartup + 3f;
        bool aiTurnFired = false;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            if (_lastTurn.playerColor == 2) { aiTurnFired = true; break; }
        }
        Assert.That(aiTurnFired, "AI's TurnChangedEvent should fire after human moves.");

        // Mission name MUST still be the player's mission, not "???".
        Assert.That(nameText.text, Is.Not.EqualTo("???"),
            "Mission bar must keep showing the player's mission during the AI's turn.");
    }
}
