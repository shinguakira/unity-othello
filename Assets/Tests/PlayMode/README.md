# Othello PlayMode E2E

Unity Test Framework だけで動く E2E。AltTester / Desktop / ライセンス不要。

## 走らせ方（GUI）

1. Unity Editor で `Window > General > Test Runner`
2. `PlayMode` タブに切り替え
3. `OthelloE2EPlayModeTests` を選択 → `Run Selected` または `Run All`
4. 4 本のテストが Game シーンを毎回ロードして実行
5. 各テスト完了時に **スクショが PNG で保存**される

## スクショと結果ファイルの場所

スクショ:
```
%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\TestArtifacts\<TestName>.png
```

具体的なパスは Unity Console の `[E2E] Artifacts dir: ...` ログに毎回出力。

**注意 — スクショは Editor 内 Test Runner で走らせた時のみ取れる**。
`-batchmode` (= `unity.ps1 playmode`) では `ScreenCapture.CaptureScreenshot`
が 0 バイトのプレースホルダを作るだけで実際のフレームをキャプチャできない
（バックバッファが無いため）。CI / AI 自動修正ループでは **アサーションが
唯一の真実**として機能。視覚確認したい時は Editor で Test Runner を開いて
PlayMode → Run All を実行する。

テスト結果 XML（CLI 実行時）:
```
<projectRoot>/test-results-playmode.xml
```

## 走らせ方（CLI / AI ループ用）

Unity Editor を**閉じてから**実行:

```sh
"C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" \
  -batchmode -nographics \
  -projectPath E:/workspace/unity-othello \
  -runTests -testPlatform PlayMode \
  -testResults E:/workspace/unity-othello/test-results-playmode.xml \
  -logFile E:/workspace/unity-othello/test-playmode.log
```

`-quit` は `-runTests` と併用しない（Unity が自動で終わる）。
`-nographics` でも `ScreenCapture` は動く（Unity 内部で frame buffer を用意するため）。

## テスト構成

| テスト | 検証内容 |
|---|---|
| `TitleScreen_ShowsLanguageButton` | タイトルに `Btn_lang_mode` が表示されている |
| `TopBar_DoesNotContainLanguageButton` | **回帰テスト**: in-game に `Btn_lang_btn` が存在しない |
| `VsAi_StartsAndShowsHomeButton` | vs AI ボタン押下で HOME 表示 + ModeSelect 非表示 |
| `VsAi_BoardPresentsValidMoveDots` | 開局時の有効手 4 点 |

## なぜ AltTester でなく PlayMode

- AltTester Desktop v2.3+ は **ライセンス必須** （メール待ち中）
- AltTester SDK 2.x は WebSocket クライアントとして dial out するだけ — Standalone モード不在（SDK 実装で確認済）
- PlayMode は Unity 標準で license なしで動く
- 取れる情報: GameObject ツリー直接アクセス、ScreenCapture でスクショ
- 制約: Unity Editor を起動してる必要がある（または CLI で別プロセス）

ライセンス到着後に AltTester に乗り換えるなら `Tests/AltTester/` に既に scaffold あり。クリック発火を `AltDriver.Tap` に置換するだけで移植できる。
