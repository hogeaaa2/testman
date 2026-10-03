# Review: d9cf1a9 fix: show file-specific revision

## 結論

指摘なし。現在HEADではなく、各Markdownファイルを最後に変更したcommit SHAをカードとTest listへ表示する意図に適合している。

## 表示用revision

- 表示用APIはファイルのGit identityを解決した後、リポジトリrootをworking directoryとして `git log -1 --format=%H -- <source_file>` 相当を実行する。
- 別ファイルだけを変更した後続commitがあっても対象ファイルの表示値は変わらないことをCoreテストで確認している。
- dirtyな対象ファイルでも最後にcommitされたrevisionを返す。実装はworking tree/indexのclean検証を行わずファイル履歴を参照するため、staged変更についても同じく最後のcommit SHAを返す。
- untrackedファイルやcommit履歴のないファイルではGit logの空出力を成功扱いせず `InvalidOperationException` とする。non-Git、Git利用不能、その他解決失敗も表示用resolverでnullへ変換され、`(Git revision unavailable)` として閲覧を継続する。
- Git引数はシェル文字列連結ではなく `ProcessStartInfo.ArgumentList` へ個別に渡し、pathspecの前に `--` を置いている。空白、先頭ハイフン、特殊文字を含むリポジトリ相対パスをオプションやシェル構文として解釈させない。

## 登録revisionとの分離

- 表示用だけが `ResolveLastCommittedRevision` を使用する。
- 結果登録は従来の `GitSpecificationReference.Resolve` を維持し、tracked、working tree clean、index cleanを検証した後のHEADを `SpecificationRevision` として保存する。
- したがってdirty/staged仕様は最後のcommit SHAを表示できる一方、結果登録は拒否される。画面表示の利便性によって履歴保存の完全性を緩めていない。
- 過去の結果履歴に保存済みのSpecificationRevisionの読取・表示にも変更はない。

## カード、Test list、安全性

- ファイルごとに解決した表示用revisionを `SpecificationFileContent` に保持し、仕様カードとTest listが同じ値を再利用する。両表示が一致し、追加のGit呼出しもない。
- SourcePathとrevisionは両方とも通常のRazor式でHTMLエンコードされる。Git出力やファイルパスからHTMLを注入する経路はない。
- 集計、履歴参照、DB障害処理、結果登録可否の各経路は変更されていない。

## 検証結果

- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 123件成功、失敗0件
  - Testman.Web.Tests: 36件成功、失敗0件
  - 合計: 159件成功、失敗0件

