# testman

`testman` は、ファイルで管理されたテスト仕様をブラウザで閲覧し、テストの実施結果をSQLiteへ記録する小規模なWebアプリケーションです。

このリポジトリの主目的は、高機能なテスト管理製品を作ることではありません。Architect、Implementer、Reviewerという異なる役割を持つAIエージェントが、仕様・実装・レビューを分担する開発プロセスを試すことを目的としています。

## 現在の状態

V0.1の実装を進めています。現在有効なテスト仕様形式は [test-format.md](docs/test-format.md)、未決事項は [open-questions.md](docs/open-questions.md) を参照してください。

## 想定する利用方法

1. 利用者がGit管理されたテスト仕様ファイルを作成・更新する。
2. `testman` が仕様ファイルを読み込む。
3. 利用者がブラウザでテスト一覧と詳細を確認する。
4. 利用者がブラウザでPass、Fail、Blocked、N/Aのいずれかと、検証実施者名、テスト対象名、必要なコメントを入力する。
5. `testman` が結果と履歴をSQLiteへ保存する。

テスト仕様ファイルはGitで管理します。実行時に作成されるSQLiteデータベースはGitで管理しません。

## 技術選定

- C# / .NET 10 LTS
- ASP.NET Core Razor Pages
- SQLite / Microsoft.Data.Sqlite
- xUnit

これらの技術選定とlocalhost限定の構成は [ADR-001](docs/decisions/ADR-001-web-application-architecture.md) で承認済みです。

## 文書

- [要求仕様](docs/requirements.md)
- [製品コンセプト](docs/product-concept.md)
- [テスト仕様形式の候補](docs/test-format-options.md)
- [正式なテスト仕様形式](docs/test-format.md)
- [Web UI](docs/web-ui.md)
- [データベース](docs/database.md)
- [CLIと起動方法](docs/cli.md)
- [未決事項](docs/open-questions.md)

## エージェントとSkills

- 役割定義: `.agents/architect.md`、`.agents/implementer.md`、`.agents/reviewer.md`
- Skills: `.agents/skills/`
- リポジトリ全体の規則: `AGENTS.md`

## Git運用

- 作業単位を小さく保つ。
- コミットメッセージには `docs:`、`feat:`、`fix:`、`test:`、`refactor:` などの接頭辞を使う。
- エージェントは明示的に依頼された場合だけコミットする。
- エージェントは明示的な依頼なしにpushしない。
- DBファイル、ビルド成果物、テスト結果などの生成物はコミットしない。
