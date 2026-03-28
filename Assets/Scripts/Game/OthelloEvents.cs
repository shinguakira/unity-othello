using System.Collections.Generic;
using UnityEngine;

public struct CellClickedEvent
{
    public int row;
    public int col;
}

public struct PiecePlacedEvent
{
    public int row;
    public int col;
    public int playerColor;
}

public struct PiecesFlippedEvent
{
    public List<Vector2Int> positions;
    public int newColor;
}

public struct TurnChangedEvent
{
    public int playerColor;
    public List<Vector2Int> validMoves;
    public int blackCount;
    public int whiteCount;
}

public struct PassTurnEvent
{
    public int playerColor;
}

public struct GameOverEvent
{
    public int blackCount;
    public int whiteCount;
    public int winner; // 0=draw, 1=black, 2=white
}

public struct BoardResetEvent { }

public struct GameModeSelectedEvent
{
    public bool vsAI;
}
