# Review: 319a8d4 `feat: coordinate result submissions`

## 最終結論

指摘なし。修正コミット `c60bc8f` により、初回レビューのMinor 1件は解消した。対象コミットと修正コミットは、今回確認した範囲で要求、Web UI方針、データベース仕様、テスト形式、関連Accepted ADRに適合している。

## 初回指摘の再確認

### 解消: WindowsでTest IDの大小文字改ざんが現在仕様照合を通過し、入力エラーではなくシステム例外になる

- 対象:
  - `src/Testman.Web/Presentation/ResultSubmissionCoordinator.cs:29-31`
  - `src/Testman.Web/Presentation/ResultSubmissionCoordinator.cs:113-142`
  - `tests/Testman.Web.Tests/ResultSubmissionCoordinatorTests.cs`
- 根拠:
  - `docs/test-format.md` / Accepted ADR-003ではTest IDを厳密な `TC-#` 形式とする
  - `docs/web-ui.md`「利用者の入力エラーとシステム障害を区別する」
  - `docs/web-ui.md` は重複IDまたは安全に識別できない行の結果登録を無効化する
  - `.agents/skills/review-implementation/SKILL.md` はresult submissionの入力検証とfalse success防止を要求する
- 説明: `MatchesCurrentCases` は、Windowsで `StringComparer.OrdinalIgnoreCase` を使用する `PathComparer` を、`sourcePath + NUL + testCaseId` という複合文字列全体のHashSet比較へ使用している。そのためパスだけでなくTest IDも大小文字を無視して比較される。現在仕様の `TC-1` に対してPOSTの `tc-1` が一致扱いとなるが、後段の `ResultHistoryStore.Validate` はTest IDを仕様どおり大小文字を区別して検証し、`ArgumentException` を送出する。Coordinatorは `InvalidOperationException` しか捕捉しないため、この例外は入力エラー応答にならず上位へ漏れる。
- 再現（Windows）:
  1. 現在仕様に `TC-1` と `TC-2` を置く。
  2. `SourcePath` は正しいまま、POST相当の `Cases` でIDを `tc-1` と `TC-2` にして結果を選択する。
  3. `MatchesCurrentCases` はtrueとなる。
  4. `Append` 内の `TestId.TryParse("tc-1")` が失敗し、未処理の `ArgumentException` になる。結果は保存されないが、改ざんされた入力が `Invalid` ではなくHTTP 500相当へ分類される。
- 影響: 悪意ある、または壊れたPOSTにより不要なシステムエラーを発生させられ、入力エラーとシステム障害の区別を満たさない。データの部分保存は事前検証とトランザクションにより発生しない。
- 修正確認:
  - pathとTest IDを保持する `CurrentCase` を、専用 `CurrentCaseComparer` で比較するよう変更された。
  - pathは従来どおりOSに応じた `PathComparer`、Test IDは常に `StringComparer.Ordinal` で比較される。
  - 投稿ケースの重複検出と現在ケース集合との一致判定にも同じ比較規則が使われる。
  - `tc-1` 改ざんが `ResultSubmissionStatus.Invalid` となり、DBへ何も保存されないことが既存の改ざんテストへ追加された。
- 判定: 解消。

## 新規回帰の確認

新たなCritical、Major、Minorの指摘はない。Windowsで必要なpathの大小文字無視は維持され、Test IDだけが仕様どおり厳密比較になった。ケース数、重複、欠落、追加の集合照合にも変更による緩みはない。

## 指定観点の確認結果

### POST改ざん耐性・現在仕様との照合

- POSTされたケース数、source path、Test IDの集合を、登録時に再読込した現在仕様の登録可能ケース集合と照合する。
- 不明ID、欠落、追加、重複、壊れたパスは拒否される。
- outcomeは `pass`、`fail`、`blocked`、`not_applicable` だけを受け付ける。
- コメント、実施者、outcome以外の投稿値からrepository rootやGit revisionを信頼せず、サーバー側で再解決する。
- 修正後はTest IDも仕様どおり厳密比較される。

### 部分選択確認

- 全ケース未選択はInvalidとなり保存しない。
- 登録可能な現在ケースの一部だけが選択され、`ConfirmPartial` がfalseなら確認要求となり保存しない。
- 確認済みの場合は選択ケースだけを保存する。
- 全ケース選択済みなら確認を要求しない。

### Git clean検証

- 選択された各仕様ファイルについて、投稿されたrevisionを使わず `GitSpecificationReference.Resolve` を保存直前に実行する。
- 未追跡、作業ツリー変更、インデックス変更を拒否し、サーバー側でHEAD SHAを取得する。
- Git不適格は `Invalid` として「committed, unchanged」要件を案内し、成功扱いにしない。

### 一括トランザクション

- 選択済みcaseを1つの `ResultSubmission` にまとめ、既存の `ResultHistoryStore.Append` へ1回だけ渡す。
- Store側はsubmissionと全resultsを1トランザクションで挿入し、途中DB失敗時の全件ロールバックが既存テストで確認されている。

### エラー分類

- 実施者なし、ケース集合不一致、outcome不正、全件未選択、Git不適格はInvalidまたは確認要求として扱う。
- DB例外はGit入力エラーとして握りつぶさず上位へ伝播するため、システム障害として表示できる。
- 修正後はWindows上のID大小文字改ざんもInvalidへ分類される。

## 確認範囲

- `AGENTS.md`
- `.agents/reviewer.md`
- `.agents/skills/review-implementation/SKILL.md`
- `docs/requirements.md`
- `docs/web-ui.md`
- `docs/database.md`
- `docs/test-format.md`
- Accepted ADR-001、ADR-002、ADR-003
- 実装コミット `319a8d4`、修正コミット `c60bc8f` と関連するparser、Git参照、履歴Store実装およびテスト

## 検証結果

- `dotnet test Testman.sln --no-restore`: 成功
- Testman.Core.Tests: 116 passed
- Testman.Web.Tests: 25 passed
- 合計: 141 passed、0 failed、0 skipped

## 残存リスク

このコミットはsubmission coordinatorまでで、実際のRazor POST handlerとフォームは対象外である。Web接続時にはantiforgery、確認画面での再改ざん防止、POST/Redirect/Get、成功・入力エラー・DB障害のHTTP表示を別途レビューする必要がある。
