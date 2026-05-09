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
    // Mission shown in the UI: in vs-AI this is ALWAYS the human player's
    // mission so it stays visible during the AI's turn. In vs-Human it's
    // the current turn player's mission.
    public string missionLocKey;
    public string missionProgress;
    public int missionBonus;
    public bool missionAchieved;
    public int missionPlayerColor; // who the displayed mission belongs to
    public bool vsAI;
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
