# Spec — unity-othello

## ボードと座標

8×8グリッド。`board[row, col]` で参照。  
`0` = 空、`1` = 黒、`2` = 白。  
初期配置: `(3,3)=白 (3,4)=黒 (4,3)=黒 (4,4)=白`

## スコア計算

```
最終スコア = 石数 + タイルボーナス + ミッションボーナス
```

勝敗は最終スコアで決定（石数ではない）。

## 得点マス配置

```
row\col  0    1    2    3    4    5    6    7
  0      .    .    .    P    P    .    .    .
  1      .    G    .    .    .    .    G    .
  2      .    .    .    .    .    .    .    .
  3      P    .    .    .    .    .    .    P
  4      P    .    .    .    .    .    .    P
  5      .    .    .    .    .    .    .    .
  6      .    G    .    .    .    .    G    .
  7      .    .    .    P    P    .    .    .

G = Gold  +5  （Xマス — 通常は最悪手）
P = Poison -2 （辺中央 — 安全すぎる手）
```

## ミッション仕様

ゲーム開始時に各プレイヤーへ1つランダム割り当て（重複なし）。  
相手のミッションはゲーム終了まで非公開。

| MissionType | 達成条件 | ボーナス |
|-------------|---------|---------|
| `XSquares` | Xマス (1,1)(1,6)(6,1)(6,6) を2つ以上保持 | +8 |
| `FewPieces` | 最終石数 ≤ 28 | +10 |
| `CenterControl` | 中央4マス (3,3)(3,4)(4,3)(4,4) を3つ以上保持 | +7 |
| `EdgeDominance` | 外周28マスを12枚以上保持 | +6 |
| `NoCorners` | 四隅 (0,0)(0,7)(7,0)(7,7) を1つも取らない | +12 |

## イベントフロー

```
GameModeSelected
  └─ BeginGame()
       └─ MissionData.AssignRandom()
       └─ BeginTurn()
            ├─ GetValidMoves() == 0 → HandlePass()
            │    └─ 両者パス → EndGame()
            └─ TurnChangedEvent { playerColor, validMoves, scores, missionLocKey, missionProgress, missionBonus, vsAI }
                 └─ [player clicks / AI picks]
                      └─ PlacePiece() → PiecePlacedEvent + PiecesFlippedEvent
                           └─ SwitchPlayer() → BeginTurn()

EndGame()
  ├─ CalcBonusScore(board, 1/2)       via BonusTileConfig
  ├─ mission.Check(board, 1/2)        via MissionData
  └─ GameOverEvent { counts, tileBonuses, missions, achieved flags, winner }
```

## ファイル責務

| ファイル | 責務 |
|---------|------|
| `OthelloBoard` | 盤面状態・合法手・反転・スコアカウント |
| `OthelloGameManager` | ターン制御・ミッション管理・EndGame |
| `BonusTileConfig` | タイル配置定義・ボーナス計算（静的） |
| `MissionData` | ミッション種別・Check・GetProgress・AssignRandom |
| `OthelloEvents` | 全イベント struct 定義 |
| `BoardView` / `CellView` | 盤面描画・タイルビジュアル |
| `OthelloUIManager` | スコア表示・ミッションパネル・ゲームオーバー画面 |
| `Loc` | 日英文字列テーブル |
