using System.Collections.Generic;
using UnityEngine;

public static class OthelloAI
{
    static readonly int[,] Directions = new int[,]
    {
        { -1, -1 }, { -1, 0 }, { -1, 1 },
        {  0, -1 },             {  0, 1 },
        {  1, -1 }, {  1, 0 }, {  1, 1 }
    };

    static readonly int[,] PositionWeights = new int[,]
    {
        { 25, -5,  5,  3,  3,  5, -5, 25 },
        { -5,-10,  1,  1,  1,  1,-10, -5 },
        {  5,  1,  3,  2,  2,  3,  1,  5 },
        {  3,  1,  2,  1,  1,  2,  1,  3 },
        {  3,  1,  2,  1,  1,  2,  1,  3 },
        {  5,  1,  3,  2,  2,  3,  1,  5 },
        { -5,-10,  1,  1,  1,  1,-10, -5 },
        { 25, -5,  5,  3,  3,  5, -5, 25 }
    };

    public static Vector2Int ChooseMoveMinMax(OthelloBoard board, int playerColor, int depth = 3)
    {
        var moves = board.GetValidMoves(playerColor);
        if (moves.Count == 0) return new Vector2Int(-1, -1);

        Vector2Int best = moves[0];
        int bestScore = int.MinValue;

        foreach (var move in moves)
        {
            var copy = board.GetBoardCopy();
            ApplyMove(copy, move.x, move.y, playerColor);
            int score = MiniMax(copy, depth - 1, Opponent(playerColor), false, int.MinValue, int.MaxValue, playerColor);
            if (score > bestScore)
            {
                bestScore = score;
                best = move;
            }
        }
        return best;
    }

    static int MiniMax(int[,] board, int depth, int currentPlayer, bool isMaximising, int alpha, int beta, int aiColor)
    {
        var moves = GetValidMovesOnCopy(board, currentPlayer);

        if (depth == 0 || (moves.Count == 0 && GetValidMovesOnCopy(board, Opponent(currentPlayer)).Count == 0))
            return Evaluate(board, aiColor);

        if (moves.Count == 0)
            return MiniMax(board, depth - 1, Opponent(currentPlayer), !isMaximising, alpha, beta, aiColor);

        if (isMaximising)
        {
            int maxScore = int.MinValue;
            foreach (var move in moves)
            {
                var copy = CopyBoard(board);
                ApplyMove(copy, move.x, move.y, currentPlayer);
                int score = MiniMax(copy, depth - 1, Opponent(currentPlayer), false, alpha, beta, aiColor);
                maxScore = Mathf.Max(maxScore, score);
                alpha = Mathf.Max(alpha, score);
                if (beta <= alpha) break;
            }
            return maxScore;
        }
        else
        {
            int minScore = int.MaxValue;
            foreach (var move in moves)
            {
                var copy = CopyBoard(board);
                ApplyMove(copy, move.x, move.y, currentPlayer);
                int score = MiniMax(copy, depth - 1, Opponent(currentPlayer), true, alpha, beta, aiColor);
                minScore = Mathf.Min(minScore, score);
                beta = Mathf.Min(beta, score);
                if (beta <= alpha) break;
            }
            return minScore;
        }
    }

    static int Evaluate(int[,] board, int aiColor)
    {
        int score = 0;
        int opponent = Opponent(aiColor);
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (board[r, c] == aiColor)
                    score += PositionWeights[r, c];
                else if (board[r, c] == opponent)
                    score -= PositionWeights[r, c];
            }
        }
        return score;
    }

    static int CountFlipsOnCopy(int[,] board, Vector2Int move, int playerColor)
    {
        int count = 0;
        int opponent = Opponent(playerColor);
        for (int d = 0; d < 8; d++)
        {
            int dr = Directions[d, 0];
            int dc = Directions[d, 1];
            var candidates = new List<Vector2Int>();
            int r = move.x + dr;
            int c = move.y + dc;
            while (InBounds(r, c) && board[r, c] == opponent)
            {
                candidates.Add(new Vector2Int(r, c));
                r += dr;
                c += dc;
            }
            if (candidates.Count > 0 && InBounds(r, c) && board[r, c] == playerColor)
                count += candidates.Count;
        }
        return count;
    }

    static void ApplyMove(int[,] board, int row, int col, int playerColor)
    {
        int opponent = Opponent(playerColor);
        board[row, col] = playerColor;
        for (int d = 0; d < 8; d++)
        {
            int dr = Directions[d, 0];
            int dc = Directions[d, 1];
            var candidates = new List<Vector2Int>();
            int r = row + dr;
            int c = col + dc;
            while (InBounds(r, c) && board[r, c] == opponent)
            {
                candidates.Add(new Vector2Int(r, c));
                r += dr;
                c += dc;
            }
            if (candidates.Count > 0 && InBounds(r, c) && board[r, c] == playerColor)
                foreach (var pos in candidates)
                    board[pos.x, pos.y] = playerColor;
        }
    }

    static List<Vector2Int> GetValidMovesOnCopy(int[,] board, int playerColor)
    {
        var moves = new List<Vector2Int>();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (board[r, c] == 0 && CountFlipsOnCopy(board, new Vector2Int(r, c), playerColor) > 0)
                    moves.Add(new Vector2Int(r, c));
        return moves;
    }

    static int[,] CopyBoard(int[,] board)
    {
        var copy = new int[8, 8];
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                copy[r, c] = board[r, c];
        return copy;
    }

    static int Opponent(int playerColor) => playerColor == 1 ? 2 : 1;

    static bool InBounds(int r, int c) => r >= 0 && r < 8 && c >= 0 && c < 8;
}
