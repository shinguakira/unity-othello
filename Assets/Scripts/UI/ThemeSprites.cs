using UnityEngine;

// Procedurally generated sprites used by the themes. Cached statically so
// each theme reuses the same texture and Unity batches them.
public static class ThemeSprites
{
    static Sprite _whiteSquare, _circle, _ring, _ringThin, _radialGlow,
                  _halftoneDense, _halftoneSparse, _gridFine, _grid8,
                  _diagStripe, _concentric, _piecePattern, _vignette,
                  _arc, _seal;

    public static Sprite WhiteSquare => _whiteSquare = _whiteSquare != null ? _whiteSquare : MakeSolid();
    public static Sprite Circle => _circle = _circle != null ? _circle : MakeCircle(128, 1f);
    public static Sprite Ring => _ring = _ring != null ? _ring : MakeRing(128, 0.42f, 1f);
    public static Sprite RingThin => _ringThin = _ringThin != null ? _ringThin : MakeRing(128, 0.84f, 1f);
    public static Sprite RadialGlow => _radialGlow = _radialGlow != null ? _radialGlow : MakeRadialGlow(256);
    public static Sprite HalftoneDense => _halftoneDense = _halftoneDense != null ? _halftoneDense : MakeHalftone(512, 24, 5.5f);
    public static Sprite HalftoneSparse => _halftoneSparse = _halftoneSparse != null ? _halftoneSparse : MakeHalftone(512, 16, 3.2f);
    public static Sprite GridFine => _gridFine = _gridFine != null ? _gridFine : MakeGrid(512, 32, 1);
    public static Sprite Grid8 => _grid8 = _grid8 != null ? _grid8 : MakeGrid(512, 64, 2);
    public static Sprite DiagStripe => _diagStripe = _diagStripe != null ? _diagStripe : MakeDiagonalStripes(256, 24);
    public static Sprite Concentric => _concentric = _concentric != null ? _concentric : MakeConcentric(256, 6);
    public static Sprite PiecePattern => _piecePattern = _piecePattern != null ? _piecePattern : MakePiecePattern(512, 64);
    public static Sprite Vignette => _vignette = _vignette != null ? _vignette : MakeVignette(512);
    public static Sprite Arc => _arc = _arc != null ? _arc : MakeArc(256);
    public static Sprite Seal => _seal = _seal != null ? _seal : MakeSealStamp(256);

    static Sprite WrapTexture(Texture2D tex, int border = 0)
    {
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        var rect = new Rect(0, 0, tex.width, tex.height);
        var pivot = new Vector2(0.5f, 0.5f);
        return Sprite.Create(tex, rect, pivot, 100f, (uint)border, SpriteMeshType.FullRect);
    }

