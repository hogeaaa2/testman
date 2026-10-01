# Review: e851e6d

## Result

指摘なし。

## Reviewed scope

- 対象コミット: `e851e6d feat: add verification specification view`
- テストパターン表示と検証実施表示の切り替え
- 検証実施表示のTest ID、Preconditions、Common steps、行別Steps、Expected result
- テストパターン表示からのTest IDとPreconditionsの除外
- Markdown変換後のHTMLサニタイズとRazor出力時のエスケープ
- 関連する確定仕様:
  - `docs/requirements.md`
  - `docs/web-ui.md`
  - `docs/test-format.md`

## Confirmed behavior

- `mode=verification`の場合のみ検証実施表示となり、同じ仕様データからTest ID、Preconditions、Common steps、行別Steps、Expected resultを表示する。
- 通常のテストパターン表示はtitle、Overview、Major item、Middle item、Minor itemに限定され、実HTTP応答にTest IDが含まれないことを確認できる。PreconditionsとCommon stepsもこの分岐で出力されない。
- Overview、Preconditions、Common steps、行別Steps、Expected resultはすべて`SafeMarkdownRenderer.Render`の結果だけが`Html.Raw`へ渡される。スクリプト除去とMarkdownの強調・生HTML表示は自動テストで確認されている。
- titleとTest IDはRazorの通常出力のためHTMLエンコードされる。
- `CanRegisterResult`は表示用モデルへ引き継がれており、後続の結果入力実装で重複ID等の登録無効化に利用できる。

## Verification

- `dotnet build Testman.sln --no-restore`: 成功（警告0、エラー0）
- `dotnet test tests/Testman.Web.Tests/Testman.Web.Tests.csproj --no-build --no-restore --logger "console;verbosity=detailed"`: 16件成功
- `dotnet test Testman.sln --no-build --no-restore`: Core 86件、Web 16件成功
- 対象コミットの`git diff --check`: 問題なし
- Git管理対象へのDB、`bin`、`obj`、`TestResults`混入: なし

## Residual risk

- 検証実施表示の結果入力は後続実装のため、現時点で`CanRegisterResult`に応じた登録操作の無効化は画面上で検証できない。
- 表示モードはクエリ文字列の完全一致で選択される。これは現行仕様に反しないが、将来URL仕様を定める場合は明文化が必要である。
