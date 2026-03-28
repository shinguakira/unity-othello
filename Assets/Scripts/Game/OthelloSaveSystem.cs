using UnityEngine;

public static class OthelloSaveSystem
{
    const string KeyTotalGames = "OTH_TotalGames";
    const string KeyBlackWins  = "OTH_BlackWins";
    const string KeyWhiteWins  = "OTH_WhiteWins";
    const string KeyHighScore  = "OTH_HighScore";
    const string KeyLastWinner = "OTH_LastWinner";

    public static void SaveResult(int winner, int blackCount, int whiteCount)
    {
        PlayerPrefs.SetInt(KeyLastWinner, winner);
        PlayerPrefs.SetInt(KeyTotalGames, GetTotalGames() + 1);

        if (winner == 1) PlayerPrefs.SetInt(KeyBlackWins, GetBlackWins() + 1);
        else if (winner == 2) PlayerPrefs.SetInt(KeyWhiteWins, GetWhiteWins() + 1);

        int best = Mathf.Max(blackCount, whiteCount);
        if (best > GetHighScore()) PlayerPrefs.SetInt(KeyHighScore, best);

        PlayerPrefs.Save();
    }

    public static int GetTotalGames() => PlayerPrefs.GetInt(KeyTotalGames, 0);
    public static int GetBlackWins()  => PlayerPrefs.GetInt(KeyBlackWins,  0);
    public static int GetWhiteWins()  => PlayerPrefs.GetInt(KeyWhiteWins,  0);
    public static int GetHighScore()  => PlayerPrefs.GetInt(KeyHighScore,  0);
    public static int GetLastWinner() => PlayerPrefs.GetInt(KeyLastWinner, 0);
}
