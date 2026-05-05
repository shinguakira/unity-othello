using NUnit.Framework;

public class BonusTileConfigTests
{
    // X-squares (Gold): (1,1)(1,6)(6,1)(6,6)
    [TestCase(1, 1)] [TestCase(1, 6)]
    [TestCase(6, 1)] [TestCase(6, 6)]
    public void XSquares_AreGold(int r, int c)
    {
        Assert.AreEqual(BonusTileType.Gold, BonusTileConfig.GetTileType(r, c));
    }

    // Edge midpoints (Poison): (0,3)(0,4)(3,0)(4,0)(3,7)(4,7)(7,3)(7,4)
    [TestCase(0, 3)] [TestCase(0, 4)]
    [TestCase(3, 0)] [TestCase(4, 0)]
    [TestCase(3, 7)] [TestCase(4, 7)]
    [TestCase(7, 3)] [TestCase(7, 4)]
    public void EdgeMidpoints_ArePoison(int r, int c)
    {
        Assert.AreEqual(BonusTileType.Poison, BonusTileConfig.GetTileType(r, c));
    }

    // Corners have no bonus (strongest positional squares → no reward)
    [TestCase(0, 0)] [TestCase(0, 7)]
    [TestCase(7, 0)] [TestCase(7, 7)]
    public void Corners_HaveNoBonus(int r, int c)
    {
        Assert.AreEqual(BonusTileType.None, BonusTileConfig.GetTileType(r, c));
    }

    [Test]
    public void GetValue_Gold_Returns5()
        => Assert.AreEqual(5, BonusTileConfig.GetValue(BonusTileType.Gold));

    [Test]
    public void GetValue_Poison_ReturnsMinus2()
        => Assert.AreEqual(-2, BonusTileConfig.GetValue(BonusTileType.Poison));

    [Test]
    public void GetValue_None_Returns0()
        => Assert.AreEqual(0, BonusTileConfig.GetValue(BonusTileType.None));

    [Test]
    public void CalcBonusScore_PieceOnGoldTile_Returns5()
    {
        var board = new int[8, 8];
        board[1, 1] = 1; // black on X-square
        Assert.AreEqual(5, BonusTileConfig.CalcBonusScore(board, 1));
    }

    [Test]
    public void CalcBonusScore_PieceOnPoisonTile_ReturnsMinus2()
    {
        var board = new int[8, 8];
        board[0, 3] = 2; // white on edge midpoint
        Assert.AreEqual(-2, BonusTileConfig.CalcBonusScore(board, 2));
    }

    [Test]
    public void CalcBonusScore_TwoGoldOnePoisonForSamePlayer_Returns8()
    {
        var board = new int[8, 8];
        board[1, 1] = 1; // Gold +5
        board[6, 6] = 1; // Gold +5
        board[0, 3] = 1; // Poison -2
        Assert.AreEqual(8, BonusTileConfig.CalcBonusScore(board, 1));
    }

    [Test]
    public void CalcBonusScore_OpponentPiecesNotCounted()
    {
        var board = new int[8, 8];
        board[1, 1] = 2; // opponent on Gold
        Assert.AreEqual(0, BonusTileConfig.CalcBonusScore(board, 1));
    }

    [Test]
    public void CalcBonusScore_EmptyBoard_Returns0()
    {
        var board = new int[8, 8];
        Assert.AreEqual(0, BonusTileConfig.CalcBonusScore(board, 1));
        Assert.AreEqual(0, BonusTileConfig.CalcBonusScore(board, 2));
    }
}
