# データベース方針

## 状態

V0.1の責務と履歴保持方針は確定済み。概念モデルの列、制約、migrationの詳細は実装時に確定する。

## 責務

SQLiteはテスト実施結果と、その結果を解釈するために必要な参照情報を保持する。テスト仕様本文のSource of Truthは仕様ファイルであり、現在の画面表示には仕様ファイルを直接使用する。仕様本文をSQLiteへimportして表示元にしない。

実行時DBファイルはGit管理しない。schema定義とmigrationはGit管理する。

## 概念モデル

### test_runs

- id: 実施記録の内部ID
- test_case_id: 対象Test ID
- result: `pass` または `fail`
- executed_at: 実施日時
- comment: 任意コメント
- executed_by: 検証実施者名
- source_file: 実施時点の仕様ファイル識別子
- specification_revision: 実施時点の仕様が含まれるGit commit SHA

上記は確定schemaではない。

## 保持すべき不変条件

- 新しい実施結果の登録で過去の結果を上書きしない。
- 仕様の再読込で既存の実施履歴を削除しない。
- 仕様からTest IDまたはファイルが削除されても、既存の実施履歴を削除しない。
- 現在の仕様に存在しないTest IDの履歴も、source_file、Test ID、Git commit SHAによって追跡可能な状態を保つ。
- 過去の仕様本文はGitで確認し、testmanは過去本文をDBへ保存またはWeb UIで復元表示しない。
- 日時の保存形式とタイムゾーン方針を一貫させる。
- migrationを適用した既存DBの履歴を保持する。

## 要決定事項

- DBファイルの既定配置場所と上書き方法
- 検証実施者名を必須入力にするか
- 仕様ファイルに未コミット変更がある場合、結果登録を拒否するか警告だけにするか
