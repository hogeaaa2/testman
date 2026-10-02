# Review: b777471 `feat: append result submissions`

## 最終結論

指摘なし。修正コミット `b62eae8` により、初回レビューのMajor 1件とMinor 1件はいずれも解消した。対象コミットと修正コミットは、今回確認した範囲で要求、データベース仕様、関連Accepted ADRに適合している。

## 初回指摘の再確認

### 解消: Gitで検証されていない仕様識別情報を実施履歴として保存できる

- 対象: `src/Testman.Core/Persistence/ResultHistoryStore.cs:65-101`
- 根拠:
  - `docs/requirements.md`「仕様ファイルがGit管理外または未コミット状態の場合は閲覧を許可するが、結果登録を許可しない」
  - `docs/database.md`「Test IDの形式、Git追跡状態、commit SHAは保存前にアプリケーションで検証する」
  - `docs/database.md`「repository_root」には仕様を管理するGitリポジトリルート、「source_file」にはそのルート基準の相対パス、「specification_revision」には当該リポジトリのHEAD commit SHAを保存する
- 説明: `Validate` は `RepositoryRoot` を `Path.GetFullPath` へ通し、`SpecificationRevision` が空白でないことだけを確認している。指定先がGitリポジトリか、`SourceFile` がその配下の追跡済みファイルか、ファイルがHEADと一致するか、revisionがそのリポジトリのHEAD SHAかを検証しない。また `SourceFile` は相対パスでさえあれば `../outside.md` を受け入れる。このコミットおよび既存コードには、保存前にこれらを検証する別の経路も存在しない。
- 再現: `RepositoryRoot = "D:/存在しないパス"`、`SourceFile = "../outside.md"`、`SpecificationRevision = "not-a-commit"` とした `TestResultInput` を `Append` へ渡すと、migration済みDBへ正常に挿入される。
- 影響: 実施時点の仕様をGitで追跡できない履歴が正常登録として残る。仕様の未コミット変更がある場合にも登録を拒否できず、結果がどの仕様に対するものかという履歴の不変条件を満たせない。
- 修正確認: `GitSpecificationReference.Resolve` が実在ファイルからGitリポジトリルート、リポジトリ相対パス、HEAD SHAを導出し、リポジトリ外のパス、未追跡ファイル、作業ツリーまたはインデックスがHEADと異なるファイルを拒否する。`TestResultInput` はこの検証済み型を要求するため、呼出し側から任意の識別文字列を保存できない。正常系と各拒否条件も自動テストで確認されている。
- 判定: 解消。

### 解消: DB書込み途中の失敗時に全件ロールバックされることをテストしていない

- 対象: `tests/Testman.Core.Tests/ResultHistoryStoreTests.cs`
- 根拠:
  - `docs/database.md`「複数ケースの登録では、submissionとすべてのresultを同じトランザクションで挿入する。失敗時は全体をロールバックする」
  - `.agents/skills/review-implementation/SKILL.md` はdatabase failuresのテスト確認を要求する
- 説明: 不正なTest IDのテストはDB接続前の入力検証失敗を確認しているだけで、submissionや先行resultを挿入した後にSQLiteエラーが起きるケースを確認していない。実装はトランザクションを使用しており妥当だが、履歴保持上重要な失敗時不変条件が回帰テストで固定されていない。
- 影響: 将来トランザクション境界や挿入処理が変更された際、一部だけ保存される回帰を検出できない。
- 修正確認: 2件目を拒否するSQLite triggerで挿入途中のエラーを発生させ、例外後に `result_submissions` と `test_results` がともに0件であることを確認するテストが追加された。
- 判定: 解消。

## 新規回帰の確認

新たなCritical、Major、Minorの指摘はない。追記、履歴保持、一括トランザクション、実施者名の正規化、UTC日時、結果値、空コメントのNULL化、Test ID検証は引き続き仕様どおりである。Gitコマンド引数には `ProcessStartInfo.ArgumentList` が使われ、パスをshell文字列へ連結していない。schema変更はなく、新規migrationは不要である。

## 確認範囲

- `AGENTS.md`
- `.agents/reviewer.md`
- `.agents/skills/review-implementation/SKILL.md`
- `docs/requirements.md`
- `docs/database.md`
- `docs/open-questions.md`（関連する未決事項なし）
- Accepted ADR-001、ADR-002、ADR-003
- 実装コミット `b777471`、修正コミット `b62eae8` の差分および関連migration・既存実装

## 検証結果

- `dotnet test Testman.sln --no-restore`: 成功
- Testman.Core.Tests: 111 passed
- Testman.Web.Tests: 17 passed
- 合計: 128 passed、0 failed、0 skipped
- 実行時DB、秘密情報、生成物のコミット追加は対象差分にない

## 残存リスク

この実装単位は保存層とGit仕様参照の生成までで、Web登録経路には未接続である。Web統合時には保存直前の仕様ファイルから `GitSpecificationReference` を解決し、画面表示後に仕様が変更された場合も登録を拒否できることを確認する必要がある。未選択ケースの確認、CSRF、二重送信、登録後表示もWeb統合コミットで別途レビュー対象とする。
