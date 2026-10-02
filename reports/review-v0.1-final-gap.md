# V0.1 Final Gap Review: origin/main 699bbad

## 結論

Major 1件。V0.1の主要機能である仕様パス解決、Markdown解析と部分表示、安全なHTML、2表示モード、一覧・状況集計、結果入力・確認・追記、履歴表示、SQLite migration、Git revision追跡、localhost限定CLI、Windows自己完結配布は現在tipで実装されている。

残る仕様ギャップは、level-1 titleを1件も含まないMarkdownファイルを構造エラーとして診断しないことである。単独の入力ファイルでもWebサーバーが起動し、診断なしの空画面をReadyとして表示する。

## Findings

### Major: titleを1件も持たない仕様ファイルが診断なしで成功扱いになる

- 対象:
  - `src/Testman.Core/Specifications/TestSpecificationParser.cs:24-38,184-197`
  - `src/Testman.Core/Commands/ServeStartup.cs:14-28`
  - `tests/Testman.Core.Tests/TestSpecificationParserTests.cs`
  - `tests/Testman.Core.Tests/ServeStartupTests.cs`
- 根拠:
  - `docs/test-format.md` は仕様ファイルをlevel-1見出しで始まるtitleブロックの集合とし、各titleにOverview、Preconditions、Common steps、テストケース表を必須とする
  - `docs/test-format.md`「対象ファイルを確定した後の…解析エラーはファイルごとの診断とし、正常なファイルとtitleの表示を妨げない」
  - `docs/requirements.md`「対象Markdownファイルの読込または解析で個別のエラーが発生した場合は…画面上部へ診断を表示する」
  - `docs/requirements.md` 品質要求「テスト仕様の解析エラーを利用者が特定できる形で表示する」
- 説明: parserはlevel-1 headingのindex一覧を作り、その各要素だけを検証する。indexが0件の場合はループを一度も実行せず、title 0件・diagnostic 0件の結果を返す。`ServeStartup` の起動可否はMarkdownファイル件数だけで決まり、解析済みtitleやdiagnosticの有無を見ないため、ファイルは入力対象として成功しサーバーが起動する。Web側はtitle 0件のファイルを表示対象から除外しつつ、diagnostic 0件なので「No parsing diagnostics」「Ready」と表示する。
- 再現:
  1. 次の内容だけを持つ `empty.md` を用意する。
     ```text
     Testman-Format-Version: 1
     ```
  2. `testman serve --specs empty.md` で起動する。
  3. サーバーは起動し、画面には仕様titleもテストケースもないが、解析診断なし・Readyと表示される。
- 影響: 必須構造を全く満たさない仕様が正常と誤認される。単独ファイル利用では主要な閲覧・実施フローが成立せず、ディレクトリ利用でも問題ファイルの存在と修正理由を利用者が特定できない。
- 期待状態:
  - format header以外にlevel-1 titleが0件なら、ファイルパスと「title block is required」等の理由を持つファイルレベルdiagnosticを返す。
  - サーバー起動方針は既存仕様どおり維持し、対象Markdown自体は存在するため起動した上で画面上部に診断する。
  - headerのみ、本文はあるがH1なし、空のH1相当を含む境界ケースのparser testと、単独ファイルで診断を表示しつつ起動するWeb/Startup testを追加する。

## V0.1仕様の実装確認

### テスト仕様入力・解析

- 単一 `.md` またはディレクトリ、絶対・相対パスを起動時working directory基準で解決する。
- ディレクトリは再帰探索し、reparse pointのファイル・ディレクトリをたどらない。
- 不存在、非Markdown単一ファイル、Markdown 0件ではサーバーを起動せずCLI errorを返す。
- UTF-8読込、BOM処理、読込失敗、ファイル別診断、正常titleの部分表示を実装している。
- format version、必須section順序・非空、表数・位置、固定列・列順、1行以上、必須cell、Test ID形式、ファイル単位重複を検証する。
- Stepsの空欄と `-` を許可し、分類値の `-` は結合しない。
- pattern分類のrowspanは連続値だけを、Major/Middleの構造的親境界内で結合する。
- 上記Findingを除き、形式仕様の主要な正常・異常系は自動テストされている。

### Markdown・HTML安全性

- Markdigのadvanced extensionsでGFM相当をHTML化し、許可tag・属性・schemeへサニタイズする。
- attributeをtag別に再制限し、画像のmailtoを除外する。
- title、分類、path、履歴値、POST入力値はRazor通常出力でエスケープする。
- Markdown由来HTMLだけをサニタイズ後に `Html.Raw` へ渡す。
- script、event attribute、危険URL、SVG/iframe/form等の除去と実HTTP出力をテストしている。

### Web表示

