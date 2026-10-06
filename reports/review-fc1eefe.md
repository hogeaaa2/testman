# Review: fc1eefe fix: load generated scoped styles

## 結論

指摘なし。Web assembly名 `testman` に対応するscoped CSS bundleを参照する修正であり、実際に生成されたフィンガープリント付きURLをHTMLから取得して配信内容まで確認する回帰テストが追加されている。既存のBootstrap、site.css、JavaScript配信への変更はない。

## Findings

なし。

## 実装確認

- `src/Testman.Web/Testman.Web.csproj` は `<AssemblyName>testman</AssemblyName>` を明示している。Razor CSS isolationが生成するbundleの論理名はassembly名に基づくため、layoutの参照先 `~/testman.styles.css` と一致する。
- 旧参照 `~/Testman.Web.styles.css` はproject名由来で、assembly名を上書きした現在の構成とは一致しない。今回の変更は参照名だけを正しいbundleへ合わせている。
- layoutは引き続きTag Helper/static web assetsによるURL解決を使用する。実行時HTMLでは `testman.<fingerprint>.styles.css` へ解決され、`MapStaticAssets()` / `WithStaticAssets()` のendpointから取得できる。
- 回帰テストはHTML内の実際のフィンガープリント付きURLを抽出し、そのURLへHTTP GETして、scoped CSS由来の `a.navbar-brand` が含まれることを確認する。単なるlink文字列の存在ではなく、bundle生成・URL変換・配信までを通して検証している。
- テストは固定hashを仮定せず、生成されたURLを使うため、内容変更によるfingerprint更新に耐える。
- Bootstrap参照 `~/lib/bootstrap/dist/css/bootstrap.min.css` と、`asp-append-version` を使う `~/css/site.css` は変更されていない。既存HTTPテストはBootstrapのローカルURL、CDN非依存、site.cssの取得とVerification用CSS内容を引き続き確認する。
- CSSの読み込み順はBootstrap、site.css、scoped bundleのままであり、既存のcascade順序を変えない。
- アプリケーションの仕様解析、履歴、フォーム、HTMLサニタイズには影響しない。

## 仕様・README照合

- `docs/requirements.md` / `docs/web-ui.md`: ブラウザで読みやすく仕様を表示する既存UI要件を成立させる修正であり、画面挙動やデータの意味は変更しない。
- `README.md`: assembly/executable名として使用している `testman`、ローカルWeb画面をブラウザで利用する手順と整合する。
- Accepted ADR-001: ASP.NET Core Razor Pagesの静的アセット機構をそのまま利用し、追加の外部配信やビルド工程を導入しない。
- Accepted ADR-002/ADR-003: 結果永続化、テスト仕様形式、安全なMarkdown表示への影響はない。

## 検証結果

- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 127件成功、失敗0件
  - Testman.Web.Tests: 39件成功、失敗0件
  - 合計: 166件成功、失敗0件

