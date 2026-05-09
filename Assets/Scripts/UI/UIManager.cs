using UnityEngine;

public class UIManager : MonoBehaviour
{
    private bool showGameOver;
    private bool showStageClear;

    private GUIStyle _gameOverStyle;
    private GUIStyle _stageClearStyle;

    public void ShowGameOver() { showGameOver = true; }
    public void ShowStageClear() { showStageClear = true; }

    void OnGUI()
    {
        if (showGameOver)
        {
            if (_gameOverStyle == null)
            {
                _gameOverStyle = new GUIStyle(GUI.skin.label);
                _gameOverStyle.fontSize = 64;
                _gameOverStyle.alignment = TextAnchor.MiddleCenter;
                _gameOverStyle.normal.textColor = Color.red;
            }
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "GAME OVER", _gameOverStyle);
        }

        if (showStageClear)
        {
            if (_stageClearStyle == null)
            {
                _stageClearStyle = new GUIStyle(GUI.skin.label);
                _stageClearStyle.fontSize = 64;
                _stageClearStyle.alignment = TextAnchor.MiddleCenter;
                _stageClearStyle.normal.textColor = Color.yellow;
            }
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "STAGE CLEAR!", _stageClearStyle);
        }
    }
}
