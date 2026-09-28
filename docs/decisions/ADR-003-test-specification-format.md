# ADR-003: テスト仕様形式

- Status: Proposed
- Date: 2026-09-28

## Context

テスト仕様には見出しと表形式が必要になる可能性が高い。Markdown表、テストごとのMarkdownセクション、構造化データ、HTML tableにはそれぞれ異なる利点と制約がある。

## Proposed Direction

Markdownでテストごとのセクションを作り、手順部分に定義済みの表を使う案を最初の比較基準とする。

任意HTMLは安全性と解析の複雑さがあるため、V0.1の標準形式にはしない。必要性が確認された場合は、許可タグを限定してサニタイズする。

## Decision Criteria

- 実際のテスト仕様を無理なく記述できる。
- Git上のレビューと差分確認がしやすい。
- C#で曖昧さなく検証できる。
- 安全にWeb表示できる。
- 将来の変更時に互換性を管理できる。

## Required Validation

採用前に、実際の利用に近いテストケースを複数案で記述し、ユーザーと比較する。詳細は `docs/test-format-options.md` を参照する。
