# Review: d4db27b

## Result

指摘なし。

## Reviewed scope

- 対象コミット: `d4db27b feat: prepare specification page content`
- `SpecificationCatalogResult`からWeb表示用データへの変換
- ファイル単位の形式バージョン、正常なtitle、診断の保持
- テストパターン表示におけるtitle、Overview、大項目、中項目、小項目の構成
- テストパターン表示からTest IDを除外する構成
- OverviewのMarkdown変換とHTMLサニタイズ
- 関連する確定仕様:
  - `docs/requirements.md`
  - `docs/web-ui.md`
  - `docs/test-format.md`

## Verification

- `dotnet build Testman.sln --no-restore`: 成功（警告0、エラー0）
- `dotnet test Testman.sln --no-build --no-restore`: 成功
  - Core: 86件成功
  - Web: 15件成功
- 対象コミットのdiff check: 問題なし
- Git管理対象にDB、`bin`、`obj`、`TestResults`の混入なし

## Notes and residual risks

- 今回のコミットは表示用データの準備までであり、Razor Pageへの接続と実画面上の配置は対象外である。
- 大項目、中項目、小項目は未加工の文字列として保持される。画面へ接続する際はRazorの既定エスケープを迂回しないこと。将来これらのセルで許可Markdown／HTMLを表示する場合は、Overviewと同様に安全なHTMLへ変換してから出力する必要がある。
- 診断を画面上部へ目立つ形で表示すること、および同一ファイル内で異常なtitleを除外し正常なtitleを継続表示することは、後続の画面接続時にも確認が必要である。
