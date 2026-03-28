using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class OthelloSceneBuilder
{
    [MenuItem("Othello/Build All Scenes")]
    static void BuildAll()
    {
        if (UnityEditor.EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play mode before running Build All Scenes.");
            return;
        }
        Directory.CreateDirectory("Assets/Scenes");
        BuildGameScene();
        SetBuildSettings();
        AssetDatabase.Refresh();
        Debug.Log("=== Othello scenes created! Open Assets/Scenes/Game.unity and hit Play. ===");
    }

    static void BuildGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Main Camera
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
        camGO.AddComponent<AudioListener>();
        camGO.AddComponent<CameraSetup>();
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        // Managers
        var mgr = new GameObject("Managers");
        mgr.AddComponent<GameManager>();
        mgr.AddComponent<AudioManager>();
        mgr.AddComponent<ScoreManager>();
        mgr.AddComponent<SceneLoader>();
        mgr.AddComponent<ObjectPool>();
        mgr.AddComponent<OthelloGameManager>();
        mgr.AddComponent<OthelloAudioManager>();

        // Board
        var board = new GameObject("Board");
        board.AddComponent<BoardView>();

        // UI
        var ui = new GameObject("UI");
        ui.AddComponent<OthelloUIManager>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
    }

    static void BuildMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Main Camera
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();

        // Menu
        var menu = new GameObject("Menu");
        menu.AddComponent<SceneLoader>();
        menu.AddComponent<MainMenuController>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    static void SetBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true),
        };
    }
}
