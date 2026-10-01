# Review: 98ed88b + 5733997

## Result

指摘なし。初回レビューのMajor 1件は、修正コミット5733997 `fix: reload specifications per request`で解消された。

## Resolved finding

### Major（解消済み）: 起動後の仕様変更が次回の画面表示へ反映されない

- 初回対象: `src/Testman.Web/Program.cs`
- 根拠:
  - `docs/web-ui.md`: 画面表示時には現在のテスト仕様ファイルを原本として直接読み込む。
  - Accepted `docs/decisions/ADR-001-web-application-architecture.md`: 仕様変更は次回の画面表示へ反映できる。
- 初回説明: 起動時に作った`SpecificationPageContent`をSingletonとして保持していたため、Markdown変更後のページ再読み込みでも古い内容を表示していた。
- 修正確認: `SpecificationPageContentSource.Load`が各GETで`SpecificationCatalog.Load`を呼び出す構成へ変更された。同一Webプロセスを維持したまま仕様titleを`Valid title`から`Updated title`へ書き換える結合テストにより、次回の画面表示で新しいtitleが表示され、古いtitleが消えることを確認した。
- 結果: `docs/web-ui.md`とAccepted ADR-001の、現在の仕様ファイルを直接読み込み、仕様変更を次回の画面表示へ反映する要件を満たす。

## Confirmed behavior

- 引数不正時は終了コード1でWebサーバーを起動せず、使用方法を標準エラーへ出力する。
- 個別ファイルの解析エラーがあってもサーバーを起動し、正常なtitleと診断を同じ画面に表示する。
- `ASPNETCORE_URLS`で指定したloopbackアドレスで起動し、非loopback設定は既存のguardで拒否する構成を維持している。
- title、パス、診断、表セルはRazorでHTMLエンコードされ、Overviewだけがサニタイズ済みHTMLとして`Html.Raw`へ渡される。
- プロセス結合テストは実プロセスの異常終了と、localhost起動後のHTTP表示を確認している。
- 同一プロセスでMarkdownを変更した後、次のHTTP GETで変更内容が反映される。
- Test IDはテストパターン表示へ露出していない。

## Verification

- `dotnet test tests/Testman.Web.Tests/Testman.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~CliWebHostTests" --logger "console;verbosity=detailed"`: 2件成功
- `dotnet build Testman.sln --no-restore`: 成功（警告0、エラー0）
- `dotnet test Testman.sln --no-build --no-restore`: Core 86件、Web 15件成功
- 98ed88bの親から5733997までの累積差分に対する`git diff --check`: 問題なし
- Git管理対象へのDB、`bin`、`obj`、`TestResults`混入: なし

## Residual risk

現段階の画面はテストパターン表示のみであり、検証実施表示、結果入力、履歴表示はこのコミットのレビュー対象外。
