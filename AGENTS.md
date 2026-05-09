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

コード変更後は必ず **フォーマット → コンパイル → テスト** の順で実行すること。
完了報告前に必須。Unity Editor は閉じてから走らせる (csproj/sln をロックするため)。

### Format

リポジトリルートで直接 `dotnet format` を呼ぶ。`unity.ps1` 経由ではない:

```powershell
# repo ルート (.editorconfig がある場所) にいること
dotnet format whitespace --folder Assets\Scripts                     # 整形を適用
dotnet format whitespace --folder Assets\Scripts --verify-no-changes # CI / pre-push: 差分があれば exit 1
```

`.editorconfig` の `root = true` がここがルートだと示すマーカーで、これがあれば
cwd は `.editorconfig` のあるディレクトリ以下ならどこでもよい (整形対象ファイルから
親方向に walk して `.editorconfig` を探すため)。npm における `package.json` 役。

### Lint (Roslyn analyzer)

`Assets/Plugins/Analyzers/Microsoft.Unity.Analyzers/` に Unity 用 Roslyn
analyzer (`UNT*` ルール) が入っている。Unity Editor を開いて Console
ウィンドウで警告を確認する。severity は `.editorconfig` の
`dotnet_diagnostic.UNT*.severity` で調整。

CLI から auto-fix を走らせるコマンド:

```powershell
dotnet format analyzers unity-othello.sln --severity warn --exclude-diagnostics IDE0051 IDE0052
```

- `analyzers` サブコマンド: Roslyn analyzer (`UNT*` など) の auto-fix のみ実行。
  whitespace / style サブコマンドの cosmetic な変更はかからない。
- `--exclude-diagnostics IDE0051 IDE0052`: 未使用 private メンバ判定。
  Unity の `[MenuItem]` / `[ContextMenu]` / `[SerializeField]` をリフレクション
  で呼ぶエントリを誤削除する。warning としては reportする ( `.editorconfig` で
  severity = warning ) が、auto-fix からは除外。

### Compile / tests

```powershell
.\unity.ps1 compile               # コンパイルだけ
Stop-Process -Name Unity -ErrorAction SilentlyContinue; .\unity.ps1 compile  # Unity を閉じてから

.\unity.ps1 test                  # EditMode テスト
.\unity.ps1 playmode              # PlayMode E2E (default テーマ)
.\unity.ps1 playmode-themes       # 4 テーマ全部 → Tests/Design-Themes/screenshots/<Theme>/
```

## UI 変更後はスクショで確認・提示する

UI を変更したら **必ず実スクリーンショットを取って該当部分をユーザに見せる**。
コード差分だけで「直しました」と報告するのは NG — レイアウト不正・色崩れ・
円が楕円になる・テキスト切れ等は、目で見ないと気付けない。

手順:

1. `.\unity.ps1 playmode` を走らせる (テスト中に PNG が自動生成される)
2. `%USERPROFILE%\AppData\LocalLow\Indie\Othell\TestArtifacts\<TestName>.png`
   から該当箇所のスクショを Read
3. 確認した画像をユーザに表示してから完了報告

スクショ取得テストの種類:

| テスト | 撮れる画面 |
|---|---|
| TitleScreen_ShowsLanguageButton | タイトル / モード選択 |
| HumanPlay_PlacesPieceAndFlipsAndUpdatesScore | 1手後の in-game (TopBar / mission strip / board) |
| Pass_HandlePassPublishesEventAndSwitchesTurn | パス時の TurnIndicator 切替 |
| GameOver_FiresWhenNeitherPlayerHasMoves | GameOver パネル全体 |
| MissionAchieved_ShowsSnackbarOverlay | ミッション達成スナックバー |

UI 大改修や複数テーマ確認は `playmode-themes` で 4 テーマ × 10 PNG 一括生成。
[Tests/Design-Themes/COMPARE.md](Tests/Design-Themes/COMPARE.md) に並べて貼り出す。

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
