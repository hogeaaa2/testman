# Re-review: 9b56009..f51b4ae align numbered test steps

## 結論

指摘なし。初回レビューのMinorは、実際の入力と生成HTMLを固定するHTTP統合テストによって解消を確認した。

## Minor対応の確認

- Stepsセルへ実例どおり `1. Case step<br>2. Follow-up step` を入力している。
- 実際の画面が `<td class="markdown-content test-steps"><ol>` を生成し、`.test-steps > ol` セレクタの対象になることをHTTPレスポンスで確認している。
- リスト内容が `<li>Case step<br>2. Follow-up step</li>` になることも固定しており、`<br>` 後の番号を含む実出力とCSSの組合せを確認できる。
- `padding-inline-start: 0` と `list-style-position: inside` はこの直下のordered listへ適用され、Steps列内の番号位置を揃える目的に妥当である。

## その他の確認

- `test-steps` は検証実施画面のStepsセルだけに付与され、Common steps、Expected result、Overviewなど他のMarkdown領域には付与されない。
- セレクタは直下の `ol` に限定されるため、Steps内でも入れ子のリストへ広範に波及しない。
- CSSと固定クラスの追加だけで、Markdown HTMLのサニタイズ経路や `Html.Raw` に渡す内容は変更していない。既存の悪意あるHTMLのエスケープ確認も引き続き通過している。
- 同じ実例入力を使用する部分選択・結果投稿のHTTP統合テストも通過し、フォーム送信や履歴保存への回帰はない。

## 検証結果

- `dotnet test Testman.sln --no-restore`
  - Testman.Core.Tests: 120件成功、失敗0件
  - Testman.Web.Tests: 35件成功、失敗0件
  - 合計: 155件成功、失敗0件
