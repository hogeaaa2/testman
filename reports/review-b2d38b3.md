# Review: b2d38b3 style: fix verification table layout

## 再レビュー: 7926f03 test: verify verification table layout

初回レビューのMinorは解消された。新規指摘なし。

- Pattern modeのHTMLに `verification-table` が存在しないことをassertし、対象外表へ適用しない境界を固定した。
- Verification modeでは `verification-table` の出現が1回だけであることをassertし、Test listへ誤適用されないことを固定した。
- ID、Steps、Expected、Previous、Result、Commentの各classについて、それぞれ6/28/26/12/12/16%との正確な対応を個別にassertする。
- `.verification-table` の `min-width: 72rem` と `table-layout: fixed`、既存の `overflow-wrap: anywhere` をassertし、狭い画面と長文表示の主要CSSを固定した。
- `dotnet test Testman.sln -c Release --no-restore` を再実行し、Core 125件、Web 37件、合計162件が成功した。
- `dotnet build Testman.sln -c Release --no-restore` を単独で再実行し、警告0・エラー0で成功した。

## 結論

実装上の要求不適合は確認されなかった。Verification表だけに固定レイアウトと6/28/26/12/12/16%の列幅を適用し、72rem未満では既存のresponsive wrapperで横スクロールさせる。Previous resultの要約は最新結果badgeまたはNot Testedだけとなり、日時・実施者・対象名・コメントは展開履歴に維持されている。

初回レビューで指摘したテスト不足は7926f03で解消され、未解決指摘はない。

## Findings

### Minor（7926f03で解消）: 列幅の正確な対応・最小幅・他表への非適用がテストで固定されていない

- 影響箇所: `tests/Testman.Web.Tests/CliWebHostTests.cs:158-194`
- 関連要求: `docs/web-ui.md` のVerification表だけを6/28/26/12/12/16%へ固定し、72remの最小幅と横スクロールを設け、Test listとPattern表には適用しない要求。
- 説明: HTTPテストはcol classの順序とCSS内の `6%`、`28%`、`12%`、`16%` の存在を確認するが、Steps=28%、Expected=26%、Result=12%というclassと値の対応をすべて確認していない。特に `26%`、`min-width: 72rem`、`table-layout: fixed` は未assertで、`verification-table` がTest listやPattern表へ付かないことも直接確認していない。
- 影響: 列幅の入れ替え、Expected列の比率変更、最小幅や固定レイアウトの削除、対象外表へのclass誤付与が起きてもテストが成功し得る。現行HTML/CSSは正しいためMinorとした。
- 推奨: 各 `.verification-col-*` selectorと対応widthを個別にassertし、`.verification-table` の72rem/fixed/折返しをassertする。Verificationページでclass出現数またはTest list tableのclassを確認し、Patternページでも非適用を確認する。
- 7926f03での解消状況: 各列classと幅の対応、72rem/fixed/折返し、Pattern modeで0件、Verification modeで1件を直接assertするようになり、指摘した全境界が固定された。

## 実装確認

- Verificationのテストケース表だけに `verification-table` と6列の `colgroup` を追加している。Test listおよびPattern表のtable classは変更していない。
- 列幅はTest ID 6%、Steps 28%、Expected result 26%、Previous result 12%、Result input 12%、Optional comment 16%で、合計100%となる。
- `.verification-table` は `min-width: 72rem` と `table-layout: fixed` を持つ。親のBootstrap `table-responsive` は維持されているため、狭い画面では入力可能な幅を保ったまま表全体を横スクロールできる。
- セルへscopedな `overflow-wrap: anywhere` を適用し、長い文章やURLをセル内で折り返す。グローバルtable、Test list、Pattern表示へは適用しない。
- Previous resultの要約から日時・実施者・対象名を削除し、履歴ありでは最新Outcome badgeだけ、履歴なしでは既存のNot Tested badgeだけを表示する。
- Historyの各項目はOutcome、ローカル日時、実施者名、対象名、任意コメントを引き続き表示する。履歴からSHAを表示しない既存仕様も維持する。
- 履歴文字列は通常のRazor式で出力される。既存HTTP統合テストは悪意ある実施者名・対象名・コメントがHTMLエンコードされ、生HTMLとして実行可能にならないことを確認している。
- フォームのfield名・index・select・textareaには変更がなく、結果登録やpartial confirmationへの影響はない。

## 仕様照合

- `docs/web-ui.md`: Previous result要約、History詳細、列幅、長文折返し、最小幅と横スクロール、他表への非適用という最新合意に適合する。
- `docs/requirements.md`: 詳細表示、結果入力、履歴表示、不正HTMLを実行させない要求を維持する。
- Accepted ADR-001/ADR-002/ADR-003: Razor Pagesでの表示、追記型履歴、仕様由来HTMLの安全化という方針への回帰はない。

## 検証結果

- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 125件成功、失敗0件
  - Testman.Web.Tests: 37件成功、失敗0件
  - 合計: 162件成功、失敗0件
