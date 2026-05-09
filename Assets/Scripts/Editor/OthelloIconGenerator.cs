using System.IO;
using UnityEngine;
using UnityEditor;

public static class OthelloIconGenerator
{
    [MenuItem("Othello/Generate App Icon")]
    static void Generate()
    {
        Directory.CreateDirectory("Assets/Icons");

        int[] sizes = { 1024, 512, 192, 144, 108, 96, 72, 48, 36 };
        foreach (int s in sizes)
        {
            var tex = DrawIcon(s);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes($"Assets/Icons/icon_{s}.png", png);
        }

        AssetDatabase.Refresh();
        AssignIcons();
        Debug.Log("Othello icons generated and assigned.");
    }

    static Texture2D DrawIcon(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        float s = size;
        float cx = s / 2f;
        float cy = s / 2f;

        // ── Colors ──
        var bgDark = new Color(0.06f, 0.18f, 0.08f, 1f);  // outer border
        var green1 = new Color(0.14f, 0.50f, 0.18f, 1f);  // light cell
        var green2 = new Color(0.10f, 0.40f, 0.14f, 1f);  // dark cell
        var lineCol = new Color(0.04f, 0.22f, 0.07f, 1f);  // grid lines
        var black = new Color(0.08f, 0.08f, 0.08f, 1f);
        var white = new Color(0.93f, 0.93f, 0.93f, 1f);
        var hlWhite = new Color(1f, 1f, 1f, 0.30f);

        float border = s * 0.06f;
        float boardSize = s - border * 2f;
        float cellSize = boardSize / 8f;

        for (int px = 0; px < size; px++)
        {
            for (int py = 0; py < size; py++)
            {
                float x = px;
                float y = py;

                // Rounded outer background
                float rx = x - cx;
                float ry = y - cy;
                float cornerR = s * 0.18f;
                float dist = RoundedBoxDist(rx, ry, s / 2f - cornerR, s / 2f - cornerR, cornerR);
                if (dist > 0f)
                {
                    tex.SetPixel(px, py, Color.clear);
                    continue;
                }

                // Board area
                float bx = x - border;
                float by = y - border;
                if (bx >= 0 && bx < boardSize && by >= 0 && by < boardSize)
                {
                    int col = (int)(bx / cellSize);
                    int row = (int)(by / cellSize);
                    col = Mathf.Clamp(col, 0, 7);
                    row = Mathf.Clamp(row, 0, 7);

                    // Grid lines
                    float fx = bx - col * cellSize;
                    float fy = by - row * cellSize;
                    float lineW = Mathf.Max(1f, s * 0.007f);
                    if (fx < lineW || fy < lineW)
                    {
                        tex.SetPixel(px, py, lineCol);
                        continue;
                    }

                    tex.SetPixel(px, py, (row + col) % 2 == 0 ? green1 : green2);
                }
                else
                {
                    tex.SetPixel(px, py, bgDark);
                }
            }
        }

        // Draw 4 starting discs (center cells 3,3 / 3,4 / 4,3 / 4,4)
        DrawDisc(tex, size, border, cellSize, 3, 3, white, hlWhite);
        DrawDisc(tex, size, border, cellSize, 4, 4, white, hlWhite);
        DrawDisc(tex, size, border, cellSize, 3, 4, black, hlWhite);
        DrawDisc(tex, size, border, cellSize, 4, 3, black, hlWhite);

        tex.Apply();
        return tex;
    }

    static void DrawDisc(Texture2D tex, int size, float border, float cellSize,
        int row, int col, Color discColor, Color highlight)
    {
        float margin = cellSize * 0.12f;
        float r = cellSize / 2f - margin;
        float cx = border + col * cellSize + cellSize / 2f;
        float cy = border + row * cellSize + cellSize / 2f;

        int x0 = Mathf.Max(0, (int)(cx - r - 2));
        int x1 = Mathf.Min(size - 1, (int)(cx + r + 2));
        int y0 = Mathf.Max(0, (int)(cy - r - 2));
        int y1 = Mathf.Min(size - 1, (int)(cy + r + 2));

        for (int px = x0; px <= x1; px++)
        {
            for (int py = y0; py <= y1; py++)
            {
                float dx = px - cx;
                float dy = py - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(r - dist + 1.5f);
                if (alpha <= 0f) continue;

                Color cur = tex.GetPixel(px, py);
                Color c = Color.Lerp(cur, discColor, alpha);

                // Highlight (top-left)
                float hdx = dx + r * 0.3f;
                float hdy = dy - r * 0.3f;
                float hd = Mathf.Sqrt(hdx * hdx + hdy * hdy);
                float ha = Mathf.Clamp01((r * 0.5f - hd + 1f) / (r * 0.5f)) * 0.5f;
                c = Color.Lerp(c, Color.white, ha * highlight.a);

                tex.SetPixel(px, py, c);
            }
        }
    }

    // Returns distance outside a rounded rectangle (negative = inside)
    static float RoundedBoxDist(float px, float py, float hw, float hh, float r)
    {
        float qx = Mathf.Abs(px) - hw;
        float qy = Mathf.Abs(py) - hh;
        return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) +
                          Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) - r;
    }

    static void AssignIcons()
    {
        // Load the largest icon for default
        var icon1024 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/icon_1024.png");
        if (icon1024 == null) return;

        // Set as default platform icon (covers Desktop + fallback)
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon1024 });

        // Android icons by size
        var androidSizes = new[] { 192, 144, 108, 96, 72, 48, 36 };
        var androidTextures = new Texture2D[androidSizes.Length];
        for (int i = 0; i < androidSizes.Length; i++)
            androidTextures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Icons/icon_{androidSizes[i]}.png");

        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, androidTextures);
    }
}
