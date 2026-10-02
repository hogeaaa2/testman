# Review: 0d3c2b7 `feat: read result history`

## 最終結論

指摘なし。修正コミット `d03e67d` により、初回レビューのMajor 1件は解消した。対象コミットと修正コミットは、今回確認した範囲で要求、Web UI方針、データベース仕様、関連Accepted ADRに適合している。

## 初回指摘の再確認

### 解消: 未コミット変更のある仕様では前回結果と履歴を読み取れない

- 対象:
  - `src/Testman.Core/Persistence/ResultHistoryStore.cs:66-91`
  - `src/Testman.Core/Persistence/GitSpecificationReference.cs:20-66`
- 根拠:
  - `docs/requirements.md`「仕様ファイルがGit管理外または未コミット状態の場合は閲覧を許可するが、結果登録を許可しない」
  - `docs/requirements.md`「利用者は過去のテスト結果を確認できる」
  - `docs/web-ui.md` のテスト一覧、テスト詳細・実施、実施履歴では最新結果、前回結果、履歴を表示する
  - `docs/database.md` は現在の仕様と同じrepository root、source file、Test IDで最新結果と履歴を関連付ける
- 説明: `ReadLatest` と `ReadHistory` は `GitSpecificationReference` を必須とする。しかし、この型を生成できる唯一の公開経路 `GitSpecificationReference.Resolve` は、対象ファイルの作業ツリーまたはインデックスがHEADと異なると例外を送出する。このclean検証は結果登録には必要だが、閲覧は未コミット状態でも許可されている。読取用の識別子と登録可否が同じ型へ結合されているため、未コミット変更のある既存仕様についてrepository rootとsource fileが変わっていなくても、履歴読取APIへ到達できない。
- 再現:
  1. Gitへコミット済みの仕様について結果を保存する。
  2. 同じ仕様ファイルを編集し、コミットしない。
  3. 現在の仕様を表示して前回結果または履歴を取得するため `GitSpecificationReference.Resolve(specificationPath)` を呼ぶ。
  4. `The specification file has uncommitted working-tree changes.` で失敗し、`ReadLatest` / `ReadHistory` に渡す参照を生成できない。
- 影響: 仕様の編集中に本文の閲覧自体は継続できても、一覧の最新結果、詳細の前回結果、実施履歴を表示できない。仕様上禁止されているのは結果登録であり、履歴閲覧まで拒否するのは主要なWeb閲覧要件への不適合となる。
- 修正確認:
  - repository rootとrepository-relative source pathだけを表す読取用 `GitSpecificationIdentity` が追加された。
  - `ReadLatest` と `ReadHistory` はidentityを受け取り、clean状態や追跡状態に依存せず履歴を検索する。
  - 登録用 `GitSpecificationReference` はidentityに加えて、追跡済み、作業ツリーとインデックスがHEADに一致、HEAD SHA取得成功という従来の検証を維持している。
  - dirtyな仕様で保存済み履歴を読めるテストと、untrackedファイルでも読取用identityを解決できるテストが追加された。
- 判定: 解消。

## 新規回帰の確認

新たなCritical、Major、Minorの指摘はない。参照型の分離後も、`Append` は登録用 `GitSpecificationReference` を要求するため、dirty/untracked仕様からの結果登録は引き続き拒否される。読取クエリのrepository root、source file、Test IDによる絞込み、内部ID降順、最新1件の選択にも変更はない。

## 確認できた仕様適合

- `ReadLatest` は実施日時ではなく `test_results.id` の最大値を最新結果として使う。
- `ReadHistory` は同じrepository root、source file、Test IDの全履歴を内部ID降順で返す。
- 履歴がないTest IDでは `ReadLatest` がnullを返し、Not Testedを導出できる。
- DBのUTC日時をUTCの `DateTimeOffset` として復元する。
- Pass、Fail、Blocked、N/Aを列挙値へ復元する。
- コメント、検証実施者、submission ID、Git commit SHAを読取モデルへ含める。
- 読取処理はDBを更新せず、既存履歴を保持する。
- schema変更はなく、新規migrationは不要。
- 実行時DB、秘密情報、生成物は対象差分へ追加されていない。

## 確認範囲

- `AGENTS.md`
- `.agents/reviewer.md`
- `.agents/skills/review-implementation/SKILL.md`
- `docs/requirements.md`
- `docs/web-ui.md`
- `docs/database.md`
- Accepted ADR-001、ADR-002、ADR-003
- 実装コミット `0d3c2b7`、修正コミット `d03e67d` と関連する保存・Git参照・migration実装

## 検証結果

- `dotnet test Testman.sln --no-restore`: 成功
- Testman.Core.Tests: 116 passed
- Testman.Web.Tests: 17 passed
- 合計: 133 passed、0 failed、0 skipped

## 残存リスク

この実装単位は永続化層の読取APIまでで、Web画面への表示接続は対象外である。Web統合レビューでは、未コミット仕様で履歴を表示しつつ登録だけを無効化できること、UTC日時を実行環境のローカル時刻へ変換すること、仕様から削除済みの履歴を削除しないことを確認する必要がある。
