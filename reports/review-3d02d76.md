# Review: 3d02d76 style: remove diagnostics status badge

## 再レビュー: 20ff5a5 test: preserve diagnostics presentation

初回レビューのMinor指摘は解消された。新規指摘なし。

- 診断ありのHTTP統合テストで、`Specification diagnostics` 見出しと `alert-warning` を直接assertしている。従来の診断ファイル名・理由および `Ready` / `Needs attention` 非表示のassertも維持されている。
- 診断なしのHTTP統合テストで、同じ見出し、`No parsing diagnostics.`、`alert-light` を直接assertしている。
- これにより、バッジだけを削除し、診断見出し・空状態・警告表示を維持する合意内容が、診断あり・なしの両経路で回帰テストに固定された。
- `dotnet test Testman.sln -c Release --no-restore` を再実行し、Testman.Core.Tests 123件、Testman.Web.Tests 36件、合計159件が成功した。

## 結論

実装上の要求不適合なし。Ready / Needs attentionバッジだけが削除され、Specification diagnosticsの本体は維持されている。初回レビューのMinor 1件は20ff5a5で解消された。

## Findings

### Minor（20ff5a5で解消）: 診断見出し・空状態・警告classの維持が自動テストで固定されていない

- 影響箇所: `tests/Testman.Web.Tests/CliWebHostTests.cs:90-103`、`tests/Testman.Web.Tests/CliWebHostTests.cs:182-197`
- 関連要求: 仕様の解析診断を画面上部へ目立つ形で表示し、正常時も診断領域の空状態を表示する。今回の合意ではstatus badgeだけを削除する。
- 説明: 現在のHTTP統合テストは `Ready` / `Needs attention` がないことと、診断発生時のファイル名・理由を確認するため、バッジ削除と診断一覧の残存は検出できる。一方で次を直接assertしていない。
  - `Specification diagnostics` 見出し。
  - 診断0件時の `No parsing diagnostics.`。
  - 診断ありで `alert-warning`、診断なしで `alert-light` になる条件付きスタイル。
- 影響: 後続変更で空状態、見出し、警告の視覚的強調を誤って削除しても既存テストが成功し得る。診断本文自体はテスト済みで主要機能は成立するためMinorとした。
- 推奨: 診断あり・なしのHTTPレスポンスについて、共通見出し、各状態の本文、対応するalert classをそれぞれassertする。

## 実装確認

- 上部diagnostics sectionと `aria-labelledby="diagnostics-heading"` は維持されている。
- `Specification diagnostics` 見出しとそのidは維持されている。
- 診断0件では `No parsing diagnostics.` を表示する。
- 診断ありではSourcePath、可能な場合のLineNumber、Reasonを一覧表示する。各値は通常のRazor式でHTMLエンコードされる。
- sectionのclassは診断ありで `alert-warning`、なしで `alert-light` を選ぶため、問題がある場合の警告スタイルを維持する。
- 削除されたのは右側の `Ready` / `Needs attention` badgeだけで、仕様ファイルの部分表示、診断生成、結果登録feedbackには変更がない。
- `docs/requirements.md` と `docs/test-format.md` が要求する画面上部の診断表示、およびAccepted ADR-003の構造エラー診断方針に適合する。

## 検証結果

- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 123件成功、失敗0件
  - Testman.Web.Tests: 36件成功、失敗0件
  - 合計: 159件成功、失敗0件
