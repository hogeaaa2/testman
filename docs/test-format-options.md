# テスト仕様形式の候補

## この文書の扱い

本書は比較検討の経緯を残す文書であり、実装根拠ではない。案Aを基礎とする形式が採用され、正式な規則は `docs/test-format.md`、判断はAcceptedのADR-003に記録されている。以下の候補例にある旧IDや未決表現は検討時点の記録である。

## 評価基準

- Git上で読みやすいか
- テキスト差分をレビューしやすいか
- 表形式のテスト仕様を書きやすいか
- セル内の長文、複数行、箇条書きを扱えるか
- 人間が特別なツールなしで編集できるか
- C#で安全かつ明確に解析できるか
- Web画面へ安全に変換できるか
- 将来の拡張時に互換性を保ちやすいか

## 案A: Markdown見出し、共通条件、Markdown表

### ユーザー確認済みの方向性

- `title`は個々の行の列ではなく、表全体の見出しとする。
- titleごとに、そのテスト群で「どのようなテストをするか」を説明する概要を必須とし、titleと表の間に置く。
- Preconditionsは見出しと表の間に置き、その表の全テストケースへ適用する。
- 各テストケースは表の1行とし、一意なIDを持つ。
- 1つのTest IDは1つのテスト意図を表す。1つの仕様ファイルにはissueのスコープに応じて複数のTest IDを置ける。
- Expected Resultは原則としてIDごとに1つとする。同じテスト意図の範囲内では、複数の手順に対応する複数の期待内容を1つのExpected Resultセルへ記載できる。
- Test IDは仕様ファイル単位で一意とする。同じ仕様ファイル内の最大番号の次を採番し、削除済みIDや欠番を再利用しない。
- 大項目、中項目、小項目の3列を用意する。
- 大項目、中項目、小項目に特に記載する内容がない場合は半角ハイフン1文字 (`-`) で示す。
- 分類列の `-` は「記載なし」を示す予約値であり、連続していてもWeb表示ではセル結合しない。
- 行別Stepsは任意とし、行別Stepsがない場合は半角ハイフン1文字 (`-`) で示す。
- 共通Stepsには「行別Stepsに記載があれば、それに従って操作する」のような条件付き手順を書ける。
- 共通Stepsから行別Stepsを参照する具体的な文章はテスト仕様の記述者に委ね、ツールは特定の推奨文言を要求しない。

### 提案中の事項

- 概要の見出し名、許可するMarkdown、文章量。
- 共通Stepsと行別Stepsの具体的な見出し名、列名、検証規則。
- Web表示では、分類列内で連続する同一の値をセル結合する。

```markdown
# Login tests

## Overview

Verify valid login and rejection of invalid or incomplete credentials.

## Preconditions

- User exists

## Common steps

1. Open login page
2. Enter the standard email address
3. If the row-specific Steps cell contains instructions, follow them
4. Submit the form

| ID | Major item | Middle item | Minor item | Steps | Expected Result |
|---|---|---|---|---|---|
| T-AUTH-001 | - | - | Valid credentials | Enter the valid password | Dashboard is displayed |
| T-AUTH-002 | - | - | Blank password | - | A validation error is displayed |
```

行別Steps列の `-` は表示用の文章ではなく、「この行に固有のStepsはない」ことを表す予約値である。

概要は詳細なStepsやExpected Resultの要約ではなく、レビュー時にテストパターンの目的と範囲を把握するための説明である。詳細内容は、この概要を具体化するものとして記述する。

### Web表示でのセル結合候補

標準的なMarkdown表には行方向のセル結合がないため、入力ファイルには各行の分類値をそのまま記載する。testmanのWeb表示では、連続する同一値を検出してHTMLの `rowspan` として描画する。

候補規則:

- 大項目は、連続する同一値を結合する。
- 中項目は、同じ大項目の範囲内にある連続する同一値を結合する。
- 小項目は、同じ大項目・中項目の範囲内にある連続する同一値を結合する。
- ID、Steps、Expected Resultは結合しない。
- `-` は「特に記載する内容がない」ことを示す予約値であり、分類値ではないため結合しない。

この方式では通常のMarkdownプレビューとの互換性を保てる。セル結合は表示上の整形に限定し、各行が持つ分類値やテストIDは変更しない。入力中の任意HTMLも不要である。

長所:

- Gitホスティングサービス上でもある程度読みやすい。
- 一般的なMarkdownエディターを利用できる。
- 表全体を一望できる。
- 前提条件や共通手順を、その表に含まれる全テストへまとめて適用できる。
- 大項目、中項目、小項目でケースを分類できる。

短所:

- 複数行、箇条書き、縦棒、長い文章のエスケープが煩雑になる。
- Markdown表はCommonMark本体ではなく拡張仕様である。
- 複雑な行別手順を1セルへ詰め込むと編集しにくい。
- Markdownプレビュー上では同じ分類値が行ごとに繰り返され、セル結合はtestmanのWeb表示に限られる。

## 案B: Markdown見出しと、テストごとのセクション

```markdown
# Login tests

## T-AUTH-001: Valid login

### Preconditions

- User exists

### Steps

| Step | Action | Expected Result |
|---:|---|---|
| 1 | Open login page | Login form is displayed |
| 2 | Enter credentials and submit | Dashboard is displayed |
```

長所:

- テスト単位の内容が読みやすい。
- 手順ごとに期待結果を対応させやすい。
- 説明や補足をMarkdownで追加しやすい。

短所:

- テストケース数が多いとファイルが長くなる。
- テスト一覧としての一覧性は案Aより低い。
- 許可する見出しや表の構造を明確に定義する必要がある。

## 案C: 構造化データとMarkdown説明の組み合わせ

```yaml
id: T-AUTH-001
title: Valid login
preconditions:
  - User exists
steps:
  - action: Open login page
    expected: Login form is displayed
  - action: Enter credentials and submit
    expected: Dashboard is displayed
```

長所:

- 機械的な解析と検証が容易である。
- フィールド追加や型の定義を行いやすい。
- 表のエスケープ問題を避けられる。

短所:

- Markdown中心という利用イメージから離れる。
- YAMLのインデントなど、編集上の別の注意点が生じる。
- Git上で完成形の表示を確認しにくい。

## 案D: Markdown内でHTML tableを許可

長所:

- colspan、複数行、複雑なレイアウトなど表現力が高い。
- ブラウザ表示との形の差が小さい。

短所:

- 人間が直接編集しにくい。
- Markdown parserごとの挙動差が生じやすい。
- 任意HTMLを表示するとスクリプト注入などの危険がある。
- HTMLの許可範囲、サニタイズ、解析規則が必要になる。

## 採用された方向

案Aをベースに、見出し、必須のOverview、表全体のPreconditions、必須のCommon steps、分類3列と一意なIDを持つ表を組み合わせる。

概要から詳細へ段階的にレビューでき、共通内容の重複を避けながら、各テストケースを一覧し、必要な行だけに具体的な手順を書けるためである。列名や検証規則を含む確定内容は `docs/test-format.md` を参照する。

GFM相当のMarkdownと生HTMLを入力できるが、表示前に許可リスト方式でサニタイズし、危険な内容を除去する。

## 検討時に確認した事項

以下は検討時の論点であり、V0.1の回答は `docs/test-format.md` に反映済みである。

- 1テストあたりの典型的な手順数と文章量
- 期待結果はテスト全体に1つか、各手順に1つか
- 画像、リンク、コードブロック、改行が必要か
- テストケースを1ファイルに何件程度置くか
- 共通前提条件や章単位の説明が必要か
- 表計算ソフトとのコピー＆ペーストを重視するか
