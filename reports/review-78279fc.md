# Review: 78279fc `feat: show result history in verification view`

## 最終結論

指摘なし。テストコミット `2be8c6d` により、初回レビューのMinor 1件は解消した。対象コミットとテストコミットは、今回確認した範囲で要求、Web UI方針、データベース仕様、関連Accepted ADRに適合している。

## 初回指摘の再確認

### 解消: 履歴表示のHTML安全性と障害時HTTP表示を統合テストしていない

- 対象:
  - `tests/Testman.Web.Tests/SpecificationPageContentTests.cs`
  - `tests/Testman.Web.Tests/CliWebHostTests.cs`
  - `src/Testman.Web/Pages/Index.cshtml:91-115`
  - `src/Testman.Web/Presentation/SpecificationPageContentSource.cs:17-28`
- 根拠:
  - `docs/web-ui.md`「利用者の入力エラーとシステム障害を区別する」
  - `docs/web-ui.md`「内部例外やスタックトレースを通常画面へそのまま表示しない」
  - `docs/requirements.md`「不正なHTMLやスクリプトをブラウザで実行させない」
  - `.agents/skills/review-implementation/SKILL.md` はdatabase failuresと関連するWeb security casesのテスト確認を要求する
- 説明: 追加テストはpresentation modelへ整形済み履歴を渡し、最新結果・ローカル時刻・履歴順を確認しているが、RazorでレンダリングされたHTTPレスポンスを確認していない。このため、ユーザー入力由来の `ExecutedBy` と `Comment`、DB由来の `SpecificationRevision` がHTMLエスケープされることや、実行中の履歴DB読取失敗が「Not Tested」または成功画面にならず、内部例外を含まないシステム障害表示になることが回帰テストで固定されていない。dirty/nonGit仕様に対する `SpecificationPageContentSource` の継続動作もWeb統合経路では未確認である。
- 影響: 現在のRazorはこれらの値を通常の `@` 出力で扱うため現時点のHTMLは安全であり、SQLite例外もcatchされず例外ハンドラへ進む。しかし将来 `Html.Raw` の誤用、例外の過剰な握りつぶし、環境設定変更による詳細例外露出が起きても、現在のテストでは検出できない。
- 修正確認:
  - `<script>` を含む実施者名とイベント属性を含むコメントを保存し、verification画面ではエスケープ済み文字列になり、実行可能な要素として現れないことをHTTPレスポンスで確認している。
  - 保存後に仕様を変更してdirtyにした状態でも、更新後の仕様本文と保存済み履歴が同じverification画面に表示されることを確認している。
  - 非Git仕様でもverification画面が表示され、Not Testedになることを確認している。
  - 起動後にDBパスをディレクトリへ置き換えて読取を失敗させ、HTTP 500、非Not-Tested表示、SQLite内部型名の非露出を確認している。
- 判定: 解消。

## 新規回帰の確認

新たなCritical、Major、Minorの指摘はない。テスト追加のみで製品コードの挙動は変更されていない。

## 指定観点の確認結果

### 未コミット・非Git仕様の閲覧継続

- dirtyなGit仕様は `GitSpecificationIdentity.Resolve` で識別でき、履歴を読み取れる。
- 非Git仕様はidentity解決時の `InvalidOperationException` を `SpecificationPageContentSource` が履歴なしへ変換するため、仕様本文の表示は継続し、前回結果はNot Testedとなる。
- `SqliteException` 等のDB障害はこのcatch対象ではなく、履歴なしとして誤表示されない。

### 最新結果の定義

- `ResultHistoryStore.ReadHistory` が `test_results.id DESC` で返し、presentation modelは先頭を `PreviousResult` とする。
- 実施日時ではなく内部ID最大の記録を最新とする `docs/web-ui.md` / `docs/database.md` の定義に一致する。

### ローカル時刻

- DBからUTCとして復元した `ExecutedAtUtc` を `ToLocalTime()` で実行環境のローカル時刻へ変換している。
- presentation testで変換結果が確認されている。

### HTML安全性

- Markdown由来のPreconditions、Steps、Expected Resultだけが `SafeMarkdownRenderer` のサニタイズ後に `Html.Raw` へ渡る。
- 履歴由来の実施者名、コメント、revision、固定列挙値、日時はRazorの通常出力でHTMLエスケープされる。
- ただし上記Findingのとおり、実際のレンダリング結果のセキュリティテストはない。

### DB障害表示

- identity解決失敗だけを履歴なしとして扱い、DB読取例外は握りつぶさない。
- Productionでは既存の `UseExceptionHandler("/Error")` により通常画面とは異なる汎用システムエラーへ遷移し、エラーページは例外本文やスタックトレースを描画しない。
- ただし実行中DB障害のHTTP統合テストはない。

## 確認範囲

- `AGENTS.md`
- `.agents/reviewer.md`
- `.agents/skills/review-implementation/SKILL.md`
- `docs/requirements.md`
- `docs/web-ui.md`
- `docs/database.md`
- Accepted ADR-001、ADR-002、ADR-003
- 実装コミット `78279fc`、テストコミット `2be8c6d` と関連する履歴読取、presentation、Razor Page、Web host実装

## 検証結果

- `dotnet test Testman.sln --no-restore`: 成功
- Testman.Core.Tests: 116 passed
- Testman.Web.Tests: 21 passed
- 合計: 137 passed、0 failed、0 skipped

## 残存リスク

結果入力・登録フォームはこのコミットの対象外であるため、CSRF、未選択ケース確認、POST/Redirect/Get、DB保存失敗の画面表示は将来の登録UIコミットで別途レビュー対象とする。
