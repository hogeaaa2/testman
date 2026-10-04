# Review: 73a34ba feat: record test target name

## 最終再レビュー: c28a20e test: verify target name not null

継続していたMinorは解消された。新規指摘なし。

- `Apply_enforces_a_non_blank_test_target_name` が `test_target_name=NULL` の直接INSERTで `SqliteException` となることを確認するようになった。
- 空白値のtrim CHECKとは独立してNULL拒否を実行検証するため、`NOT NULL` だけを誤って削除する回帰も検出できる。
- 9d680cdで追加済みの空白CHECK、既存version 1 DB非再作成、partial confirmationでの対象名保持・再抽出POSTと合わせ、初回Minor 2件の全境界が固定された。
- `dotnet test Testman.sln -c Release --no-restore` を再実行し、Core 125件、Web 37件、合計162件が成功した。

## 再レビュー: 9d680cd test: cover target name boundaries

初回Minor 2件のうち、partial confirmationの指摘は解消された。migrationの指摘も空白CHECKと既存DB非再作成については解消されたが、`NOT NULL` の回帰を直接検出するテストがない点だけが残る。新規の実装不適合はない。

- `Apply_enforces_a_non_blank_test_target_name` は空白だけの値を直接INSERTし、trim CHECKによる拒否を確認する。
- `Apply_does_not_recreate_an_existing_version_one_database` は旧列構成と既存submissionを持つversion 1 DBへ `Apply` し、行と旧列構成がそのまま維持されることを確認する。
- HTTP統合テストは確認レスポンスに `value="app.exe"` があることを確認し、`SubmissionValues` がHTMLから対象名を再抽出して確認POSTへ渡す。画面で値を失う回帰を検出できる。
- 残るテスト境界: SQLiteのCHECK制約は式の評価結果がNULLの場合に成功扱いとなるため、空白INSERTのテストだけでは `NOT NULL` を誤って削除した回帰を検出できない。`test_target_name=NULL` のINSERT失敗、または `pragma_table_info` の `notnull` 値をassertする必要がある。
- `dotnet test Testman.sln -c Release --no-restore` を再実行し、Core 125件、Web 37件、合計162件が成功した。

## 結論

実装上の要求不適合は確認されなかった。`test_target_name` は登録単位で必須・trim後保存され、同一transactionの結果群とともに追記される。Previous resultと履歴には安全に表示され、最新結果・集計の検索キーには追加されていない。

初回レビューのMinor 2件は9d680cdとc28a20eで解消された。最終的な未解決指摘はない。

## Findings

### Minor（c28a20eで解消）: 初期migrationの値制約と既存DB非再作成の境界が実行テストされていない

- 影響箇所: `tests/Testman.Core.Tests/DatabaseMigrationRunnerTests.cs:14-31`、`migrations/001_create_result_history.sql:11`
- 関連要求: `docs/database.md` の `test_target_name` はNOT NULLかつtrim後に空でないこと、V0.1公開前の既存DBを暗黙に削除・再作成しないこと。
- 説明: migrationテストは列名の存在を確認するが、`NULL`、空文字、空白だけの値がSQLiteのNOT NULL/CHECKで拒否されることを実行していない。また、version 1適用済みの旧schemaを模したDBへ `Apply` したとき、DBや既存行を削除・再作成せずそのまま残す境界も固定していない。
- 影響: 後続変更でSQL制約が弱まる、またはmigration runnerが開発用旧DBを暗黙再作成する回帰が入っても、現在のテストは成功し得る。現行実装は要求どおりであるためMinorとした。
- 推奨: 初期migration適用後に不正な `test_target_name` のINSERTが失敗するテストと、旧version 1 schema＋既存行を用意して `Apply` 後もファイル/schema/行が維持されるテストを追加する。
- 9d680cdでの解消状況: 空白値の拒否と旧version 1 DBの保持は直接テストされ、該当部分は解消した。NULL値の拒否は未検証であり、`NOT NULL` を削除しても空白CHECKテストは成功するため、この境界だけ指摘を継続する。
- c28a20eでの解消状況: NULL値の直接INSERTが失敗することもassertされ、残っていた `NOT NULL` 境界を含めて解消した。

### Minor（9d680cdで解消）: partial confirmationのHTMLでテスト対象名の保持を直接確認していない

