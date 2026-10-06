# testman

`testman` は、ファイルで管理されたテスト仕様をブラウザで閲覧し、テストの実施結果をSQLiteへ記録する小規模なWebアプリケーションです。

このリポジトリの主目的は、高機能なテスト管理製品を作ることではありません。Architect、Implementer、Reviewerという異なる役割を持つAIエージェントが、仕様・実装・レビューを分担する開発プロセスを試すことを目的としています。

## Getting Started

### 配布版を使う

Windows x64向けの配布ZIPを展開し、PowerShellで展開先をカレントディレクトリにします。`--specs`にはGit管理されたテスト仕様Markdownのパスを指定します。次の例はこのリポジトリの `testspecs/testman-v01.md` を読み込む場合です。`C:\path\to\testman` は実際のリポジトリの絶対パスに置き換えてください。

```powershell
.\testman.exe serve --specs 'C:\path\to\testman\testspecs\testman-v01.md' --db .\testman.db --port 5000
```

`--specs`には `./testspecs/testman-v01.md` のような相対パスも指定できます。相対パスは起動時のカレントディレクトリを基準に解決されます。

ブラウザで `http://localhost:5000/` を開きます。DBは展開先の `testman.db` に作成されるため、配布版を入れ替える際もこのファイルを残してください。結果を保存するには、指定した仕様ファイルがGitにコミット済みで、未コミットの変更がないことが必要です。配布版の実行に.NET SDKや.NETランタイムの事前インストールは不要です。

### ソースからビルドして確認する

.NET 10 SDKを用意し、このリポジトリのルートディレクトリ（`testman`）をカレントディレクトリにして実行します。

```powershell
dotnet build .\Testman.sln
dotnet run --project .\src\Testman.Web\Testman.Web.csproj --no-build --launch-profile http -- serve --specs "$PWD\testspecs\testman-v01.md" --db "$PWD\.testman\testman.db" --port 5000
```

`http` 起動プロファイルのDevelopment環境で起動します。ブラウザで `http://localhost:5000/` を開き、テスト仕様「仕様パスからの起動」が表示されることを確認します。`Test patterns` と `Verification` を切り替えると、同じ仕様の概要とテストケースを確認できます。DBは `.testman/testman.db` に作成され、Gitの管理対象には含まれません。

どちらの起動方法でも、終了するときはターミナルで `Ctrl+C` を押します。ポート5000が使用中の場合は、起動コマンドの `--port` を空いているポートに変更し、ブラウザでも同じポートを指定してください。

## 利用方法

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
- [正式なテスト仕様形式](docs/test-format.md)
- [要求とテストケースの対応](docs/traceability.md)
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
