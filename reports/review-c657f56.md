# Review: c657f56 perf: reuse Git identities while loading specifications

## 結論

指摘なし。Git identityのキャッシュは1回の `SpecificationPageContentSource.Load()` 内だけに限定され、履歴参照とファイル最終変更SHAで同じimmutable identityを再利用する。次回Loadでは再解決されるため、仕様更新やGit障害からの復旧の意味を変えていない。

## Findings

なし。

## 実装確認

- identity辞書は `Load()` のローカル変数であり、singletonの `SpecificationPageContentSource` に永続キャッシュされない。ページロードごとに新しい辞書を作る。
- 同一Load内では、各仕様ファイルについて最初の履歴参照時に `GitSpecificationIdentity.Resolve` を1回実行し、以降のTest IDの履歴参照と `ResolveLastCommittedRevision` で同じidentityを使う。
- identityは正規化済みrepository rootとrepository相対source fileだけを持つimmutable objectである。再利用しても、履歴の検索キーであるrepository root / source file / Test IDは変わらない。
- 表示SHAは再利用したidentityから従来と同じ `git log -1 --format=%H -- <source_file>` を実行する。identityのキャッシュはcommit SHAをキャッシュしないため、各Load時点の対象ファイル最終変更commitを表示する。
- 結果登録用 `GitSpecificationReference.Resolve` は変更されていない。tracked/clean検証と保存する仕様SHAの意味には影響しない。
- identity解決の `InvalidOperationException` / `ArgumentException` は従来どおり履歴なし・revision unavailableへ変換する。同一Loadでは失敗をnullとして共有するが、次回Loadでは再試行する。
- 履歴DBの `SqliteException` などは新しいcatch対象に含まれず、従来どおりシステム障害として上位へ伝播する。DB障害をNot Testedへ誤変換しない。
- SHA解決だけが失敗した場合も、その前に取得済みの履歴は維持される。逆にidentity自体が解決できない場合は、Git repository/source fileによる履歴同定ができないため従来どおり履歴を空として閲覧を継続する。
- path comparerはWindowsで `OrdinalIgnoreCase`、その他OSで `Ordinal` を使用し、既存の投稿時path比較と同じOS別方針になっている。culture依存比較は行わない。

## テスト確認

- 既存identityを受ける `ResolveLastCommittedRevision` が対象ファイルの最終変更commitを返し、repository HEADを誤って返さないことをunit testで確認する。
- 公開overloadへnullを渡した場合の `ArgumentNullException` を確認する。
- 結果保存後に同じsource instanceでLoadし、仕様ファイルを新しいcommitへ更新して再Loadしたとき、表示SHAだけが新commitへ更新され、保存済み履歴とその保存SHAが維持されることを確認する。
- 一時的に `.git` を利用不能にして同じsource instanceでLoadし、revision unavailable・履歴なしで閲覧を継続した後、Gitを復元して再LoadするとSHAと履歴が復旧することを確認する。キャッシュがLoadを越えないことを直接固定している。
- 既存のdirty/untracked/non-Git、DB読取障害、履歴検索、最新結果、HTML表示のテスト群にも回帰はない。

## 仕様照合

- `docs/requirements.md`: 未コミット・Git管理外でも閲覧継続、Test ID単位の履歴表示、再読込で履歴を失わない要求を維持する。
- `docs/web-ui.md`: ファイル最終変更SHA、revision unavailable時の閲覧継続、履歴表示の意味を維持する。
- `docs/database.md`: repository root / source file / Test IDによる履歴同定と、ファイル最終変更commit SHAの意味を維持する。
- Accepted ADR-001/ADR-002: 現在仕様をファイルから直接読み、SQLiteの追記履歴を別管理する構成を変更しない。

## 検証結果

- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 127件成功、失敗0件
  - Testman.Web.Tests: 39件成功、失敗0件
  - 合計: 166件成功、失敗0件

