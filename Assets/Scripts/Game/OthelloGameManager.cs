using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OthelloGameManager : MonoBehaviour
{
    public static OthelloGameManager Instance { get; private set; }

    OthelloBoard _board;
    int _currentPlayer; // 1=black, 2=white
    bool _vsAI;
    bool _gameOver;
    Coroutine _aiCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _board = new OthelloBoard();
    }

    void OnEnable()
    {
        EventBus.Subscribe<CellClickedEvent>(OnCellClicked);
        EventBus.Subscribe<GameModeSelectedEvent>(OnGameModeSelected);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<CellClickedEvent>(OnCellClicked);
        EventBus.Unsubscribe<GameModeSelectedEvent>(OnGameModeSelected);
    }

    void OnDestroy()
    {
        if (_aiCoroutine != null) StopCoroutine(_aiCoroutine);
    }

    public void BeginGame()
    {
        if (_aiCoroutine != null) StopCoroutine(_aiCoroutine);
        _board.Reset();
        _currentPlayer = 1;
        _gameOver = false;
        BeginTurn();
    }

    void BeginTurn()
    {
        List<Vector2Int> moves = _board.GetValidMoves(_currentPlayer);

        if (moves.Count == 0)
        {
            HandlePass();
            return;
        }

        EventBus.Publish(new TurnChangedEvent
        {
            playerColor = _currentPlayer,
            validMoves  = moves,
            blackCount  = _board.GetScore(1),
            whiteCount  = _board.GetScore(2)
        });

        if (_vsAI && _currentPlayer == 2)
            _aiCoroutine = StartCoroutine(AITurnCoroutine(moves));
    }

    void HandlePass()
    {
        EventBus.Publish(new PassTurnEvent { playerColor = _currentPlayer });
        SwitchPlayer();

        List<Vector2Int> nextMoves = _board.GetValidMoves(_currentPlayer);
        if (nextMoves.Count == 0)
            EndGame();
        else
            BeginTurn();
    }

    void SwitchPlayer() => _currentPlayer = _currentPlayer == 1 ? 2 : 1;

    void EndGame()
    {
        _gameOver = true;
        int black = _board.GetScore(1);
        int white = _board.GetScore(2);
        int winner = black > white ? 1 : white > black ? 2 : 0;

        EventBus.Publish(new GameOverEvent { blackCount = black, whiteCount = white, winner = winner });
        OthelloSaveSystem.SaveResult(winner, black, white);

        if (GameManager.Instance != null) GameManager.Instance.GameOver();
    }

    void OnCellClicked(CellClickedEvent e)
    {
        if (_gameOver) return;
        if (_vsAI && _currentPlayer == 2) return;

        List<Vector2Int> valid = _board.GetValidMoves(_currentPlayer);
        if (!valid.Contains(new Vector2Int(e.row, e.col))) return;

        _board.PlacePiece(e.row, e.col, _currentPlayer);
        SwitchPlayer();
        BeginTurn();
    }

    void OnGameModeSelected(GameModeSelectedEvent e)
    {
        _vsAI = e.vsAI;
        BeginGame();
    }

    IEnumerator AITurnCoroutine(List<Vector2Int> validMoves)
    {
        yield return new WaitForSeconds(0.5f);
        if (_gameOver) yield break;

        Vector2Int move = OthelloAI.ChooseMoveMinMax(_board, 2, 3);
        if (move.x == -1) yield break;

        _board.PlacePiece(move.x, move.y, 2);
        SwitchPlayer();
        BeginTurn();
    }
}
