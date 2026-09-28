# テスト仕様形式の候補

## この文書の扱い

本書は比較検討用であり、テスト仕様形式を確定するものではない。正式な形式はサンプルを比較し、ユーザーの承認後に `docs/test-format.md` とADRへ記録する。

## 評価基準

- Git上で読みやすいか
- テキスト差分をレビューしやすいか
- 表形式のテスト仕様を書きやすいか
- セル内の長文、複数行、箇条書きを扱えるか
- 人間が特別なツールなしで編集できるか
- C#で安全かつ明確に解析できるか
- Web画面へ安全に変換できるか
- 将来の拡張時に互換性を保ちやすいか

## 案A: Markdown見出しとMarkdown表

```markdown
# Login tests

| ID | Title | Preconditions | Steps | Expected Result |
|---|---|---|---|---|
| T-AUTH-001 | Valid login | User exists | 1. Open login page<br>2. Enter credentials<br>3. Submit | Dashboard is displayed |
```

長所:

- Gitホスティングサービス上でもある程度読みやすい。
- 一般的なMarkdownエディターを利用できる。
- 表全体を一望できる。

短所:

- 複数行、箇条書き、縦棒、長い文章のエスケープが煩雑になる。
- Markdown表はCommonMark本体ではなく拡張仕様である。
- 複雑な手順を1セルへ詰め込むと編集しにくい。

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

## Architectの暫定推奨

案Bをベースに、1つのテストケースを見出しで区切り、手順部分だけを表にする構成を最初の比較基準とする。

理由は、テスト全体を1行へ押し込めず、表形式の利点も残せるためである。ただし、実際に利用するテスト仕様に近いサンプルを複数作成し、案Aまたは案Cの方が扱いやすくないか確認してから決定する。

V0.1では任意HTMLを入力形式として積極的に採用しない。HTMLが必要なら、許可するタグを限定し、表示前にサニタイズする。

## 決定前に確認すること

- 1テストあたりの典型的な手順数と文章量
- 期待結果はテスト全体に1つか、各手順に1つか
- 画像、リンク、コードブロック、改行が必要か
- テストケースを1ファイルに何件程度置くか
- 共通前提条件や章単位の説明が必要か
- 表計算ソフトとのコピー＆ペーストを重視するか
