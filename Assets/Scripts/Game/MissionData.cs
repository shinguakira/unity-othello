using UnityEngine;

public enum MissionType
{
    XSquares,       // Hold 2+ X-squares at game end      → +8
    FewPieces,      // Final piece count ≤ 28             → +10
    CenterControl,  // Hold 3+ of the center 4 squares   → +7
    EdgeDominance,  // Hold 12+ edge squares              → +6
    NoCorners,      // Hold 0 corners                     → +12
}

public class MissionData
{
    public readonly MissionType Type;
    public readonly int Bonus;

    static readonly Vector2Int[] XSquarePositions =
    {
        new Vector2Int(1, 1), new Vector2Int(1, 6),
        new Vector2Int(6, 1), new Vector2Int(6, 6),
    };

    static readonly Vector2Int[] CenterPositions =
    {
        new Vector2Int(3, 3), new Vector2Int(3, 4),
        new Vector2Int(4, 3), new Vector2Int(4, 4),
    };

    static readonly Vector2Int[] CornerPositions =
    {
        new Vector2Int(0, 0), new Vector2Int(0, 7),
        new Vector2Int(7, 0), new Vector2Int(7, 7),
    };

    static readonly (MissionType type, int bonus)[] Pool =
    {
        (MissionType.XSquares,       8),
        (MissionType.FewPieces,     10),
        (MissionType.CenterControl,  7),
        (MissionType.EdgeDominance,  6),
        (MissionType.NoCorners,     12),
    };

    public MissionData(MissionType type, int bonus)
    {
        Type  = type;
        Bonus = bonus;
    }

    public static (MissionData black, MissionData white) AssignRandom()
    {
        int i = Random.Range(0, Pool.Length);
        int j;
        do { j = Random.Range(0, Pool.Length); } while (j == i);
        return (new MissionData(Pool[i].type, Pool[i].bonus),
                new MissionData(Pool[j].type, Pool[j].bonus));
    }

    public bool Check(int[,] board, int playerColor)
    {
        switch (Type)
        {
            case MissionType.XSquares:      return CountAt(board, playerColor, XSquarePositions) >= 2;
            case MissionType.FewPieces:     return CountAll(board, playerColor) <= 28;
            case MissionType.CenterControl: return CountAt(board, playerColor, CenterPositions) >= 3;
            case MissionType.EdgeDominance: return CountEdges(board, playerColor) >= 12;
            case MissionType.NoCorners:     return CountAt(board, playerColor, CornerPositions) == 0;
            default:                        return false;
        }
    }

    // Returns a short "current/goal" string shown during gameplay
    public string GetProgress(int[,] board, int playerColor)
    {
        switch (Type)
        {
            case MissionType.XSquares:      return CountAt(board, playerColor, XSquarePositions) + "/2";
            case MissionType.FewPieces:     return CountAll(board, playerColor) + "/28";
            case MissionType.CenterControl: return CountAt(board, playerColor, CenterPositions) + "/3";
            case MissionType.EdgeDominance: return CountEdges(board, playerColor) + "/12";
            case MissionType.NoCorners:     return CountAt(board, playerColor, CornerPositions) + " corners";
            default:                        return "";
        }
    }

    public string GetLocKey()
    {
        switch (Type)
        {
            case MissionType.XSquares:      return "mission_x";
            case MissionType.FewPieces:     return "mission_few";
            case MissionType.CenterControl: return "mission_center";
            case MissionType.EdgeDominance: return "mission_edges";
            case MissionType.NoCorners:     return "mission_no_corners";
            default:                        return "";
        }
    }

    static int CountAt(int[,] board, int playerColor, Vector2Int[] positions)
    {
        int count = 0;
        foreach (var p in positions)
            if (board[p.x, p.y] == playerColor) count++;
        return count;
    }

    static int CountAll(int[,] board, int playerColor)
    {
        int count = 0;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (board[r, c] == playerColor) count++;
        return count;
    }

    // Counts all 28 edge squares (outer ring, excluding corners to avoid double-count — actually include all)
    static int CountEdges(int[,] board, int playerColor)
    {
        int count = 0;
        for (int c = 0; c < 8; c++)
        {
            if (board[0, c] == playerColor) count++;
            if (board[7, c] == playerColor) count++;
        }
        for (int r = 1; r < 7; r++)
        {
            if (board[r, 0] == playerColor) count++;
            if (board[r, 7] == playerColor) count++;
        }
        return count;
    }
}
