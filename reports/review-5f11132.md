# Review: 5f11132 fix: save specification file revision

## 結論

指摘なし。結果登録時に保存する `specification_revision` は、対象Markdownファイルを最後に変更したcommit SHAへ修正され、最新仕様に適合している。登録可否のclean/tracked検証は維持され、表示用リビジョンとSHAの意味も一致している。

## Findings

なし。

## 実装確認

- `GitSpecificationReference.Resolve` は従来どおり、対象ファイルがGit追跡済みであること、作業ツリーがHEADと一致すること、インデックスがHEADと一致することを結果登録前に検証する。
- 登録用SHAの取得だけが `rev-parse HEAD` から、`git log -1 --format=%H -- <source_file>` 相当の `ResolveLastCommittedRevision` へ変更された。このため、対象ファイル以外だけを変更した後続commitがあっても、対象Markdownを最後に変更したcommitを保存する。
- `Resolve` と表示用 `ResolveLastCommittedRevision(string)` は、解決済みの `GitSpecificationIdentity` を受ける同一のprivate処理へ収束しており、`repository_root`、`source_file`、リビジョンの意味にずれがない。
- Git引数は `ProcessStartInfo.ArgumentList` で個別に渡され、パス指定の前に `--` があるため、仕様ファイルパスをshell文字列として解釈しない。
- Git実行失敗または履歴が空の場合は成功扱いせず例外となる。登録経路では既存のエラー分類に渡され、誤ったSHAで保存されない。
- DB schemaやmigrationの変更はなく、`specification_revision` の既存列へ仕様どおりの値を保存する意味修正に限定される。

## テスト境界

- 追加テストは、仕様ファイルのcommit後に別ファイルだけをcommitし、登録用 `Resolve` がHEADではなく元の仕様ファイルcommit SHAを返すことを確認している。今回の不具合を直接再現する境界になっている。
- 既存テストにより、未追跡ファイル、対象仕様の未stage変更、対象仕様のstage済み変更を登録用 `Resolve` が拒否することを確認しており、clean/tracked検証の回帰を検出できる。
- 表示用処理については、別ファイルだけの後続commit、dirtyな対象ファイル、未追跡ファイルの境界が既存テストで固定されている。
- 保存処理は `ResultSubmissionCoordinator` から登録用 `Resolve` を呼び、得た `SpecificationRevision` を既存の追記型DB保存経路へ渡す構造が維持されている。

## 仕様照合

- `docs/requirements.md`: 各結果へ対象Markdownを最後に変更したSHAを保存し、HEAD SHAを代用しない要求、および未追跡・未コミット仕様では閲覧のみ許可する要求に適合する。
- `docs/database.md`: `specification_revision` の定義、Test list表示と同じ意味、対象ファイル以外の変更だけでは登録を拒否しない要求に適合する。
- `docs/web-ui.md`: 仕様カード/Test listに表示するファイル最終変更SHAと、結果へ保存するSHAの意味が一致する。
- Accepted ADR-001/ADR-002: Git管理された仕様とSQLiteの追記型結果履歴を分離し、実施時点の仕様リビジョンを保存する方針に適合する。

## 検証結果

- `git diff --check 5f11132^ 5f11132`: 問題なし。
- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 123件成功、失敗0件
  - Testman.Web.Tests: 36件成功、失敗0件
  - 合計: 159件成功、失敗0件

