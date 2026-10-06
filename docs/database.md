# データベース方針

## 状態

V0.1の責務、schema、履歴保持方針を定義する。物理schemaの変更はmigrationとして実装する。

## 責務

**REQ-0107**: SQLiteはテスト実施結果と、その結果を解釈するために必要な参照情報を保持する。テスト仕様本文のSource of Truthは仕様ファイルであり、現在の画面表示には仕様ファイルを直接使用する。仕様本文をSQLiteへimportして表示元にしない。

REQ-0023 と REQ-0024 に従い、実行時DBファイルはGit管理せず、schema定義とmigrationはGit管理する。

## Schema

### result_submissions

**REQ-0108**: 1回の登録操作を表す。同じ画面から登録された複数ケースは1つのsubmissionに属するが、これはリリースやテスト実行セッションを表すものではない。

| Column | SQLite type | Constraint | Meaning |
|---|---|---|---|
| id | INTEGER | PRIMARY KEY AUTOINCREMENT | **REQ-0109**: 登録操作の内部ID |
| executed_at_utc | TEXT | NOT NULL | **REQ-0110**: UTCの実施日時 |
| executed_by | TEXT | NOT NULL | **REQ-0111**: 前後の空白を除去した検証実施者名 |
| test_target_name | TEXT | NOT NULL | **REQ-0112**: 1回の登録操作に共通するテスト対象ソフトの名前 |

**REQ-0113**: `executed_at_utc`はUTCのISO 8601 round-trip形式で保存し、末尾に `Z` を持つ。`executed_by`は前後の空白を除去した後に空であってはならない。

**REQ-0114**: `test_target_name`は検証実施者名と同様に必須とし、前後の空白を除去した後に空であってはならない。テスト対象名にはexeのファイル名などを記載できるが、特定のビルドを一意に識別する保証はしない。

### test_results

| Column | SQLite type | Constraint | Meaning |
|---|---|---|---|
| id | INTEGER | PRIMARY KEY AUTOINCREMENT | **REQ-0115**: 実施結果の内部ID |
| submission_id | INTEGER | NOT NULL, FOREIGN KEY | **REQ-0116**: `result_submissions.id` |
| repository_root | TEXT | NOT NULL | **REQ-0117**: 仕様ファイルを管理するGitリポジトリルートの正規化済み絶対パス |
| source_file | TEXT | NOT NULL | **REQ-0118**: Gitリポジトリルート基準の仕様ファイル相対パス |
| test_case_id | TEXT | NOT NULL | **REQ-0119**: 対象Test ID |
| result | TEXT | NOT NULL, CHECK | **REQ-0120**: `pass`、`fail`、`blocked`、`not_applicable` のいずれか |
| comment | TEXT | NULL | **REQ-0121**: 任意コメント |
| specification_revision | TEXT | NOT NULL | **REQ-0122**: 実施時点で対象Markdownファイルを最後に変更したGit commit SHA |

- **REQ-0123**: `submission_id`の外部キーは削除を連鎖させず、親submissionがあることを要求する。
- **REQ-0124**: 空のコメントはNULLとして保存してよい。
- **REQ-0125**: Test IDの形式、Git追跡状態、commit SHAは保存前にアプリケーションで検証する。
- **REQ-0126**: 複数ケースの登録では、submissionとすべてのresultを同じトランザクションで挿入する。失敗時は全体をロールバックする。
- **REQ-0127**: 製品機能として実施結果を更新または削除する操作をV0.1では設けない。

### schema_migrations

| Column | SQLite type | Constraint | Meaning |
|---|---|---|---|
| version | INTEGER | PRIMARY KEY | **REQ-0128**: 単調増加するmigration番号 |
| name | TEXT | NOT NULL | **REQ-0129**: migration名 |
| applied_at_utc | TEXT | NOT NULL | **REQ-0130**: UTCの適用日時 |

**REQ-0131**: migrationはversion順に一度だけ適用する。V0.1公開後のschema変更では、既存の実施履歴への影響と移行方法を変更ごとに確認する。

### Indexes

- **REQ-0132**: `test_results(repository_root, source_file, test_case_id, id DESC)`: 前回結果、履歴、現在状況集計用
- **REQ-0133**: `test_results(submission_id)`: 一括登録内容の参照と外部キー操作補助用

