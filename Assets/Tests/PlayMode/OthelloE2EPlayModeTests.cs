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
}