- 影響箇所: `tests/Testman.Web.Tests/CliWebHostTests.cs:276-330`、`tests/Testman.Web.Tests/CliWebHostTests.cs:432-458`
- 関連要求: `docs/web-ui.md` のテスト対象名は登録操作に共通する必須入力であり、部分登録確認時も入力内容を保持して再送できること。
- 説明: HTTP統合テストは最初のPOSTへ `TestTargetName=app.exe` を含めるが、確認レスポンスのinputにその値が再描画されたことをassertせず、再POST用dictionaryでも固定値 `app.exe` を再設定している。そのため、確認画面で値を失う回帰を検出できない。
- 影響: 現行Razor Pagesのmodel bindingでは値は保持されるが、将来のPageModelやフォーム変更で確認画面だけ入力が空になってもテストが通る。限定された回帰防止不足のためMinorとした。
- 推奨: 確認レスポンスから `TestTargetName` inputのvalueを抽出して `app.exe` をassertし、その抽出値を確認POSTへそのまま使用する。
- 9d680cdでの解消状況: 確認HTMLの保持値をassertし、HTMLから再抽出した値を確認POSTへ使用するようになったため解消した。

## 実装確認

- 初期migrationの `test_target_name` は `TEXT NOT NULL CHECK (length(trim(test_target_name)) > 0)` で、正式schemaの要求に一致する。
- Web coordinatorと永続化層の両方で空白だけの対象名を拒否する。永続化層は前後空白を除去してから保存し、DB制約も最終防衛となる。
- `ResultSubmission` に対象名を1つ保持し、submission行と選択された複数result行を同一transactionで保存する。事前validationと既存rollbackテストにより、部分保存は行わない。
- partial confirmation時はbound `TestTargetName` をPageModel上に保持したまま同じPageを描画する。成功時は既存どおりPRGを行う。
- `ReadLatest` / `ReadHistory` はsubmissionをJOINして対象名を返すが、WHERE条件はrepository root、source file、Test IDだけであり、対象名で履歴や集計を絞り込まない。最新結果は従来どおりresult内部IDの降順で決まる。
- Previous resultと履歴に対象名を通常のRazor式で出力するためHTMLエンコードされる。悪意ある対象名のHTTP統合テストも、escaped textの表示と生script非表示を確認している。
- 履歴表示から `SpecificationRevision` の `<code>` 出力が削除され、対象名が表示される。一方、DBへの仕様SHA保存と仕様カード/Test listのSHA表示は維持される。
- migration runnerは適用済みversionをskipするだけで、DBファイルや既存tableを削除・再作成しない。仕様どおり旧開発DBの後方互換移行は提供しない。
- schema変更は公開前の初期migration更新として行われ、別migrationを追加しない方針は最新 `docs/database.md` と一致する。

## テスト確認

- coordinator: 対象名の必須検証、trim後保存、一括submission、partial confirmation、Git dirty拒否を確認する。
- store: 対象名の保存・読取、異なる対象名を持つ同一Test IDの追記・最新結果・全履歴、invalid batchと後続INSERT失敗時のrollbackを確認する。
- presentation/HTTP: Previous resultと履歴への対象名伝播、悪意ある対象名のRazor encoding、履歴でSHAを表示しないことを確認する。
- 集計: 既存の履歴readerが対象名を条件にせず、異なる対象名でも内部ID最大の結果を返すため、対象名で集計を分割しない実装になっている。

## 仕様照合

- `docs/requirements.md`: 対象名の入力・保存、登録単位で共通、必須trim検証、仕様SHAとは別情報、追記型履歴に適合する。
- `docs/database.md`: NOT NULL列、trim後非空、履歴保持、対象名で前回結果・現在状況を絞らない方針、公開前初期migration更新方針に適合する。
- `docs/web-ui.md`: 入力欄、Previous result、展開履歴での対象名表示、履歴内SHA非表示に適合する。
- Accepted ADR-001/ADR-002: 仕様ファイルとSQLite結果履歴を分離し、登録結果を追記する方針を維持する。

## 検証結果

- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 123件成功、失敗0件
  - Testman.Web.Tests: 37件成功、失敗0件
  - 合計: 160件成功、失敗0件
