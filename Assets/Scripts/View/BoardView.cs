using UnityEngine;

public class BoardView : MonoBehaviour
{
    CellView[,] _cells;
    const float CellSize = 1f;
    const float BoardOrigin = -3.5f; // centers 8 cells at 0,0

    void Awake()
    {
        CreateBoard();
    }

    void OnEnable()
    {
        EventBus.Subscribe<PiecePlacedEvent>(OnPiecePlaced);
        EventBus.Subscribe<PiecesFlippedEvent>(OnPiecesFlipped);
        EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Subscribe<BoardResetEvent>(OnBoardReset);
        EventBus.Subscribe<BoardClearAllEvent>(OnBoardClearAll);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<PiecePlacedEvent>(OnPiecePlaced);
        EventBus.Unsubscribe<PiecesFlippedEvent>(OnPiecesFlipped);
        EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Unsubscribe<BoardResetEvent>(OnBoardReset);
        EventBus.Unsubscribe<BoardClearAllEvent>(OnBoardClearAll);
    }

    // Clear every cell and valid-move dot WITHOUT auto-placing the initial
    // 4 stones. Used by the game-over reveal animation.
    void OnBoardClearAll(BoardClearAllEvent e)
    {
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                _cells[r, c].HidePiece();
                _cells[r, c].SetValidDot(false);
            }
    }

    void CreateBoard()
    {
        _cells = new CellView[8, 8];

        CreateBoardFrame();

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                var pos = new Vector3(
                    BoardOrigin + c * CellSize,
                    BoardOrigin + r * CellSize,
                    0f);

                var go = new GameObject($"Cell_{r}_{c}");
                go.transform.SetParent(transform, false);
                go.transform.position = pos;

                var cell = go.AddComponent<CellView>();
                cell.Init(r, c, BonusTileConfig.GetTileType(r, c));
                _cells[r, c] = cell;
            }
        }

        // Draw grid lines between cells
        DrawGridLines();

        // Place initial 4 pieces immediately (no animation)
        _cells[3, 3].ShowPiece(2, true);
        _cells[3, 4].ShowPiece(1, true);
        _cells[4, 3].ShowPiece(1, true);
        _cells[4, 4].ShowPiece(2, true);
    }

    void CreateBoardFrame()
    {
        var frameGO = new GameObject("BoardFrame");
        frameGO.transform.SetParent(transform, false);

        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        var sr = frameGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.04f, 0.16f, 0.06f);
        sr.sortingOrder = -1;
        // Board spans 8 world units; frame adds 0.4 padding on each side
        frameGO.transform.localScale = new Vector3(8.8f, 8.8f, 1f);
    }

    void DrawGridLines()
    {
        // Thin dark lines between cells for visual clarity
        var lineGO = new GameObject("GridLines");
        lineGO.transform.SetParent(transform, false);

        // Resolve the shader once and share a single Material across all 18 lines.
        var sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        for (int i = 0; i <= 8; i++)
        {
            float pos = BoardOrigin - CellSize * 0.5f + i * CellSize;
            CreateLine(lineGO, sharedMaterial,
                               new Vector3(pos, BoardOrigin - CellSize * 0.5f, 0f),
                               new Vector3(pos, BoardOrigin + 7.5f * CellSize, 0f));
            CreateLine(lineGO, sharedMaterial,
                               new Vector3(BoardOrigin - CellSize * 0.5f, pos, 0f),
                               new Vector3(BoardOrigin + 7.5f * CellSize, pos, 0f));
        }
    }

    void CreateLine(GameObject parent, Material sharedMaterial, Vector3 start, Vector3 end)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = 0.025f;
        lr.endWidth = 0.025f;
        lr.sharedMaterial = sharedMaterial;
        lr.startColor = new Color(0.04f, 0.22f, 0.07f, 1f);
        lr.endColor = new Color(0.04f, 0.22f, 0.07f, 1f);
        lr.sortingOrder = 2;
    }

    void OnPiecePlaced(PiecePlacedEvent e)
    {
        _cells[e.row, e.col].ShowPiece(e.playerColor, false);
    }

    void OnPiecesFlipped(PiecesFlippedEvent e)
    {
        for (int i = 0; i < e.positions.Count; i++)
        {
            var pos = e.positions[i];
            _cells[pos.x, pos.y].ShowFlip(e.newColor, i);
        }
    }

    void OnTurnChanged(TurnChangedEvent e)
    {
        // Clear all valid dots
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                _cells[r, c].SetValidDot(false);

        // Show valid move dots for current player
        foreach (var pos in e.validMoves)
            _cells[pos.x, pos.y].SetValidDot(true);
    }

    void OnBoardReset(BoardResetEvent e)
    {
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                _cells[r, c].HidePiece();
                _cells[r, c].SetValidDot(false);
            }
        }

        // Re-place starting 4 pieces
        _cells[3, 3].ShowPiece(2, true);
        _cells[3, 4].ShowPiece(1, true);
        _cells[4, 3].ShowPiece(1, true);
        _cells[4, 4].ShowPiece(2, true);
    }
}