    static Sprite MakeSolid()
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        return WrapTexture(tex);
    }

    static Sprite MakeCircle(int size, float radiusFrac)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = size * 0.5f, cy = size * 0.5f, r = size * 0.5f * radiusFrac;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = Mathf.Clamp01(r - d + 0.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        return WrapTexture(tex);
    }

    static Sprite MakeRing(int size, float innerFrac, float outerFrac)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = size * 0.5f, cy = size * 0.5f;
        float ro = size * 0.5f * outerFrac;
        float ri = size * 0.5f * innerFrac;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float outer = Mathf.Clamp01(ro - d + 0.5f);
                float inner = Mathf.Clamp01(d - ri + 0.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Min(outer, inner)));
            }
        return WrapTexture(tex);
    }

    static Sprite MakeRadialGlow(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = size * 0.5f, cy = size * 0.5f;
        float maxR = size * 0.5f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = Mathf.Clamp01(1f - d / maxR);
                a = Mathf.Pow(a, 1.6f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        return WrapTexture(tex);
    }

    // Halftone dot grid: rows of small circles on a regular grid, density via dot radius.
    static Sprite MakeHalftone(int size, int cell, float dotRadius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var clear = new Color(0, 0, 0, 0);
        for (int x = 0; x < size; x++) for (int y = 0; y < size; y++) tex.SetPixel(x, y, clear);
        for (int gy = 0; gy < size; gy += cell)
            for (int gx = 0; gx < size; gx += cell)
            {
                float cx = gx + cell * 0.5f, cy = gy + cell * 0.5f;
                int r = Mathf.CeilToInt(dotRadius) + 1;
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int x = (int)cx + dx, y = (int)cy + dy;
                        if (x < 0 || y < 0 || x >= size || y >= size) continue;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(dotRadius - d + 0.5f);
                        if (a > 0)
                        {
                            var c = tex.GetPixel(x, y);
                            tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(c.a, a)));
                        }
                    }
            }
        return WrapTexture(tex);
    }

    static Sprite MakeGrid(int size, int cell, int line)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var clear = new Color(0, 0, 0, 0);
        for (int x = 0; x < size; x++) for (int y = 0; y < size; y++) tex.SetPixel(x, y, clear);
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                bool onLine = (x % cell) < line || (y % cell) < line;
                if (onLine) tex.SetPixel(x, y, Color.white);
            }
        return WrapTexture(tex);
    }

    static Sprite MakeDiagonalStripes(int size, int period)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                int v = (x + y) % period;
                bool on = v < period / 2;
                tex.SetPixel(x, y, on ? Color.white : new Color(0, 0, 0, 0));
            }
        return WrapTexture(tex);
    }

    static Sprite MakeConcentric(int size, int rings)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = size * 0.5f, cy = size * 0.5f;
        float maxR = size * 0.5f - 1;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float t = d / maxR;
                if (t > 1f) { tex.SetPixel(x, y, new Color(0, 0, 0, 0)); continue; }
                float band = Mathf.Sin(t * rings * Mathf.PI);
                float a = band > 0.5f ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        return WrapTexture(tex);
    }

    // 8×8 board pattern with alternating dark squares + black/white discs in an
    // arrangement reminiscent of a mid-game state. Used as decorative bg.
    static Sprite MakePiecePattern(int size, int cell)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        int n = size / cell;
        bool[,] hasPiece = new bool[n, n];
        bool[,] isWhite = new bool[n, n];
        // Sample pattern: scattered pieces, denser near center.
        var rng = new System.Random(42);
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                float dx = (i - (n - 1) * 0.5f) / n;
                float dy = (j - (n - 1) * 0.5f) / n;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float prob = Mathf.Clamp01(0.7f - dist);
                if (rng.NextDouble() < prob)
                {
                    hasPiece[i, j] = true;
                    isWhite[i, j] = (i + j) % 2 == 0 ? rng.NextDouble() < 0.5
                                                     : rng.NextDouble() < 0.5;
                }
            }

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                int gx = x / cell, gy = y / cell;
                bool dark = (gx + gy) % 2 == 0;
                Color c = dark ? new Color(1, 1, 1, 0.05f) : new Color(0, 0, 0, 0);
                if (hasPiece[gx, gy])
                {
                    float lx = x - gx * cell - cell * 0.5f;
                    float ly = y - gy * cell - cell * 0.5f;
                    float d = Mathf.Sqrt(lx * lx + ly * ly);
                    float pr = cell * 0.36f;
                    float a = Mathf.Clamp01(pr - d + 0.5f);
                    if (a > 0)
                    {
                        c = isWhite[gx, gy] ? new Color(1, 1, 1, a)
                                           : new Color(0, 0, 0, a);
                    }
                }
                tex.SetPixel(x, y, c);
            }
        return WrapTexture(tex);
    }

    static Sprite MakeVignette(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = size * 0.5f, cy = size * 0.5f;
        float maxR = size * 0.5f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float t = d / maxR;
                float a = Mathf.Clamp01(Mathf.Pow(t, 2.4f));
                tex.SetPixel(x, y, new Color(0, 0, 0, a * 0.85f));
            }
        return WrapTexture(tex);
    }

    // Quarter-circle arc (top-right quadrant filled), used as decorative element.
    static Sprite MakeArc(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = 0f, cy = size; // pivot at bottom-left of texture
        float r = size;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - (size - cy)) * (y - (size - cy)));
                float a = Mathf.Clamp01(r - d + 0.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        return WrapTexture(tex);
    }

    // Round seal-like stamp with concentric ring + center dot.
    static Sprite MakeSealStamp(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = size * 0.5f, cy = size * 0.5f;
        float ro = size * 0.48f, ri = size * 0.42f;
        float dot = size * 0.10f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float ringA = (d <= ro && d >= ri) ? 1f : 0f;
                float dotA = (d <= dot) ? 1f : 0f;
                float a = Mathf.Max(ringA, dotA);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        return WrapTexture(tex);
    }
}
