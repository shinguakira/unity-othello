using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;

        var cam = GetComponent<Camera>();
        if (cam == null) return;

        cam.orthographic = true;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.08f);

        // Fit the 8-unit board to screen width with ~10% padding on sides
        // Board = 8 world units wide; we want visible width = 8 / 0.9 ≈ 8.89 units
        float aspect = (float)Screen.width / Screen.height;
        float visibleWidth = 8f / 0.9f;
        cam.orthographicSize = visibleWidth / (2f * aspect);

        // Shift camera down a bit to leave room for the top UI bar (≈7% of world height)
        float worldHeight = cam.orthographicSize * 2f;
        cam.transform.position = new Vector3(0f, -worldHeight * 0.07f, -10f);
    }
}