- pattern modeはtitle、Overview、Major/Middle/Minorだけを表示し、Test IDとPreconditionsを表示しない。
- verification modeは横断テスト一覧、Test ID、Preconditions、Common steps、Steps、Expected result、前回結果、入力、履歴を表示する。
- テスト一覧はtitle/file/latest result/last executedを持ち、履歴なしをNot Testedとする。
- ディレクトリ指定時だけファイル別Total/Pass/Fail/Blocked/N/A/Not Testedを表示する。
- 一覧と集計は現在の有効かつ一意なTest IDだけを母集合とし、削除済み・不正・重複IDを除外する。
- 最新状態は実施日時ではなく `test_results.id` 最大の記録を使い、時刻を実行環境のlocal timeへ変換する。

### 結果登録・履歴

- result selectは前回値に依存せずGET時未選択で、Not Testedは選択肢・DB値に含めない。
- 全未選択を拒否し、部分選択は確認後に選択済みだけを保存し、全選択は確認なしで保存する。
- POSTケース集合を保存直前の現在仕様と照合し、欠落・追加・重複・不明ID・不正outcomeを拒否する。
- 結果登録時に対象ファイルがGit追跡済みかつindex/worktreeがHEADと同一であることを再検証し、HEAD SHAをサーバー側で取得する。
- dirty/untracked仕様は閲覧できるが保存できない。dirty仕様ではrepository/file identityを使って既存履歴を引き続き読める。
- 複数結果は1 submission・1 transactionで追記し、DB途中失敗時に全件rollbackする。過去履歴を更新・削除しない。
- 成功後はPRGとTempData messageを使い、ブラウザ更新による同一POST再送信を避ける。
- Razor Pages既定antiforgeryが有効で、form tokenを使うHTTP testがある。
- 入力不正、Git不適格、部分確認、DB system errorを別表示にし、保存失敗を成功表示しない。

### SQLite・migration

- documented schema、CHECK、foreign key、履歴・submission indexesをmigration 001で作成する。
- migration versionを一度だけ順番に適用し、既存履歴を保持する。
- foreign keysをconnectionごとに有効化し、親なしresultと親submission削除を拒否する。
- 実行時DB・sidecar・`.testman/`・artifactsはgitignore対象である。

### CLI・Web host・配布

- `testman serve --specs <path> [--db] [--port]`、既定DB/port、終了コード1の起動前errorを実装する。
- localhost URLを明示し、環境・Kestrel設定による非loopback上書きをguardする。
- DB migration失敗はサーバー起動前にterminal errorとして終了する。
- Windows x64 self-contained publish、en/ja/zh-CNだけを許可するsatellite設定、publish後zipを実装する。
- 通常buildへのRID/self-contained影響はconditional propertyで隔離されている。

## 重要なテスト範囲

- parser、path探索、sanitizer、CLI parse/startup、localhost guard、migration、Git tracking、履歴追記/読取/rollback、presentation、HTTP閲覧・投稿・PRG・runtime DB障害、distribution設定をカバーしている。
- HTTP投稿テストは実formからantiforgery tokenとpartial confirmation hidden値を取得して再送する。
- 悪意ある履歴文字列のRazor encoding、非Git閲覧、dirty仕様履歴、DB読取障害の非Not-Tested表示を統合確認している。
- Findingの「title 0件」だけはparser/Startup/Web testがなく、現不具合を検出できない。

## 確認範囲

- `origin/main` / `HEAD`: `699bbad`
- `docs/requirements.md`
- `docs/test-format.md`
- `docs/web-ui.md`
- `docs/database.md`
- `docs/cli.md`
- Accepted ADR-001、ADR-002、ADR-003
- `src/`、`tests/`、`migrations/` の現在tip
- 既存レビュー報告は結論の根拠として再掲せず、現在コードで再検証した
- 別作業の未追跡 `.agents/skills/write-test-spec/` は確認・変更対象に含めていない

## 検証結果

- `git status`: `main...origin/main`、対象外の未追跡 `.agents/skills/write-test-spec/` のみ
- `dotnet build Testman.sln --no-restore`: 成功、0 warnings、0 errors
- `dotnet test Testman.sln --no-build --no-restore`: 成功
- Testman.Core.Tests: 116 passed
- Testman.Web.Tests: 34 passed
- 合計: 150 passed、0 failed、0 skipped

## 残存リスク

- release packaging testはcsproj構造を検証する軽量テストで、CI上の実publish・zip内容検査までは自動化していない。ただし直前のpackaging reviewでは同tip系列で実publishと自己完結成果物を確認済みである。
- Git commandの存在は結果登録に必要であり、Gitが利用できない場合は閲覧を継続し登録を拒否する。配布物は.NET runtimeを自己完結するがGit自体は同梱しないため、利用手順でGit管理・Git CLI前提を明示し続ける必要がある。
