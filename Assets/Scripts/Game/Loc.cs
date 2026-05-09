using System.Collections.Generic;
using UnityEngine;

public static class Loc
{
    public enum Lang { EN, JA }
    public static Lang Current { get; private set; }

    static readonly Dictionary<string, string[]> _table = new Dictionary<string, string[]>
    {
        { "title",         new[] { "OTHELLO",            "オセロ" } },
        { "subtitle",      new[] { "Classic Board Game", "クラシックボードゲーム" } },
        { "select_mode",   new[] { "Select Mode",        "モードを選択" } },
        { "vs_ai",         new[] { "vs AI",              "AIと対戦" } },
        { "vs_human",      new[] { "vs Human",           "2人で対戦" } },
        { "records",       new[] { "Records",            "記録" } },
        { "back",          new[] { "Back",               "戻る" } },
        { "title_btn",     new[] { "⌂ HOME",             "⌂ ホーム" } },
        { "play_again",    new[] { "Play Again",         "もう一度" } },
        { "menu",          new[] { "Menu",               "メニュー" } },
        { "black_turn",    new[] { "Black's Turn",       "黒のターン" } },
        { "white_turn",    new[] { "White's Turn",       "白のターン" } },
        { "black_passes",  new[] { "Black passes!",      "黒がパス！" } },
        { "white_passes",  new[] { "White passes!",      "白がパス！" } },
        { "draw",          new[] { "Draw!",              "引き分け！" } },
        { "black_wins",    new[] { "Black Wins!",        "黒の勝ち！" } },
        { "white_wins",    new[] { "White Wins!",        "白の勝ち！" } },
        { "you_win",       new[] { "You Win!",           "あなたの勝ち！" } },
        { "you_lose",      new[] { "You Lose",           "あなたの負け" } },
        { "you_played_as", new[] { "you played as",      "プレイしたのは" } },
        { "mission_complete", new[] { "🎉 Mission Complete!", "🎉 ミッション達成！" } },
        { "stat_games",    new[] { "Games Played: ",     "プレイ数：" } },
        { "stat_black",    new[] { "Black Wins: ",       "黒の勝利：" } },
        { "stat_white",    new[] { "White Wins: ",       "白の勝利：" } },
        { "stat_high",     new[] { "High Score: ",       "ハイスコア：" } },
        { "lang_btn",      new[] { "日本語",              "English" } },
        { "lang_mode",     new[] { "日本語",              "English" } },  // alias for themed text buttons
        { "version",       new[] { "v1.0",               "v1.0" } },

        // Bonus tiles
        { "tile_gold",     new[] { "Gold Tile +5",       "金マス +5" } },
        { "tile_poison",   new[] { "Poison Tile -2",     "毒マス -2" } },

        // Mission names
        { "mission_x",           new[] { "Take 2+ X-squares",     "Xマスを2つ以上取る" } },
        { "mission_few",         new[] { "End with ≤28 pieces",   "28枚以下で終える" } },
        { "mission_center",      new[] { "Control 3+ center",     "中央を3マス取る" } },
        { "mission_edges",       new[] { "Hold 12+ edges",        "辺を12枚取る" } },
        { "mission_no_corners",  new[] { "Take no corners",       "角を取らない" } },

        // Mission UI
        { "your_mission",        new[] { "Your Mission",          "あなたのミッション" } },
        { "opp_mission",         new[] { "Opponent",              "相手" } },
        { "mission_achieved",    new[] { "ACHIEVED!",             "達成！" } },
        { "mission_failed",      new[] { "Not achieved",          "未達成" } },
        { "missions_revealed",   new[] { "Missions Revealed",     "ミッション公開" } },
    };

    static Loc()
    {
        Current = PlayerPrefs.GetInt("OTH_Lang", 0) == 1 ? Lang.JA : Lang.EN;
    }

    public static string Get(string key)
    {
        if (_table.TryGetValue(key, out var arr))
            return arr[(int)Current];
        return key;
    }

    public static void Toggle()
    {
        SetLang(Current == Lang.EN ? Lang.JA : Lang.EN);
    }

    static void SetLang(Lang lang)
    {
        Current = lang;
        PlayerPrefs.SetInt("OTH_Lang", (int)lang);
    }
}
