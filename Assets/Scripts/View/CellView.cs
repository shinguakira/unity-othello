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

    public void Init(int row, int col)
    {
        _row = row;
        _col = col;

        EnsureSharedSprites();

        // Background
        _bgRenderer = gameObject.AddComponent<SpriteRenderer>();
        _bgRenderer.sprite = _whiteSquareSprite;
        _bgRenderer.color = new Color(0.18f, 0.55f, 0.2f); // dark green
        _bgRenderer.sortingOrder = 0;

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
        _pieceGO.SetActive(false);

        // Valid-move dot child
        _validDotGO = new GameObject("ValidDot");
        _validDotGO.transform.SetParent(transform, false);
        _validDotGO.transform.localScale = Vector3.one * 0.3f;
        var dotSR = _validDotGO.AddComponent<SpriteRenderer>();
        dotSR.sprite = _circleSprite;
        dotSR.color = new Color(0f, 0f, 0f, 0.4f);
        dotSR.sortingOrder = 1;
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

    void OnMouseDown()
    {
        EventBus.Publish(new CellClickedEvent { row = _row, col = _col });
    }
}
