public enum BonusTileType { None, Gold, Poison }

public static class BonusTileConfig
{
    // X-squares (diagonal to corners) = Gold +5  — normally the worst positional choice
    // Edge midpoints (row/col 0 or 7, columns/rows 3-4) = Poison -2 — safe but passive
    const BonusTileType N = BonusTileType.None;
    const BonusTileType G = BonusTileType.Gold;
    const BonusTileType P = BonusTileType.Poison;

    static readonly BonusTileType[,] Layout = new BonusTileType[8, 8]
    {
        { N, N, N, P, P, N, N, N },  // row 0
        { N, G, N, N, N, N, G, N },  // row 1 — X-squares
        { N, N, N, N, N, N, N, N },  // row 2
        { P, N, N, N, N, N, N, P },  // row 3
        { P, N, N, N, N, N, N, P },  // row 4
        { N, N, N, N, N, N, N, N },  // row 5
        { N, G, N, N, N, N, G, N },  // row 6 — X-squares
        { N, N, N, P, P, N, N, N },  // row 7
    };

    public static BonusTileType GetTileType(int row, int col) => Layout[row, col];

    public static int GetValue(BonusTileType type)
    {
        switch (type)
        {
            case BonusTileType.Gold:   return 5;
            case BonusTileType.Poison: return -2;
            default:                   return 0;
        }
    }

    public static int CalcBonusScore(int[,] board, int playerColor)
    {
        int bonus = 0;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (board[r, c] == playerColor)
                    bonus += GetValue(Layout[r, c]);
        return bonus;
    }
}
