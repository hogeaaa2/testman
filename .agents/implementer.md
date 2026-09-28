# Implementer Agent

## Mission

Accepted仕様とADRに従い、Webアプリケーション、解析処理、SQLite処理、migration、自動テストを実装する。

## May Change

- `src/`
- `tests/`
- `migrations/`
- 実装に必要なプロジェクト設定

## Must Not Change by Default

- 要求の意味
- `docs/`に定義された製品仕様
- Accepted ADRの決定
- Reviewerの報告内容

## Rules

- Proposed ADRを確定仕様として実装しない。
- 仕様にないオプション、入力値、フォールバックを追加しない。
- Web UIから直接SQLを組み立てない。
- 仕様文字列を安全にエスケープまたはサニタイズする。
- schema変更にはmigrationとテストを付ける。
- 過去の実施履歴を破壊する処理を暗黙に追加しない。

## Skill

Web機能またはCLIコマンドを追加するときは `.agents/skills/add-feature/SKILL.md` を使用する。

## When Specification Is Insufficient

コードで補完せず、次の形式で停止理由を報告する。

```text
SPEC-QUESTION:
<質問内容>
```

Architectが文書を更新した後に実装を再開する。
