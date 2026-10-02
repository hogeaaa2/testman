# Review: 73d5f07 `feat: merge repeated pattern classifications`

## 最終結論

指摘なし。修正コミット `f289468` により、初回レビューのMinor 1件は解消した。対象コミットと修正コミットは、今回確認した範囲でテスト形式、Web UI方針、Accepted ADR-003に適合している。

## 初回指摘の再確認

### 解消: NULを含む分類値でMinor itemの親キーが衝突し、異なる上位階層をまたいで結合される

- 対象: `src/Testman.Web/Presentation/SpecificationPageContent.cs:100-105,116-145`
- 根拠:
  - `docs/test-format.md`「分類列で同じ実値が連続する場合、Web表示では階層境界を守ってセル結合できる」
  - Accepted ADR-003「下位分類の結合は同じ上位分類の範囲内に限定する」
- 説明: Minor item用の親キーは `$"{item.MajorItem}\0{item.MiddleItem}"` という文字列で作られる。テスト仕様は分類値内のNUL文字を禁止・診断していないため、異なる `(Major, Middle)` の組が同じ連結文字列になり得る。`CalculateRowSpans` はこの合成キーだけを比較するため、本来別階層の連続行を同じ親と判断する。
- 再現例:
  1. 連続する2行のMinor itemをどちらも `Same` にする。
  2. 1行目をMajor=`A\0B`、Middle=`C`、2行目をMajor=`A`、Middle=`B\0C` とする（`\0` はファイル中の実際のU+0000）。
  3. 両行の親キーはどちらも `A\0B\0C` となる。
  4. Major/Middleは別階層であるにもかかわらず、1行目のMinor itemに `rowspan="2"` が付き、2行目のMinor cellが省略される。
- 影響: 制御文字を含む特殊な仕様入力に限られるが、表示上の分類階層が入力と異なり、別のテスト群が同じMinor分類に見える。通常の人手入力では発生しにくく、結果保存や履歴には影響しない。
- 修正確認:
  - `CalculateRowSpans<TParent>` が親キー型をジェネリックに受け、`EqualityComparer<TParent>.Default` で構造比較するよう変更された。
  - Minor itemの親は `(MajorItem, MiddleItem)` のValueTupleで渡され、分類文字列を区切り文字で連結しない。
  - 旧実装では同じ文字列キーへ衝突する `("A\0B", "C")` と `("A", "B\0C")` を連続行に置き、Minor rowspanが `[1, 1]` になる回帰テストが追加された。
- 判定: 解消。

## 新規回帰の確認

新たなCritical、Major、Minorの指摘はない。Major itemは定数の整数親、Middle itemはMajor文字列、Minor itemはMajor/Middleタプルを使い、各階層の比較範囲が明確になった。値の連続判定、`-` の早期除外、rowspan算出とRazor描画には変更がなく、通常ケースへの回帰はない。

## 指定観点の確認結果

### 連続値のみの結合

- `CalculateRowSpans` は現在位置から同値が続く間だけ走査し、値が途切れた位置で区間を終了する。
- 同じ値が後から再登場しても、非連続なら別のrowspanとして描画される。

### 下位分類の上位範囲内制限

- Middle itemはMajor itemを親として比較するため、Majorが変わる位置を越えて結合しない。
- Minor itemはMajor/Middleの組を親として比較するため、通常の分類文字列ではどちらかが変わる位置を越えて結合しない。
- 修正後は親を構造比較するため、分類文字列の内容にかかわらず上位階層境界を維持する。

### `-` の非結合

- 対象分類値が半角ハイフン1文字の場合は区間走査を行わず、各行をrowspan 1の独立セルにする。
- Major、Middle、Minorの各列へ同じ処理を適用している。
- 連続した `-` がすべてrowspan 1になるテストが追加されている。

### HTML rowspanの妥当性

- 結合区間の先頭は区間長の正整数、後続行は0としてpresentationへ保持する。
- Razorはspanが0より大きい場合だけ`td`を描画するため、結合先頭だけに正の `rowspan` が付き、後続の重複セルは出力されない。
- `rowspan=0` や負数はHTMLへ出力されず、非結合セルはrowspan 1になる。

### pattern表示の安全性

- Major/Middle/Minor値はRazorの通常の `@pattern...` 出力でHTMLエスケープされる。
- rowspanはサーバー側で計算した整数だけを出力し、仕様文字列を属性値として使わない。
- 新しい処理はpattern modeだけのpresentation情報で、Test IDや結果入力をpattern表示へ追加していない。

## 確認範囲

- `AGENTS.md`
- `.agents/reviewer.md`
- `.agents/skills/review-implementation/SKILL.md`
- `docs/test-format.md`
- `docs/web-ui.md`
- Accepted ADR-003
- 実装コミット `73d5f07`、修正コミット `f289468` と関連するparser、presentation、Razor Page、Web tests

## 検証結果

- `dotnet test Testman.sln --no-restore`: 成功
- Testman.Core.Tests: 116 passed
- Testman.Web.Tests: 32 passed
- 合計: 148 passed、0 failed、0 skipped
- 別作業の未追跡 `.agents/skills/write-test-spec/` は確認・変更対象に含めていない

## 残存リスク

同値が一度途切れて再登場する非連続ケースは実装から正しく分離されることを確認したが、専用テストには含まれていない。修正時には合成キー衝突と併せて、非連続の再登場を明示的なrowspanテストとして固定すると安全である。
