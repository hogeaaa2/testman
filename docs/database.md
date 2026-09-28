# データベース方針

## 状態

概念設計。テスト仕様形式と履歴保持方針の確定後に、列、制約、migrationを確定する。

## 責務

SQLiteはテスト実施結果と、その結果を解釈するために必要な参照情報を保持する。テスト仕様本文のSource of Truthは仕様ファイルである。

実行時DBファイルはGit管理しない。schema定義とmigrationはGit管理する。

## 概念モデル

### test_cases

- test_case_id: 安定したTest ID
- title: 読み込み時点のタイトル
- source_file: 仕様ファイルの識別子
- content_hash: 仕様内容の変更検出用ハッシュ
- imported_at: 読み込み日時

### test_runs

- id: 実施記録の内部ID
- test_case_id: 対象Test ID
- result: `pass` または `fail`
- executed_at: 実施日時
- comment: 任意コメント
- specification_hash: 実施時点の仕様を識別する値の候補

上記は確定schemaではない。

## 保持すべき不変条件

- 新しい実施結果の登録で過去の結果を上書きしない。
- 仕様の再読込で既存の実施履歴を削除しない。
- DB上でTest IDの参照整合性を保つ。
- 日時の保存形式とタイムゾーン方針を一貫させる。
- migrationを適用した既存DBの履歴を保持する。

## 要決定事項

- 仕様から削除されたテストケースの扱い
- Test IDを変更した場合の扱い
- 実施時点の仕様本文をスナップショットとして保存するか
- 同じTest IDが複数ファイルに現れた場合のエラー単位
- import全体をtransactionにするか
- DBファイルの既定配置場所と上書き方法
- 実施者情報を保存するか
