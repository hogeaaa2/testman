# Review: dbfd5c3 `feat: add result history migration`

## 結論

指摘なし。初回レビューのMajor 1件は、修正コミット`6f840f8`で解消された。

## Findings

### 解消済み（Major）: 通常のDB接続で外部キーが強制されない

- 対象: `src/Testman.Core/Persistence/DatabaseMigrationRunner.cs:27`、`migrations/001_create_result_history.sql:21`
- 根拠: `docs/database.md`は`test_results.submission_id`について、親`result_submissions`が存在することを要求し、削除を連鎖させないと定めている。
- 初回内容: migration接続でのみ外部キーを有効化していたため、後続の通常接続で孤児となる`test_results`を保存できる状態だった。
- 修正確認: `6f840f8`で全DBアクセス用の`SqliteConnectionFactory`が追加され、接続文字列の`ForeignKeys = true`によって接続ごとの有効化が保証された。migrationも同じfactoryを使用する。
- 回帰確認: 追加テストにより、存在しないsubmissionへのINSERTが失敗すること、結果を持つsubmissionのDELETEが失敗して子結果が保持されることを確認した。

## 確認範囲

- `docs/requirements.md`、`docs/database.md`、Acceptedの`ADR-002`
- 3テーブルの列、型、NOT NULL、CHECK、外部キー宣言
- 指定された2索引の列順と降順指定
- migrationのトランザクション、適用履歴、再適用時の履歴保持
- migration SQLの埋め込み方法
- SQLite実行時ファイルと生成物のGit混入
- NuGetの直接・推移依存脆弱性

物理schemaと接続時の外部キー設定は確定仕様と一致している。migration本体と適用記録は同じトランザクション内にあり、再適用テストでは既存履歴が保持されている。実行時DBの追跡も検出されなかった。

## 検証結果

- migration関連テスト: 3件成功
- solution build: 成功、警告0、エラー0
- 全テスト: Core 102件、Web 16件成功
- `dotnet list Testman.sln package --vulnerable --include-transitive`: 既知の脆弱なパッケージなし
- `git diff dbfd5c3^ 6f840f8 --check`: 問題なし

## 残存リスク

- 現在のテストはschema作成、migration記録、冪等性、既存履歴保持、外部キー、非連鎖削除を確認している。CHECK制約とmigration失敗時のロールバックは直接検証していないが、実装とschemaの目視確認上、このコミットを止める問題は認められない。
- 結果の一括追記と保存失敗時の全件ロールバックは後続の保存機能の対象であり、このコミット単体の欠陥とはしていない。
