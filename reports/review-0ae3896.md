# Review: 0ae3896 `feat: submit results from verification view`

## 最終結論

指摘なし。修正コミット `47a194f` により、初回レビューのMajor 1件は解消した。対象コミットと修正コミットは、今回確認した範囲で要求、Web UI方針、データベース仕様、テスト形式、関連Accepted ADRに適合している。

## 初回指摘の再確認

### 解消: 部分選択の確認フラグがModelStateの旧値に上書きされ、確認操作を完了できない

- 対象:
  - `src/Testman.Web/Pages/Index.cshtml.cs:39-66`
  - `src/Testman.Web/Pages/Index.cshtml:64-65`
  - `tests/Testman.Web.Tests/CliWebHostTests.cs:203-244,351-371`
- 根拠:
  - `docs/requirements.md`「登録対象となるテストケースの一部に結果未選択がある場合は、選択済みケースだけを登録する前に確認を求める」
  - `docs/web-ui.md`「登録可能なケースの一部に未選択がある場合は、選択済みケースだけを登録するか確認する」
- 説明: 最初のPOSTでは `ConfirmPartial=false` がモデルバインドされ、ModelStateにもattempted valueとして残る。Coordinatorが確認要求を返した際、handlerはプロパティを `ConfirmPartial = true` に変更して `Page()` を返すが、`<input asp-for="ConfirmPartial" type="hidden" />` のInput Tag HelperはモデルプロパティよりModelStateのattempted valueを優先する。そのためボタン表示はプロパティを見て「Confirm selected results」へ変わる一方、hidden inputはfalseのままレンダリングされる。次のPOSTでもfalseが送信され、再び確認要求になる。
- 再現:
  1. 2ケースのうち1ケースだけ結果を選び、「Save results」を押す。
  2. 確認メッセージと「Confirm selected results」ボタンが表示される。
  3. レスポンス内の `ConfirmPartial` hidden inputは、ModelStateが保持するfalseになる。
  4. 確認ボタンを押してもCoordinatorへfalseが渡り、保存されず同じ確認画面へ戻る。
- テストが検出しない理由: 統合テストの `SubmissionValues(confirmationHtml, confirmPartial: true)` は、確認レスポンスのhidden値を解析せず、テストコードからtrueを直接注入している。したがって実ブラウザのフォーム再送信を再現していない。
- 影響: 一部だけ結果を選択して保存する仕様上の主要フローがUIから成立しない。全選択保存には影響せず、誤保存や部分保存は発生しない。
- 修正確認:
  - 確認要求時に `ConfirmPartial = true` を設定した後、`ModelState.Remove(nameof(ConfirmPartial))` でPOST時の旧attempted valueを除去している。
  - hidden inputは更新後のmodel値trueを描画し、次のPOSTで確認済み状態がCoordinatorへ渡る。
  - HTTP統合テストは引数からtrueを注入せず、初回GETと確認レスポンスそれぞれの `ConfirmPartial` hidden値をHTMLから抽出してそのまま再POSTする。
  - 実フォーム相当の2回目POSTで保存、PRG後の成功表示、履歴表示、DB 1件追記まで確認されている。
- 判定: 解消。

## 新規回帰の確認

新たなCritical、Major、Minorの指摘はない。削除するModelState entryは確認フラグだけであり、結果選択、コメント、実施者名のModelStateは維持されるため入力保持に影響しない。保存後のPRG、antiforgery tokenの再取得、一括保存にも回帰はない。

## 指定観点の確認結果

### フォーム初期未選択・全未選択

- GET時は現在の登録可能ケースから新しいform modelを作り、Outcomeはnullのため各selectは「Select result」が初期値となる。前回結果は入力値へコピーされない。
- 全件未選択はCoordinatorでInvalidとなり、保存せずModelStateエラーを表示する。

### 部分選択確認・入力保持

- 選択値、コメント、実施者名はPOSTのModelStateにより確認・入力エラー画面で保持される。
- 確認メッセージと確認用ボタン文言は表示される。
- 修正後は確認済みフラグもhidden inputで正しく次のPOSTへ渡る。

### PRG

- 保存成功時はTempDataへ成功メッセージを入れ、verification modeへ `RedirectToPage` する。
- GET後に履歴と成功メッセージが表示され、ブラウザ更新による同じPOSTの再送信を避ける構造になっている。
- 現在の統合テストはHttpClientの自動リダイレクト後を確認しており、明示的な302確認はないが実装はPRGになっている。

### CSRF

- Razor Pagesのform tag helperがantiforgery tokenを生成し、Page handlerには無効化属性がないため、既定のantiforgery検証が有効である。
- 統合テストもレスポンスからtokenを取得してPOSTしている。

### 無効ケース・POST改ざん

- `CanRegisterResult=false` のケースにはselect、comment、hidden identityを出さず「Registration unavailable」を表示する。
- POST時はCoordinatorが仕様を再読込し、登録可能な現在ケース集合との完全一致、IDの厳密比較、重複・欠落・追加、outcomeを検証する。
- source pathやGit revisionを投稿値だけで信用せず、選択された仕様をサーバー側で再解決する。

### Git clean検証・一括トランザクション

- 保存直前に選択対象ごとの追跡状態、worktree/indexのHEAD一致、HEAD SHAを再検証する。
- 選択済み結果を1つのsubmissionとしてStoreへ渡し、既存の同一トランザクション処理で追記する。

### DB障害・エラー分類

- 書込み時の `SqliteException` は成功扱いせず、固定のsystem error messageを表示する。
- 入力・現行仕様不一致・Git不適格はModelState入力エラー、部分選択は確認、DB障害はsystem errorとして分けている。
- DB障害時もPOST入力はModelStateに保持される。

### HTML安全性

- 実施者名、コメント、入力値、固定エラーメッセージはRazorの通常出力またはtag helperを通り、HTML属性・本文としてエスケープされる。
- Markdown由来HTMLだけが既存のサニタイズ後に `Html.Raw` へ渡る。
- 投稿されたTest IDやpathは現行仕様不一致時の固定メッセージへ反射されない。

## 確認範囲

- `AGENTS.md`
- `.agents/reviewer.md`
- `.agents/skills/review-implementation/SKILL.md`
- `docs/requirements.md`
- `docs/web-ui.md`
- `docs/database.md`
- `docs/test-format.md`
- Accepted ADR-001、ADR-002、ADR-003
- 実装コミット `0ae3896`、修正コミット `47a194f` と関連するsubmission coordinator、履歴Store、Razor Page、Web統合テスト

## 検証結果

- `dotnet test Testman.sln --no-restore`: 成功
- Testman.Core.Tests: 116 passed
- Testman.Web.Tests: 26 passed
- 合計: 142 passed、0 failed、0 skipped

## 残存リスク

CSRF token欠落時の拒否、全未選択、無効ケース、POST identity改ざん、DB書込み失敗、入力値のHTMLエスケープは下位層またはフレームワーク動作から確認できるが、すべてがHTTP統合テストとして固定されているわけではない。今後フォームを変更する際には、現在追加された実フォーム値再送方式で主要異常系も拡充すると安全である。
