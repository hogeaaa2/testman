# Review: 4d143e4 `feat: add verification test list`

## 結論

指摘なし。コミット `4d143e4` は、今回確認した範囲で要求、Web UI方針、データベース仕様、関連Accepted ADRに適合している。

## 指定観点の確認結果

### Test ID・title・仕様ファイル・最新結果・最終実施日時

- verification modeの先頭に、Test ID、title見出し名、仕様ファイルパス、最新結果、最終実施日時を持つ横断一覧を表示する。
- titleと仕様ファイルは、ケースを生成した現在のtitle/file presentationから取得しており、DBへ仕様本文を複製していない。
- Pass、Fail、Blocked、N/Aは既存の固定 `OutcomeLabel` を表示する。
- 実施日時はDBからUTCとして復元後、既存の `ToLocalTime()` 済み値を `g` 形式で表示する。
- 複数ファイルにまたがる一覧、title/file、結果、ローカル時刻をpresentation testで確認している。

### 内部ID最大の最新結果

- 一覧は各 `VerificationCase` の `PreviousResult` を `LatestResult` として使う。
- `PreviousResult` は既存の `ResultHistoryStore.ReadHistory` が `test_results.id DESC` で返す履歴の先頭である。
- 実施日時やGit revisionではなく、仕様どおり内部ID最大の実施記録が最新結果と最終実施日時になる。
- Storeの既存テストは、実施日時が古くても後から追加された内部ID最大の記録を最新として返すことを確認している。

### 履歴なしのNot Tested

- `LatestResult` がnullの場合、結果列へ `Not Tested`、最終実施日時列へダッシュを表示する。
- Not TestedをDBの結果値として保存せず、現在ケースに履歴がない状態から導出している。
- presentation testで履歴ありと履歴なしを同じ一覧内で確認している。

### 無効・重複IDの除外

- 一覧の母集合は現在表示中の `VerificationCases` のうち `CanRegisterResult=true` のケースだけである。
- parserが不正行、重複ID、安全に識別できないケースを登録不可にするため、それらは一覧へ入らない。
- 現在のMarkdownから一覧を作るため、現在仕様から削除済みのIDも一覧へ入らず、DB履歴自体は保持される。
- 同じfilterはファイル状況集計と結果登録にも使われ、画面間で母集合が一致する。

### 安全な表示

- Test ID、title、仕様ファイル、結果ラベル、時刻はRazorの通常出力を使用し、HTMLエスケープされる。
- 新しい一覧では `Html.Raw` を使用せず、仕様由来のtitle/file文字列やDB値を実行可能なHTMLとして扱わない。
- 結果ラベルは列挙値から生成する固定文字列である。

### パターンモードでのTest ID非表示

- test list全体が `Model.IsVerificationMode` 条件内にあり、pattern modeでは描画されない。
- 既存の詳細テーブル、結果入力hidden fieldsもverification分岐内にあり、pattern modeではTest IDを出力しない。
- 既存HTTPテストは既定のpattern modeレスポンスに `TC-1` が含まれないことを確認しており、新しい一覧追加後も成功している。

## その他の確認

- dirty仕様では読取用identityから既存履歴を表示でき、非Git仕様では本文表示を継続してNot Testedを導出する既存動作を維持する。
- DB読取障害をNot Testedへ偽装せずsystem errorへ送る既存経路に変更はない。
- 表示専用の変更でschema変更はなく、新規migrationは不要。
- 実行時DB、秘密情報、生成物は対象差分へ追加されていない。
- 別作業の未追跡 `.agents/skills/write-test-spec/` は確認・変更対象に含めていない。

## 確認範囲

- `AGENTS.md`
- `.agents/reviewer.md`
- `.agents/skills/review-implementation/SKILL.md`
- `docs/requirements.md`
- `docs/web-ui.md`
- `docs/database.md`
- Accepted ADR-001、ADR-002、ADR-003
- コミット `4d143e4` と関連するparser、履歴読取、presentation、Razor Page、Web統合テスト

## 検証結果

- `dotnet test Testman.sln --no-restore`: 成功
- Testman.Core.Tests: 116 passed
- Testman.Web.Tests: 30 passed
- 合計: 146 passed、0 failed、0 skipped

## 残存リスク

一覧専用テストは正常な複数ファイル、履歴あり・なしを確認している。重複・不正IDの一覧除外と悪意あるtitle/file文字列の実HTTPレンダリングは、同じ `CanRegisterResult` filter、Razorエスケープ、既存parser/HTTP安全性テストから適合を確認したが、一覧固有の回帰テストとしては追加されていない。今後一覧の描画方法や母集合を独立変更する場合は明示的なテスト追加が安全である。
