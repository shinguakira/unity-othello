using UnityEngine;

public enum ThemeKind
{
    Riso,    // 2-color risograph print
    Wabi,    // Japanese minimalism
    Neon,    // synthwave / arcade
    Pieces,  // board-forward (default)
}

public static class OthelloTheme
{
    const string PrefKey = "OTH_Theme";
    public const ThemeKind Default = ThemeKind.Pieces;

    public static ThemeKind Active { get; private set; } = Default;

    static OthelloTheme()
    {
        // 1) Load saved preference (set via in-game settings).
        if (PlayerPrefs.HasKey(PrefKey) &&
            System.Enum.TryParse<ThemeKind>(PlayerPrefs.GetString(PrefKey), out var saved))
        {
            Active = saved;
        }
        // 2) -theme=<Kind> CLI flag overrides saved preference (used by
        //    automated theme-comparison test runs).
        foreach (var arg in System.Environment.GetCommandLineArgs())
        {
            if (arg.StartsWith("-theme=") &&
                System.Enum.TryParse<ThemeKind>(arg.Substring("-theme=".Length), true, out var k))
            {
                Active = k;
                Debug.Log($"[Theme] Active = {k} (from CLI)");
                return;
            }
        }
        Debug.Log($"[Theme] Active = {Active}");
    }

    /// <summary>
    /// Persist the new theme and reload the current scene so the UI rebuilds
    /// with the new visual language. No need to restart the app.
    /// </summary>
    public static void SetActive(ThemeKind kind)
    {
        if (Active == kind) return;
        Active = kind;
        PlayerPrefs.SetString(PrefKey, kind.ToString());
        PlayerPrefs.Save();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
    }
}
