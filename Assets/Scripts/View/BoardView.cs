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
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<PiecePlacedEvent>(OnPiecePlaced);
        EventBus.Unsubscribe<PiecesFlippedEvent>(OnPiecesFlipped);
        EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        EventBus.Unsubscribe<BoardResetEvent>(OnBoardReset);
    }

    void CreateBoard()
    {
        _cells = new CellView[8, 8];

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
                cell.Init(r, c);
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

    void DrawGridLines()
    {
        // Thin dark lines between cells for visual clarity
        var lineGO = new GameObject("GridLines");
        lineGO.transform.SetParent(transform, false);

        for (int i = 0; i <= 8; i++)
        {
            float pos = BoardOrigin - CellSize * 0.5f + i * CellSize;
            CreateLine(lineGO, new Vector3(pos, BoardOrigin - CellSize * 0.5f, 0f),
                               new Vector3(pos, BoardOrigin + 7.5f * CellSize, 0f));
            CreateLine(lineGO, new Vector3(BoardOrigin - CellSize * 0.5f, pos, 0f),
                               new Vector3(BoardOrigin + 7.5f * CellSize, pos, 0f));
        }
    }

    void CreateLine(GameObject parent, Vector3 start, Vector3 end)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(parent.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0f, 0.25f, 0.05f, 1f);
        lr.endColor = new Color(0f, 0.25f, 0.05f, 1f);
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
