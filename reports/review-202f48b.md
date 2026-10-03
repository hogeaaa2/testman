# Review: 202f48b feat: show specification revision

## 結論

指摘なし。仕様カードheaderのMarkdownパス直後に、利用可能ならfull Git commit SHA、利用不能なら `(Git revision unavailable)` を表示する合意に適合している。

## Git revisionの判定と表示

- 表示用revisionはファイルごとに `GitSpecificationReference.Resolve` で解決される。同処理はGit追跡済みで、working treeとindexの双方がHEADと一致する場合だけHEADのfull SHAを返す。
- dirty、staged、untracked、non-Git、ファイル不在、Gitプロセス起動不能、Gitコマンド失敗は `InvalidOperationException` または `ArgumentException` として表示用resolver内で捕捉され、nullへ変換される。ページ全体の読込を中止しない。
- null時は固定文言 `(Git revision unavailable)` を表示する。利用可能時はパス直後へ `(<full SHA>)` を表示し、HTTP統合テストが40桁の小文字16進SHAを確認している。
- revision表示はpattern/verificationで共通の仕様カードheaderに置かれ、モードごとの差異や重複実装がない。
- ファイルパスとrevisionは通常のRazor式で出力されるためHTMLエンコードされる。固定のunavailable文言にもHTML入力は含まれず、新しい注入経路はない。

## 既存機能への影響

- 表示用revision解決は、履歴参照用の `GitSpecificationIdentity.Resolve` とは分離されている。dirty/untrackedファイルでも従来どおりidentityを解決でき、現在仕様の閲覧と既存履歴参照を継続する。
- 結果登録は従来どおりPOST時に `GitSpecificationReference.Resolve` を再実行する。表示時にSHAが利用不能でも入力欄を誤って恒久的に無効化せず、保存時点のclean状態を検証する既存仕様を維持している。
- DB履歴読取の例外処理は変更されていない。DB障害をrevision unavailableとして隠蔽せず、既存テストどおりHTTP 500として扱う。
- 履歴内の実施時点revision表示・保存形式には変更がなく、現在仕様のrevision表示と過去履歴のrevisionを混同しない。

## テスト確認

- cleanな追跡済み仕様の画面でfull SHAがパス直後に表示されるHTTP統合テストがある。
- 表示用resolverがnullを返しても仕様ファイルを保持するプレゼンテーションテストがある。
- Coreではcleanファイルのidentity/full HEAD SHA、untracked拒否、dirty拒否、identityによるuntracked読取を個別に確認している。
- non-Git仕様の閲覧継続、dirty仕様の履歴表示、Git clean検証付き結果登録、DB障害、悪意あるHTML値の既存統合テストも引き続き成功している。

## 検証結果

- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 120件成功、失敗0件
  - Testman.Web.Tests: 36件成功、失敗0件
  - 合計: 156件成功、失敗0件

