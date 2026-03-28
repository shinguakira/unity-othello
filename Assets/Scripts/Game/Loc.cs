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
        { "stat_games",    new[] { "Games Played: ",     "プレイ数：" } },
        { "stat_black",    new[] { "Black Wins: ",       "黒の勝利：" } },
        { "stat_white",    new[] { "White Wins: ",       "白の勝利：" } },
        { "stat_high",     new[] { "High Score: ",       "ハイスコア：" } },
        { "lang_btn",      new[] { "日本語",              "English" } },
        { "version",       new[] { "v1.0",               "v1.0" } },
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
