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

        // Mobile-friendly layout: TopBar at top, Mission strip just below it,
        // Board pushed toward the bottom for easy thumb reach. Bottom padding
        // is small so the board sits low without clipping.
        const float topFrac = 240f / 1920f;  // TopBar (now 2 rows, ~0.125)
        const float missionFrac = 0.084f;        // Mission strip below TopBar
        const float gapAbove = 0.012f;        // breathing space above board
        const float bottomPad = 0.04f;         // small bottom padding
        float boardAreaFrac = 1f - topFrac - missionFrac - gapAbove - bottomPad;
        float availH = boardAreaFrac * 0.96f;    // 96% of board area used (light side padding)

        float boardHalf = 4.4f; // board + frame half-extent in world units
        float orthForWidth = boardHalf / (0.94f * cam.aspect); // fill 94% of screen width
        float orthForHeight = boardHalf / availH;
        cam.orthographicSize = Mathf.Max(orthForWidth, orthForHeight);

        // Center board within its allocated area (between bottomPad and
        // bottomPad + boardAreaFrac). Result: board sits low on the screen
        // for thumb-reach friendliness.
        float boardCenterFrac = bottomPad + boardAreaFrac / 2f;
        float worldHeight = cam.orthographicSize * 2f;
        float camY = -(boardCenterFrac - 0.5f) * worldHeight;
        cam.transform.position = new Vector3(0f, camY, -10f);
    }
}
