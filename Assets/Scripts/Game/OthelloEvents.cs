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
    // Current player's mission (opponent's is always hidden as "???")
    public string missionLocKey;   // Loc key → mission name
    public string missionProgress; // e.g. "1/2"
    public int missionBonus;       // e.g. 8
    public bool missionAchieved;   // current state of THIS player's mission
    public bool vsAI;              // true when playing against AI
}

public struct PassTurnEvent
{
    public int playerColor;
}

public struct GameOverEvent
{
    public int blackCount;
    public int whiteCount;
    public int blackTileBonus;
    public int whiteTileBonus;
    public MissionData blackMission;
    public MissionData whiteMission;
    public bool blackMissionAchieved;
    public bool whiteMissionAchieved;
    public int winner; // 0=draw, 1=black, 2=white  (based on total score)
}

public struct BoardResetEvent { }

public struct GameModeSelectedEvent
{
    public bool vsAI;
}
