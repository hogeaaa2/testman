# Review: 72c7665 fix: keep result feedback in view

## 結論

指摘なし。結果登録後の確認・エラー・成功feedbackを含む操作カードへ戻すためのfragment指定は、仕様・既存の登録フローと整合している。

## フォームactionと非redirect応答

- POST formは `asp-route-mode="verification"` と `asp-fragment="result-submission"` を持ち、生成されたactionはverification queryと `#result-submission` を含む。
- partial confirmation、全未選択、入力不正、改ざん、Git clean不成立、DB保存エラーは `Page()` で同じPOST navigationへ応答する。ブラウザはactionのfragmentをHTTP request自体には送らないが、navigation URLのfragmentとして保持し、応答描画後に対象anchorへ移動する。
- 対象idはExecuted byとSave/Confirmボタンを含む下部操作カード自身に設定され、feedbackもそのカード先頭にある。確認・入力エラー時に登録操作とメッセージが同じviewportへ戻る。
- mode queryもactionへ明示され、再描画はVerificationモードを維持する。fragmentはサーバー側model bindingやhandler選択へ混入しない。

## 保存成功PRG

- 保存成功時は従来どおりTempDataへsuccess messageを格納し、GETのverificationモードへredirectするPRGを維持する。
- `RedirectToPage` の専用fragment引数へ `result-submission` を渡しており、Locationのqueryとfragmentが適切に分離される。
- HTTP統合テストはredirect追従後の最終URIが `#result-submission` を持つこと、成功feedbackと履歴が表示されること、保存件数が1件であることを確認している。

## CSRF、フォーム動作、安全性

- form tag helperを維持したままroute/fragment属性を追加しており、antiforgery token生成は失われていない。統合テストは実際のHTMLからtokenを含む値を抽出してPOSTし、partial confirmationと保存を完了している。
- fragmentは固定文字列で、ユーザー入力をURLやidへ反映しない。feedbackや入力値のRazorエンコード、Markdownサニタイズ経路にも変更はない。
- formの包含範囲、hiddenのConfirmPartial、ResultCases、ExecutedBy、submit buttonは変更されず、部分登録・入力保持・DB保存に回帰はない。
- patternモードでは登録form内の操作カードは表示されず、従来どおり結果登録UIを出さない。

## テスト妥当性

- 統合テストは初期フォームactionのfragmentを確認し、そのactionを使って実際にPOSTしている。
- partial confirmation後のHTMLからactionとhidden値を再抽出し、確認POSTを行うため、再描画後もfragment、CSRF、ConfirmPartial、入力値が協調することを確認できる。
- ブラウザの実スクロール位置自体はHttpClientテストの対象外だが、標準fragment navigationに必要なaction fragment、redirect fragment、対応する一意なidをそれぞれ確認しており、この変更範囲の回帰テストとして妥当である。

## 検証結果

- `dotnet test Testman.sln -c Release --no-restore`
  - Testman.Core.Tests: 123件成功、失敗0件
  - Testman.Web.Tests: 36件成功、失敗0件
  - 合計: 159件成功、失敗0件

