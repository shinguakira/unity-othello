using NUnit.Framework;
using UnityEngine;

public class MissionDataTests
{
    // ── helpers ────────────────────────────────────────────────────────────

    static MissionData Mission(MissionType type) => new MissionData(type, 0);

    static int[,] EmptyBoard() => new int[8, 8];

    static int[,] BoardWith(params (int r, int c, int color)[] pieces)
    {
        var b = new int[8, 8];
        foreach (var (r, c, color) in pieces) b[r, c] = color;
        return b;
    }

    // ── XSquares ──────────────────────────────────────────────────────────

    [Test]
    public void XSquares_TwoOrMore_Achieved()
    {
        var board = BoardWith((1, 1, 1), (1, 6, 1)); // two X-squares for black
        Assert.IsTrue(Mission(MissionType.XSquares).Check(board, 1));
    }

    [Test]
    public void XSquares_OnlyOne_NotAchieved()
    {
        var board = BoardWith((1, 1, 1));
        Assert.IsFalse(Mission(MissionType.XSquares).Check(board, 1));
    }

    [Test]
    public void XSquares_OpponentPiecesNotCounted()
    {
        var board = BoardWith((1, 1, 2), (6, 6, 2)); // opponent has 2 X-squares
        Assert.IsFalse(Mission(MissionType.XSquares).Check(board, 1));
    }

    // ── FewPieces ─────────────────────────────────────────────────────────

    [Test]
    public void FewPieces_Exactly28_Achieved()
    {
        var board = new int[8, 8];
        int placed = 0;
        for (int r = 0; r < 8 && placed < 28; r++)
            for (int c = 0; c < 8 && placed < 28; c++)
            {
                board[r, c] = 1;
                placed++;
            }
        Assert.IsTrue(Mission(MissionType.FewPieces).Check(board, 1));
    }

    [Test]
    public void FewPieces_29Pieces_NotAchieved()
    {
        var board = new int[8, 8];
        int placed = 0;
        for (int r = 0; r < 8 && placed < 29; r++)
            for (int c = 0; c < 8 && placed < 29; c++)
            {
                board[r, c] = 1;
                placed++;
            }
        Assert.IsFalse(Mission(MissionType.FewPieces).Check(board, 1));
    }

    // ── CenterControl ─────────────────────────────────────────────────────

    [Test]
    public void CenterControl_ThreeCenterSquares_Achieved()
    {
        var board = BoardWith((3, 3, 1), (3, 4, 1), (4, 3, 1));
        Assert.IsTrue(Mission(MissionType.CenterControl).Check(board, 1));
    }

    [Test]
    public void CenterControl_AllFour_Achieved()
    {
        var board = BoardWith((3, 3, 1), (3, 4, 1), (4, 3, 1), (4, 4, 1));
        Assert.IsTrue(Mission(MissionType.CenterControl).Check(board, 1));
    }

    [Test]
    public void CenterControl_TwoSquares_NotAchieved()
    {
        var board = BoardWith((3, 3, 1), (3, 4, 1));
        Assert.IsFalse(Mission(MissionType.CenterControl).Check(board, 1));
    }

    // ── EdgeDominance ─────────────────────────────────────────────────────

    [Test]
    public void EdgeDominance_Exactly12Edges_Achieved()
    {
        var board = new int[8, 8];
        // Fill top row (8) + left col rows 1-4 (4) = 12 edge squares
        for (int c = 0; c < 8; c++) board[0, c] = 1;
        for (int r = 1; r <= 4; r++) board[r, 0] = 1;
        Assert.IsTrue(Mission(MissionType.EdgeDominance).Check(board, 1));
    }

    [Test]
    public void EdgeDominance_11Edges_NotAchieved()
    {
        var board = new int[8, 8];
        for (int c = 0; c < 8; c++) board[0, c] = 1; // 8
        for (int r = 1; r <= 3; r++) board[r, 0] = 1; // +3 = 11
        Assert.IsFalse(Mission(MissionType.EdgeDominance).Check(board, 1));
    }

    // ── NoCorners ─────────────────────────────────────────────────────────

    [Test]
    public void NoCorners_ZeroCorners_Achieved()
    {
        var board = BoardWith((3, 3, 1), (3, 4, 1)); // non-corner pieces only
        Assert.IsTrue(Mission(MissionType.NoCorners).Check(board, 1));
    }

    [Test]
    public void NoCorners_EmptyBoard_Achieved()
    {
        Assert.IsTrue(Mission(MissionType.NoCorners).Check(EmptyBoard(), 1));
    }

    [Test]
    public void NoCorners_OneCorner_NotAchieved()
    {
        var board = BoardWith((0, 0, 1));
        Assert.IsFalse(Mission(MissionType.NoCorners).Check(board, 1));
    }

    // ── GetProgress ───────────────────────────────────────────────────────

    [Test]
    public void GetProgress_XSquares_ShowsCurrentOverGoal()
    {
        var board = BoardWith((1, 1, 1));
        string progress = Mission(MissionType.XSquares).GetProgress(board, 1);
        Assert.AreEqual("1/2", progress);
    }

    [Test]
    public void GetProgress_NoCorners_ShowsCornerCount()
    {
        var board = BoardWith((0, 0, 1));
        string progress = Mission(MissionType.NoCorners).GetProgress(board, 1);
        Assert.AreEqual("1 corners", progress);
    }

    // ── AssignRandom ──────────────────────────────────────────────────────

    [Test]
    public void AssignRandom_ReturnsTwoDifferentMissions()
    {
        var (black, white) = MissionData.AssignRandom();
        Assert.AreNotEqual(black.Type, white.Type);
    }

    [Test]
    public void AssignRandom_BothHavePositiveBonus()
    {
        var (black, white) = MissionData.AssignRandom();
        Assert.Greater(black.Bonus, 0);
        Assert.Greater(white.Bonus, 0);
    }
}