## 保持すべき不変条件

- **REQ-0134**: 新しい実施結果の登録で過去の結果を上書きしない。
- **REQ-0135**: 画面から複数ケースを一括登録する場合は、同じトランザクションですべて追記する。いずれかの保存に失敗した場合は全件を保存しない。
- **REQ-0136**: 仕様の再読込で既存の実施履歴を削除しない。
- **REQ-0137**: 仕様からTest IDまたはファイルが削除されても、既存の実施履歴を削除しない。
- **REQ-0138**: 現在の仕様に存在しないTest IDの履歴も、repository_root、source_file、Test ID、Git commit SHAによって追跡可能な状態を保つ。
- **REQ-0139**: 過去の仕様本文はGitで確認し、testmanは過去本文をDBへ保存またはWeb UIで復元表示しない。
- **REQ-0140**: 実施日時はUTCで保存し、Web画面では実行環境のローカル時刻へ変換して表示する。
- **REQ-0141**: migrationで履歴の削除、意味の変更、または復元できない変換が必要になる場合は、対象データと影響を示して実装前にユーザーの判断を得る。履歴をどう扱うかは変更内容ごとに決める。
- **REQ-0142**: V0.1の初期migrationには正式schemaを定義する。アプリケーションが既存DBを暗黙に削除・再作成する挙動は設けない。

## 現在状況の集計

**REQ-0143**: 仕様ファイルごとの現在状況は、現在のMarkdown仕様に存在する有効かつ一意なTest IDと、同じrepository_root、source_file、Test IDを持つ最新の実施結果を組み合わせて算出する。DBだけに仕様本文や現在のケース一覧を複製しない。

- **REQ-0144**: 総数は現在の有効かつ一意なTest ID数とする。
- **REQ-0145**: 各結果の件数は、Test IDごとに内部IDが最大の実施記録を最新結果として集計する。
- **REQ-0146**: テスト対象名で前回結果や現在状況の集計を絞り込まない。
- **REQ-0147**: 履歴がない現在のTest IDは未テストとして数える。
- **REQ-0148**: Not TestedはDBへ保存する結果値ではなく、現在のTest IDに対応する実施記録が存在しない状態から導出する。
- **REQ-0149**: 現在の仕様から削除済みのTest IDは現在状況から除外するが、保存済み履歴は保持する。

**REQ-0150**: この集計は現在状況を示すものであり、過去の特定時点に固定された実行セッションまたは確定レポートではない。

## DBファイル

- **REQ-0151**: 既定パスは起動時のカレントディレクトリを基準とする `./.testman/testman.db` とする。
- **REQ-0152**: `--db`で別の絶対パスまたは相対パスを指定できる。相対パスは起動時のカレントディレクトリを基準とする。
- **REQ-0153**: `.testman/`とSQLiteの実行時ファイルはGit管理しない。

## Gitリビジョンと仕様ファイル識別子

- **REQ-0154**: 仕様ファイルごとに、そのファイルを管理するGitリポジトリを探索する。testman本体と異なるGitリポジトリでもよい。
- **REQ-0155**: `repository_root`には、当該Gitリポジトリルートの正規化済み絶対パスを保存する。
- **REQ-0156**: `source_file`には、当該Gitリポジトリのルートを基準とする相対パスを `/` 区切りで保存する。
- **REQ-0157**: `specification_revision`には、結果登録時点で当該仕様Markdownファイルを最後に変更したコミットのSHAを保存する。Test listで当該ファイルのパスに併記するSHAと同じ意味とする。
- **REQ-0158**: `specification_revision`は仕様Markdownのリビジョンであり、テスト対象ソフトのリビジョンではない。テスト対象名とは別に保持する。
- **REQ-0159**: 対象の仕様ファイルがGitで追跡されていない場合、またはインデックスもしくは作業ツリーでHEADと異なる場合は、閲覧を許可するが結果登録を許可しない。
- **REQ-0160**: 同じGitリポジトリ内の対象仕様ファイル以外に未コミット変更があっても、それだけを理由に結果登録を拒否しない。
- **REQ-0161**: V0.1ではGitリポジトリまたは仕様ファイルの移動・改名を想定しない。移動後のパスと過去履歴を自動的に再関連付けしない。
