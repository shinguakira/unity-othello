# AGENTS.md — unity-othello

Agent guidance for this repository. Read this before starting any work.

## Project Overview

オセロ（リバーシ）ゲーム。Unity 2022.3 LTS、外部アセット不要。
得点マス・秘密ミッションのボーナスルール付き。

## Directory Structure

```
unity-othello/
├── Assets/
│   ├── Scenes/
│   │   ├── Game.unity
│   │   └── MainMenu.unity
│   └── Scripts/
│       ├── Game/
│       │   ├── OthelloBoard.cs        # 盤面状態・石の配置・反転ロジック
│       │   ├── OthelloGameManager.cs  # ターン管理・パス・ゲーム終了・ミッション割り当て
│       │   ├── OthelloAI.cs           # MiniMax + alpha-beta (depth=3)
│       │   ├── OthelloEvents.cs       # イベント定義 (struct)
│       │   ├── BonusTileConfig.cs     # 得点マス配置・ボーナス計算
│       │   ├── MissionData.cs         # ミッション定義・達成チェック・進捗
│       │   ├── EventBus.cs            # 型安全 pub-sub
│       │   ├── OthelloSaveSystem.cs   # PlayerPrefs セーブ
│       │   ├── Loc.cs                 # 日英ローカライズ
│       │   ├── GameManager.cs         # ポーズ・タイム管理
│       │   ├── ScoreManager.cs        # コンボスコア
│       │   └── SceneLoader.cs
│       ├── View/
│       │   ├── BoardView.cs           # ボード描画・イベント受信
│       │   ├── CellView.cs            # セル描画・ボーナスタイルビジュアル
│       │   └── PieceView.cs           # 石のアニメーション
│       ├── UI/
│       │   ├── OthelloUIManager.cs    # ゲームUI・ミッションパネル・ゲームオーバー画面
│       │   ├── UIManager.cs
│       │   ├── MainMenuController.cs
│       │   └── CameraSetup.cs
│       └── Editor/
│           ├── OthelloSceneBuilder.cs
│           └── OthelloIconGenerator.cs
├── Assets/Tests/EditMode/
│   ├── OthelloTests.EditMode.asmdef
│   ├── BonusTileConfigTests.cs
│   └── MissionDataTests.cs
├── Packages/
├── ProjectSettings/
└── unity.ps1                          # compile / Editor 起動
```

## Verifying Changes

コード変更後は必ずコンパイルチェックを実行すること。完了報告前に必須。

```powershell
.\unity.ps1 compile
```

Editor を開いた状態では実行不可。先に Unity を閉じること:

```powershell
Stop-Process -Name Unity -ErrorAction SilentlyContinue; .\unity.ps1 compile
```

テストは Unity Editor の **Window → General → Test Runner → EditMode** から実行。

## Coding Conventions

- **No namespaces** — 全クラスはグローバルスコープ
- **MonoBehaviour singleton** パターン:
  ```csharp
  public static Foo Instance { get; private set; }
  void Awake() {
      if (Instance != null && Instance != this) { Destroy(gameObject); return; }
      Instance = this;
  }
  ```
- **`FindObjectOfType<T>()` 禁止** — `EventBus` 経由で通信
- ゲームロジックの変更は必ず `EventBus` イベントで通知

## Touch/Modify Rules

**絶対に触らない:**
- `OthelloBoard.GetValidMoves` / `GetAllFlips` / `PlacePiece`
- `OthelloGameManager.HandlePass` / `SwitchPlayer`

**スコア変更時に触る箇所:**
- `OthelloGameManager.EndGame()` — ボーナス計算はここに集約
- `OthelloEvents.GameOverEvent` — スコア関連フィールドはここで定義

## What Not To Do

- `Resources.Load()` を多用しない
- `FindObjectOfType<T>()` を `Update()` 内で呼ばない
- `.meta` ファイルを手動作成しない（Unity が自動生成）
- コンパイルチェックせずに完了報告しない
