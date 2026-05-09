# E2E 実行レポート — 2026-05-08

## サマリー

| 項目 | 結果 |
|---|---|
| .NET SDK | ✅ 8.0.419 検出 |
| `dotnet restore` | ✅ 成功 (NuGet 復元 8.15s) |
| `dotnet build` | ❌→✅ 1 回失敗（namespace 誤り）→ 修正後成功 |
| テスト発見 | ✅ NUnit が 4/4 検出 |
| **テスト実行** | ❌ **4/4 失敗** — `ConnectionException`（AltTester Server 未起動 / Unity 未 Play） |
| **スクショ取得** | ❌ **0 件** — 接続不能のため `GetPNGScreenshot` まで到達せず |

**結論: スクリーンショットは取れていない。** Unity Editor を起動して AltTester instrumentation を有効にし、AltTester Server デスクトップアプリを起動するまで E2E は走らない。これは AltTester の仕組み上、ローカル環境を私の側で立ち上げられないため。

---

## 確認できたこと（コード/設定レイヤ）

### 1. NuGet パッケージ解決
`AltTester-Driver 2.3.1` が NuGet から取得できた:
```
C:/Users/user2/.nuget/packages/alttester-driver/2.3.1/lib/net5.0/AltDriver.dll
C:/Users/user2/.nuget/packages/alttester-driver/2.3.1/lib/netstandard2.0/AltDriver.dll
```

### 2. Namespace 修正
最初に書いた `using AltTester.AltTesterUnitySDK.Driver;` はビルドエラー。実際のアセンブリの namespace は `AltTester.AltTesterSDK.Driver`（DLL の strings ダンプで確認）。修正後ビルド成功。

### 3. ビルド成功
```
OthelloE2E -> E:\workspace\unity-othello\Tests\AltTester\bin\Debug\net8.0\OthelloE2E.dll
0 Warning(s) / 0 Error(s)
```

### 4. テスト発見
```
NUnit3TestExecutor discovered 4 of 4 NUnit test cases
```
4 本すべて発見:
- `TitleScreen_ShowsLanguageButton`
- `TopBar_DoesNotContainLanguageButton`
- `VsAi_StartsAndShowsHomeButton`
- `VsAi_BoardPresentsValidMoveDots`

---

## 失敗の中身

4 本すべて `[SetUp]` で `new AltDriver(host: "127.0.0.1", port: 13000)` の段階で落ちた:

```
AltTester.AltTesterSDK.Driver.ConnectionException :
  An error has occurred during the OnClose event.
  at DriverWebSocketClient.Connect()
  at AltDriver..ctor(...)
  at OthelloE2ETests.SetUp() line 34
```

各テスト約 1分9秒（接続タイムアウトのデフォルト）× 4 = 約 5 分。

`netstat -an` で確認したところ **`127.0.0.1:13000` は LISTENING していない**。期待通り — AltTester Server が起動していない。

`artifacts/` ディレクトリも作られていない（`[TearDown]` の `GetPNGScreenshot` まで到達していないため）。

---

## いま AI / 自動化のループに足りないもの

私（Claude Code）の側からは下記が原理的にできない:

1. **Unity Editor の起動・操作**（`AltTester > AltTester Editor` メニューを開く、トグルする）
2. **AltTester Server デスクトップアプリの DL & 起動**（独立 GUI アプリ、ポート 13000）
3. **Unity Play 開始**（Editor の Play ボタン）

これらは人間（または別の自動化、例えば computer-use MCP）が一度やる必要がある。

代替案として、**`AltTester Server` を起動する CLI / Docker イメージが配布されてるか**を調査するなら、それは可能。もし CLI 版があれば bash から起動でき、AI ループを完結できる。

---

## 次にやるべきこと（人間タスク）

1. Unity Editor で本プロジェクトを開く（Packages 解決のため）
2. `AltTester > AltTester Editor` メニューで現在の build target に対し AltTester ON
3. <https://alttester.com/alttester-server/> から Server DL → 起動
4. Unity で `Assets/Scenes/Game.unity` を開いて Play
5. 別ターミナルで:
   ```sh
   cd E:/workspace/unity-othello/Tests/AltTester
   dotnet test
   ```
6. `bin/Debug/net8.0/artifacts/*.png` にスクショが 4 枚生成される

ここまで来たら、あとは私が `dotnet test` を回して artifacts を読んでバグ判定 → コード修正 → 再実行のループに入れる。

---

## 生ログ

完全な失敗ログ:

```
Failed TitleScreen_ShowsLanguageButton [1 m 9 s]
Failed TopBar_DoesNotContainLanguageButton [1 m 9 s]
Failed VsAi_StartsAndShowsHomeButton [1 m 9 s]
Failed VsAi_BoardPresentsValidMoveDots [1 m 9 s]
（全件 ConnectionException — Server 127.0.0.1:13000 に到達できず）
```
