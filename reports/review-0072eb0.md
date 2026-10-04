# Review: 0072eb0 style: simplify page header

## 再レビュー: f97620e test: verify page mode controls

初回レビューのMinorは解消された。新規指摘なし。

- Pattern modeで右寄せheader、group role/label、両リンクのhref、Test patterns=`aria-pressed="true"`、Verification=`false` を直接assertする。
- Verification modeでは同じリンクについてpressed状態が反転することを直接assertする。
- Razorのbool値をそのまま属性へ渡さず、小文字の文字列 `true` / `false` を明示的に出力するため、HTML上に常に有効なARIA stateが残る。
- `dotnet build Testman.sln -c Release --no-restore` は警告0・エラー0で成功した。
- `dotnet test Testman.sln -c Release --no-restore` はCore 125件、Web 37件、合計162件が成功した。

## 結論

実装上の要求不適合は確認されなかった。左側の `Test specifications`、`Specification workspace`、説明文だけが削除され、Test patterns / Verificationの切替は右寄せで維持されている。リンクのキーボード操作、group label、現在モードの状態表現も残る。

初回レビューで指摘したテスト不足はf97620eで解消され、未解決指摘はない。

## Findings

### Minor（f97620eで解消）: 切替の右寄せとアクセシビリティ属性がHTTPテストで固定されていない

- 影響箇所: `tests/Testman.Web.Tests/CliWebHostTests.cs:101-106`
- 関連要求: 合意されたTest patterns / Verification切替の維持・右寄せ、および現在モードを識別できるアクセシビリティ。
- 説明: 追加テストは削除した3文言が存在しないことを確認し、既存assertで2つの表示名が存在することを確認する。一方、headerの `justify-content-end`、切替groupの `role="group"` / `aria-label="Specification view mode"`、各リンクのhrefとモード別 `aria-pressed` 値を直接assertしていない。
- 影響: 後続変更で切替が左寄せへ戻る、group labelや現在モード状態が失われる、リンク先が入れ替わる回帰があっても、現在の文言assertだけでは検出できない。現行HTMLは合意を満たすためMinorとした。
- 推奨: PatternとVerificationの両レスポンスでheader class、group label、各href、相互に反転する `aria-pressed="True"` / `"False"` をassertする。
- f97620eでの解消状況: header class、group role/label、両href、両モードで反転する小文字 `aria-pressed="true"` / `"false"` がHTTPレスポンスで固定され、指摘は解消した。

## 実装確認

- 削除対象はheader左側のeyebrow、h1、説明文だけで、diagnostics、Test list、仕様カード、フォームなど他の画面要素は変更していない。
- headerは `d-flex justify-content-end mb-4` となり、Bootstrap flex utilityで切替groupを右端へ配置する。
- Test patternsは `/`、Verificationは `/?mode=verification` への通常リンクとして残り、JavaScriptなし・キーボード操作可能なモード切替を維持する。
- groupには `role="group"` と `aria-label="Specification view mode"` があり、2リンクの関係を補足する。
- 現在モードはprimary/outlineの視覚差に加え `aria-pressed` で示し、PatternとVerificationで真偽を反転する既存挙動を維持する。
- `<title>` 用の `ViewData["Title"] = "Specifications"` は維持され、ブラウザ上のページ識別情報は失われていない。
- 仕様由来データの描画、フォーム、履歴、HTML安全化には変更がない。

## 仕様照合

- `docs/web-ui.md`: 同じ仕様をTest patterns / Verificationで切り替える要求を維持する。
- `docs/requirements.md`: パターンレビュー表示と検証実施表示を切り替えられる要求に適合する。
- Accepted ADR-001: JavaScriptへ依存しないRazor Pagesのサーバーサイド画面構成を維持する。
- Accepted ADR-002/ADR-003: 結果履歴、仕様形式、安全なMarkdown表示には影響しない。

## 検証結果

- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 125件成功、失敗0件
  - Testman.Web.Tests: 37件成功、失敗0件
  - 合計: 162件成功、失敗0件
