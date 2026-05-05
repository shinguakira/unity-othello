using System.Collections.Generic;
using UnityEngine;

public class OthelloBoard
{
    int[,] _board; // 0=empty, 1=black, 2=white

    static readonly int[,] Directions = new int[,]
    {
        { -1, -1 }, { -1, 0 }, { -1, 1 },
        {  0, -1 },             {  0, 1 },
        {  1, -1 }, {  1, 0 }, {  1, 1 }
    };

    public OthelloBoard()
    {
        _board = new int[8, 8];
        PlaceStartingPieces();
    }

    void PlaceStartingPieces()
    {
        _board[3, 3] = 2;
        _board[3, 4] = 1;
        _board[4, 3] = 1;
        _board[4, 4] = 2;
    }

    public List<Vector2Int> GetValidMoves(int playerColor)
    {
        var moves = new List<Vector2Int>();
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (_board[r, c] != 0) continue;
                if (GetAllFlips(r, c, playerColor).Count > 0)
                    moves.Add(new Vector2Int(r, c));
            }
        }
        return moves;
    }

    public List<Vector2Int> PlacePiece(int row, int col, int playerColor)
    {
        var flipped = GetAllFlips(row, col, playerColor);
        _board[row, col] = playerColor;
        foreach (var pos in flipped)
            _board[pos.x, pos.y] = playerColor;

        EventBus.Publish(new PiecePlacedEvent { row = row, col = col, playerColor = playerColor });
        EventBus.Publish(new PiecesFlippedEvent { positions = flipped, newColor = playerColor });
        return flipped;
    }

    public bool IsGameOver()
    {
        return GetValidMoves(1).Count == 0 && GetValidMoves(2).Count == 0;
    }

    public int GetScore(int playerColor)
    {
        int count = 0;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (_board[r, c] == playerColor) count++;
        return count;
    }

    public int GetBonusScore(int playerColor)
        => BonusTileConfig.CalcBonusScore(_board, playerColor);

    public int[,] GetBoardCopy()
    {
        var copy = new int[8, 8];
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                copy[r, c] = _board[r, c];
        return copy;
    }

    public void Reset()
    {
        _board = new int[8, 8];
        PlaceStartingPieces();
        EventBus.Publish(new BoardResetEvent());
    }

    List<Vector2Int> GetAllFlips(int row, int col, int playerColor)
    {
        var all = new List<Vector2Int>();
        for (int d = 0; d < 8; d++)
        {
            int dr = Directions[d, 0];
            int dc = Directions[d, 1];
            all.AddRange(GetFlipsInDirection(row, col, dr, dc, playerColor));
        }
        return all;
    }

    List<Vector2Int> GetFlipsInDirection(int row, int col, int dr, int dc, int playerColor)
    {
        int opponent = playerColor == 1 ? 2 : 1;
        var candidates = new List<Vector2Int>();
        int r = row + dr;
        int c = col + dc;

        while (InBounds(r, c) && _board[r, c] == opponent)
        {
            candidates.Add(new Vector2Int(r, c));
            r += dr;
            c += dc;
        }

        if (candidates.Count > 0 && InBounds(r, c) && _board[r, c] == playerColor)
            return candidates;

        return new List<Vector2Int>();
    }

    bool InBounds(int r, int c) => r >= 0 && r < 8 && c >= 0 && c < 8;
}
