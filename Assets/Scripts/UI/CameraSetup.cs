using System.Collections;
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
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.04f);

        StartCoroutine(ApplyLayout(cam));
    }

    // Wait one frame so portrait orientation is committed before reading screen dimensions
    IEnumerator ApplyLayout(Camera cam)
    {
        yield return null;

        // cam.aspect is reliable after one frame (portrait orientation committed)
        // Compute the orthSize that satisfies BOTH width and height constraints, take the larger.
        const float topFrac = 160f / 1920f;   // TopBar fraction of screen
        const float botFrac = 0.10f;           // MissionPanel fraction
        float availH = (1f - topFrac - botFrac) * 0.90f; // 90% of available height for board

        float boardHalf = 4.4f; // board + frame half-extent in world units
        float orthForWidth  = boardHalf / (0.90f * cam.aspect); // fill 90% of screen width
        float orthForHeight = boardHalf / availH;               // fill available vertical space
        cam.orthographicSize = Mathf.Max(orthForWidth, orthForHeight);

        // Center vertically in the space between TopBar and MissionPanel
        float availCenter = (botFrac + (1f - topFrac)) / 2f; // fraction from screen bottom
        float worldHeight = cam.orthographicSize * 2f;
        float camY        = -(availCenter - 0.5f) * worldHeight;
        cam.transform.position = new Vector3(0f, camY, -10f);
    }
}
