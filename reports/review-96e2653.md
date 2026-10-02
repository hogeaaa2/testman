# Review: 96e2653 fix: diagnose missing specification titles

## 結論

指摘なし。V0.1最終ギャップレビューで挙げたMajor（形式ヘッダーは正しいがレベル1 title見出しがない仕様ファイルを、診断なしで正常扱いする問題）は解消されている。

## 仕様適合性

- `docs/test-format.md` の「titleブロックはレベル1見出しで開始する」「解析エラーはファイルごとの診断とする」に従い、レベル1見出しが1件もない場合は `A title block is required.` をファイルレベルで返す。
- レベル1見出し自体はあるが見出し文字列が空の場合は、行番号付きの `Title heading must not be empty.` を返し、解析不能なtitleを表示対象にしない。
- `docs/requirements.md` の起動方針に従い、対象Markdownファイルが存在する場合はtitle欠落を理由にサーバー起動を中止せず、画面上部の診断として表示する。
- 既存のtitle単位の部分成功処理には変更がなく、正常に解析できた別titleの表示を妨げない。

## 確認した境界ケース

- 形式ヘッダーだけのファイル: title必須診断を返すパーサーテストあり。
- レベル1見出しがなく本文・レベル2見出しだけのファイル: title必須診断を返すパーサーテストあり。
- 空のレベル1見出し (`#`): 行番号付きの空title診断を返すパーサーテストあり。
- ディレクトリ読込: 対象ファイルを保持し、起動可能なままtitle必須診断を集約するカタログテストあり。
- 単独ファイル指定のWeb起動: サーバーが起動し、ファイル名と診断、`Needs attention` を表示し、`Ready` と誤表示しない統合テストあり。

## 検証結果

- `dotnet test Testman.sln --no-restore`
  - Testman.Core.Tests: 120件成功、失敗0件
  - Testman.Web.Tests: 35件成功、失敗0件

