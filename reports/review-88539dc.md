# Review: 88539dc feat: collapse specification cards

## 結論

指摘なし。各仕様ファイルのcard-header全体をトグルとし、Markdown内容全体を展開・折り畳みできるという合意に適合している。

## 確認内容

- 各仕様ファイルの外枠はHTML標準の `<details>`、従来のcard-header全体はその直下の `<summary>` になっている。パス、形式バージョン、右端の状態表示を含むヘッダー全体がトグルとして操作できる。
- `<details ... open>` により初期状態は展開される。JavaScriptは追加されず、展開状態はブラウザ標準動作だけで切り替わる。
- ファイル概要、title、Markdown本文、テストパターン表、検証表を含む従来のカード内容全体が `details` の折り畳み対象内にある。
- POST用の `<form>` は各ファイルの `details` を包含している。折り畳みはフォームコントロールのdisabled状態やname/valueを変更しないため、閉じたカード内の結果入力も通常どおり送信対象になる。既存の部分選択・確認・保存テストも成功している。
- `summary` は標準でキーボード操作と展開状態のアクセシビリティ semantics を持つ。標準マーカーを非表示にする代わりに視覚的なExpand/Collapse表示があり、`focus-visible` のアウトラインも明示されている。補助表示は `aria-hidden` で、読み上げ上はパスと形式バージョンを含むsummary自体が操作名になる。
- 結果履歴用の入れ子の `details` はファイルカード内で従来どおり保持され、独立して操作できる。
- Markdown変換・サニタイズ処理や `Html.Raw` へ渡す値は変更されていない。既存の悪意ある仕様本文・履歴値の安全性テストも通過しており、新しいHTML注入経路はない。
- HTTP統合テストは、ファイルカードが初期 `open` の `details` であること、card-header全体が `summary` であること、トグル表示が存在することを確認している。

## 検証結果

- `dotnet test Testman.sln --no-restore`
  - Testman.Core.Tests: 120件成功、失敗0件
  - Testman.Web.Tests: 35件成功、失敗0件
  - 合計: 155件成功、失敗0件

