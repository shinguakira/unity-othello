using System.Collections;
using UnityEngine;

public class CellView : MonoBehaviour
{
    int _row;
    int _col;

    SpriteRenderer _bgRenderer;
    GameObject _pieceGO;
    PieceView _pieceView;
    GameObject _validDotGO;

    static Sprite _whiteSquareSprite;
    static Sprite _circleSprite;
    static Sprite _ringSprite;

    public void Init(int row, int col, BonusTileType bonusTile = BonusTileType.None)
    {
        _row = row;
        _col = col;

        EnsureSharedSprites();

        // Background — bonus tiles override the checkerboard green
        _bgRenderer = gameObject.AddComponent<SpriteRenderer>();
        _bgRenderer.sprite = _whiteSquareSprite;
        if (bonusTile == BonusTileType.Gold)
            _bgRenderer.color = new Color(0.55f, 0.40f, 0.05f);
        else if (bonusTile == BonusTileType.Poison)
            _bgRenderer.color = new Color(0.40f, 0.07f, 0.07f);
        else
            _bgRenderer.color = (row + col) % 2 == 0
                ? new Color(0.14f, 0.50f, 0.18f)
                : new Color(0.12f, 0.44f, 0.16f);
        _bgRenderer.sortingOrder = 0;

        // Small corner dot: stays visible even when a piece occupies the cell
        if (bonusTile != BonusTileType.None)
        {
            var dot = new GameObject("BonusDot");
            dot.transform.SetParent(transform, false);
            dot.transform.localPosition = new Vector3(0.37f, 0.37f, -0.01f);
            dot.transform.localScale = Vector3.one * 0.20f;
            var dotSR = dot.AddComponent<SpriteRenderer>();
            dotSR.sprite = _circleSprite;
            dotSR.sortingOrder = 3;
            dotSR.color = bonusTile == BonusTileType.Gold
                ? new Color(1.00f, 0.85f, 0.10f, 1f)
                : new Color(0.90f, 0.15f, 0.15f, 1f);
        }

        // Collider for touch — must be 3D; OnMouseDown uses Physics.Raycast, not Physics2D
        var col3d = gameObject.AddComponent<BoxCollider>();
        col3d.size = new Vector3(1f, 1f, 0.1f);

        // Piece child
        _pieceGO = new GameObject("Piece");
        _pieceGO.transform.SetParent(transform, false);
        _pieceGO.transform.localScale = Vector3.one * 0.85f;
        var pieceSR = _pieceGO.AddComponent<SpriteRenderer>();
        pieceSR.sprite = _circleSprite;
        pieceSR.sortingOrder = 1;
        _pieceView = _pieceGO.AddComponent<PieceView>();

        // Inner highlight — simulates top-left light source for a 3-D disc look
        var hlGO = new GameObject("Highlight");
        hlGO.transform.SetParent(_pieceGO.transform, false);
        hlGO.transform.localPosition = new Vector3(-0.15f, 0.15f, 0f);
        hlGO.transform.localScale = Vector3.one * 0.38f;
        var hlSR = hlGO.AddComponent<SpriteRenderer>();
        hlSR.sprite = _circleSprite;
        hlSR.color = new Color(1f, 1f, 1f, 0.22f);
        hlSR.sortingOrder = 2;

        _pieceGO.SetActive(false);

        // Valid-move ring indicator
        _validDotGO = new GameObject("ValidDot");
        _validDotGO.transform.SetParent(transform, false);
        _validDotGO.transform.localScale = Vector3.one * 0.42f;
        var ringSR = _validDotGO.AddComponent<SpriteRenderer>();
        ringSR.sprite = _ringSprite;
        ringSR.color = new Color(0f, 0f, 0f, 0.55f);
        ringSR.sortingOrder = 1;
        _validDotGO.SetActive(false);
    }

    static void EnsureSharedSprites()
    {
        if (_whiteSquareSprite == null)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSquareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        if (_circleSprite == null)
            _circleSprite = CreateCircleSprite(64);

        if (_ringSprite == null)
            _ringSprite = CreateRingSprite(64);
    }

    static Sprite CreateCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = size / 2f;
        float cy = size / 2f;
        float r = size / 2f - 1f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(r - dist + 1f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public void ShowPiece(int playerColor, bool immediate)
    {
        _pieceGO.SetActive(true);
        _pieceView.SetColor(playerColor == 1, immediate);
    }

    public void HidePiece()
    {
        _pieceGO.SetActive(false);
    }

    public void ShowFlip(int newColor, int flipIndex)
    {
        StartCoroutine(DelayedFlip(newColor, flipIndex));
    }

    IEnumerator DelayedFlip(int newColor, int flipIndex)
    {
        yield return new WaitForSeconds(0.05f * flipIndex);
        if (_pieceGO.activeSelf)
            _pieceView.SetColor(newColor == 1, false);
    }

    public void SetValidDot(bool show)
    {
        _validDotGO.SetActive(show);
    }

    static Sprite CreateRingSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = size / 2f - 0.5f;
        float cy = size / 2f - 0.5f;
        float outerR = size / 2f - 2f;
        float innerR = outerR * 0.55f;
        const float aa = 1.5f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float outer = Mathf.Clamp01((outerR - dist + aa) / aa);
                float inner = Mathf.Clamp01((dist - innerR + aa) / aa);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Min(outer, inner)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnMouseDown()
    {
        EventBus.Publish(new CellClickedEvent { row = _row, col = _col });
    }
}
