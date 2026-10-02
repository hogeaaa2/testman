# Review: dfb2f7f `feat: summarize current file status`

## 結論

指摘なし。コミット `dfb2f7f` は、今回確認した範囲で要求、Web UI方針、データベース仕様、関連Accepted ADRに適合している。

## 指定観点の確認結果

### フォルダ指定時のみ表示

- `SpecificationPageContentSource` は起動時のworking directoryを基準に仕様入力パスを絶対化し、`Directory.Exists` の結果を `showFileSummaries` としてpresentationへ渡す。
- ディレクトリ指定では各表示対象ファイルにsummaryを作り、単一Markdownファイル指定ではsummaryをnullにする。
- Razorはsummaryがnullでない場合だけ「Current test status」を描画する。
- HTTPテストでディレクトリ指定時の表示と、単一ファイル指定時の非表示が確認されている。

### 現在の有効かつ一意なTest IDだけの母数

- 集計対象は現在読み込んだ各ファイルのtitleに属する `VerificationCases` のうち、`CanRegisterResult=true` のケースだけである。
- parserは有効な `TC-#` 形式を検証し、重複IDや安全に識別できない行を登録不可にするため、summaryのTotalには現在の有効かつ一意なIDだけが入る。
- duplicate IDのファイルでTotalとNot Testedが0になるpresentation testが追加されている。
- 解析不能でtitleとして成立しない内容は表示対象ファイル・ケースの母集合へ入らない。

### 内部ID最大の最新結果

- 各現在ケースのhistoryは既存の `ResultHistoryStore.ReadHistory` から `test_results.id DESC` で取得される。
- presentationはその先頭を `PreviousResult` とし、summaryはこの値だけをPass、Fail、Blocked、N/Aへ分類する。
- 実施日時やGit revisionの大小ではなく、仕様どおり内部ID最大の結果が現在状態になる。
- Pass、Fail、Blocked、N/A、履歴なしの各1件を集計するテストが追加されている。

### 削除済み・不正・重複IDの除外

- DB全体を直接集計せず、現在のMarkdownから得たcaseごとに履歴を結合しているため、現在仕様から削除済みのファイル・Test IDはsummaryへ入らない。DB履歴自体は変更・削除されない。
- 不正行と重複IDは `CanRegisterResult=false` となり母集合から除外される。
- 現在仕様の履歴がない有効IDだけがNot Testedに数えられる。

### 非Git・dirty仕様の閲覧

- dirty仕様は読取用 `GitSpecificationIdentity` を解決できるため、同じrepository root、source file、Test IDの既存履歴からsummaryを表示できる。
- Gitリポジトリ外などidentityを解決できない仕様は、従来どおり履歴なしとして扱い、仕様本文の閲覧を継続する。ディレクトリ入力なら現在の有効IDがNot Testedとして集計される。
- Git clean検証は結果登録時だけに維持され、summary表示によって登録制約は緩和されない。

### DB障害の扱い

- `SpecificationPageContentSource.ReadHistory` が履歴なしへ変換するのはidentity解決に関する `InvalidOperationException` / `ArgumentException` だけである。
- `SqliteException` 等のDB障害は握りつぶされず上位のsystem error経路へ進むため、DB障害をNot Testedとして誤表示しない。
- 既存HTTP統合テストで、実行中DB読取障害がHTTP 500となりNot Testedを表示せず内部SQLite型名も露出しないことが確認されている。

## その他の確認

- TotalはPass + Fail + Blocked + N/A + Not Testedと同じ母集合から計算される。
- summary値は整数としてRazorへ出力され、仕様由来の未サニタイズHTMLを追加していない。
- 読取・集計だけの変更でschema変更はなく、新規migrationは不要。
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
- コミット `dfb2f7f` と関連するparser、履歴読取、presentation、Razor Page、Web統合テスト

## 検証結果

- `dotnet test Testman.sln --no-restore`: 成功
- Testman.Core.Tests: 116 passed
- Testman.Web.Tests: 29 passed
- 合計: 145 passed、0 failed、0 skipped

## 残存リスク

削除済みID、不正な単独行、dirty仕様で既存履歴を集計する組合せは、実装の母集合・既存parser/履歴テストから仕様適合を確認したが、summary専用の個別回帰テストとしては追加されていない。今後集計ロジックを永続化層へ移す場合は、これらを明示的な集計テストとして固定するのが安全である。
