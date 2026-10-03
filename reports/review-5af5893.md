# Review: 5af5893 feat: show revisions in test list

## 結論

指摘なし。Test listのSpecification file列へ仕様カードと同じrevision表示を追加し、ファイル単位で解決済みの値を再利用する実装になっている。

## 確認内容

- revision resolverは `SpecificationFileContent` の生成時にファイルごとに1回だけ呼ばれる。Test list項目は生成済み `file.SpecificationRevision` をコピーするため、Test list追加によるGitコマンドの再実行はない。
- 仕様カードとTest listは同じnullable revision値を参照する。cleanな追跡済みファイルでは同じfull SHAを、dirty、staged、untracked、non-Git、Git利用不能では同じ `(Git revision unavailable)` を表示する。
- HTTP統合テストはdirtyになった同一ファイルについて、Test listと仕様カードの双方にunavailable表示が存在することを確認している。プレゼンテーションテストは複数ファイルのrevisionが各Test list行へ正しく引き継がれることを確認している。
- SourcePathとSpecificationRevisionはTest listでも通常のRazor式で出力され、HTMLエンコードされる。固定のunavailable文言にも外部入力は含まれず、新しいHTML注入経路はない。
- 集計処理は従来どおり、ファイルの登録可能な現在ケースを `CreateSummary` へ渡してPass/Fail/Blocked/N/A/Not Tested/Totalを算出する。revisionフィールドを項目へ追加しただけで、母数、最新結果、削除済み・重複・不正IDの除外条件に変更はない。
- dirty/untracked/non-Git時の閲覧・履歴参照、clean検証付き結果登録、DB障害処理も従来の別経路を維持している。

## 検証結果

- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 120件成功、失敗0件
  - Testman.Web.Tests: 36件成功、失敗0件
  - 合計: 156件成功、失敗0件

