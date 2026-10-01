# データベース方針

## 状態

V0.1の責務、schema、履歴保持方針を定義する。物理schemaの変更はmigrationとして実装する。

## 責務

SQLiteはテスト実施結果と、その結果を解釈するために必要な参照情報を保持する。テスト仕様本文のSource of Truthは仕様ファイルであり、現在の画面表示には仕様ファイルを直接使用する。仕様本文をSQLiteへimportして表示元にしない。

実行時DBファイルはGit管理しない。schema定義とmigrationはGit管理する。

## Schema

### result_submissions

1回の登録操作を表す。同じ画面から登録された複数ケースは1つのsubmissionに属するが、これはリリースやテスト実行セッションを表すものではない。

| Column | SQLite type | Constraint | Meaning |
|---|---|---|---|
| id | INTEGER | PRIMARY KEY AUTOINCREMENT | 登録操作の内部ID |
| executed_at_utc | TEXT | NOT NULL | UTCの実施日時 |
| executed_by | TEXT | NOT NULL | 前後の空白を除去した検証実施者名 |

`executed_at_utc`はUTCのISO 8601 round-trip形式で保存し、末尾に `Z` を持つ。`executed_by`は前後の空白を除去した後に空であってはならない。

### test_results

| Column | SQLite type | Constraint | Meaning |
|---|---|---|---|
| id | INTEGER | PRIMARY KEY AUTOINCREMENT | 実施結果の内部ID |
| submission_id | INTEGER | NOT NULL, FOREIGN KEY | `result_submissions.id` |
| repository_root | TEXT | NOT NULL | 仕様ファイルを管理するGitリポジトリルートの正規化済み絶対パス |
| source_file | TEXT | NOT NULL | Gitリポジトリルート基準の仕様ファイル相対パス |
| test_case_id | TEXT | NOT NULL | 対象Test ID |
| result | TEXT | NOT NULL, CHECK | `pass`、`fail`、`blocked`、`not_applicable` のいずれか |
| comment | TEXT | NULL | 任意コメント |
| specification_revision | TEXT | NOT NULL | 実施時点の仕様を含むGit HEAD commit SHA |

- `submission_id`の外部キーは削除を連鎖させず、親submissionがあることを要求する。
- 空のコメントはNULLとして保存してよい。
- Test IDの形式、Git追跡状態、commit SHAは保存前にアプリケーションで検証する。
- 複数ケースの登録では、submissionとすべてのresultを同じトランザクションで挿入する。失敗時は全体をロールバックする。
- 製品機能として実施結果を更新または削除する操作をV0.1では設けない。

### schema_migrations

| Column | SQLite type | Constraint | Meaning |
|---|---|---|---|
| version | INTEGER | PRIMARY KEY | 単調増加するmigration番号 |
| name | TEXT | NOT NULL | migration名 |
| applied_at_utc | TEXT | NOT NULL | UTCの適用日時 |

migrationはversion順に一度だけ適用し、適用済みDBの実施履歴を保持する。

### Indexes

- `test_results(repository_root, source_file, test_case_id, id DESC)`: 前回結果、履歴、現在状況集計用
- `test_results(submission_id)`: 一括登録内容の参照と外部キー操作補助用

## 保持すべき不変条件

- 新しい実施結果の登録で過去の結果を上書きしない。
- 画面から複数ケースを一括登録する場合は、同じトランザクションですべて追記する。いずれかの保存に失敗した場合は全件を保存しない。
- 仕様の再読込で既存の実施履歴を削除しない。
- 仕様からTest IDまたはファイルが削除されても、既存の実施履歴を削除しない。
- 現在の仕様に存在しないTest IDの履歴も、repository_root、source_file、Test ID、Git commit SHAによって追跡可能な状態を保つ。
- 過去の仕様本文はGitで確認し、testmanは過去本文をDBへ保存またはWeb UIで復元表示しない。
- 実施日時はUTCで保存し、Web画面では実行環境のローカル時刻へ変換して表示する。
- migrationを適用した既存DBの履歴を保持する。

## 現在状況の集計

仕様ファイルごとの現在状況は、現在のMarkdown仕様に存在する有効かつ一意なTest IDと、同じrepository_root、source_file、Test IDを持つ最新の実施結果を組み合わせて算出する。DBだけに仕様本文や現在のケース一覧を複製しない。

- 総数は現在の有効かつ一意なTest ID数とする。
- 各結果の件数は、Test IDごとに内部IDが最大の実施記録を最新結果として集計する。
- 履歴がない現在のTest IDは未テストとして数える。
- Not TestedはDBへ保存する結果値ではなく、現在のTest IDに対応する実施記録が存在しない状態から導出する。
- 現在の仕様から削除済みのTest IDは現在状況から除外するが、保存済み履歴は保持する。

この集計は現在状況を示すものであり、過去の特定時点に固定された実行セッションまたは確定レポートではない。

## DBファイル

- 既定パスは起動時のカレントディレクトリを基準とする `./.testman/testman.db` とする。
- `--db`で別の絶対パスまたは相対パスを指定できる。相対パスは起動時のカレントディレクトリを基準とする。
- `.testman/`とSQLiteの実行時ファイルはGit管理しない。

## Gitリビジョンと仕様ファイル識別子

- 仕様ファイルごとに、そのファイルを管理するGitリポジトリを探索する。testman本体と異なるGitリポジトリでもよい。
- `repository_root`には、当該Gitリポジトリルートの正規化済み絶対パスを保存する。
- `source_file`には、当該Gitリポジトリのルートを基準とする相対パスを `/` 区切りで保存する。
- `specification_revision`には、当該GitリポジトリのHEAD commit SHAを保存する。
- 対象の仕様ファイルがGitで追跡されていない場合、またはインデックスもしくは作業ツリーでHEADと異なる場合は、閲覧を許可するが結果登録を許可しない。
- 同じGitリポジトリ内の対象仕様ファイル以外に未コミット変更があっても、それだけを理由に結果登録を拒否しない。
- V0.1ではGitリポジトリまたは仕様ファイルの移動・改名を想定しない。移動後のパスと過去履歴を自動的に再関連付けしない。
