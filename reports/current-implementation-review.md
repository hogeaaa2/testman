# Current Implementation Review

## Review scope

- Review date: 2026-10-01
- Reviewed branch: `main` (`origin/main` と同期した状態)
- Source of Truth: `docs/requirements.md`、関連する `docs/*.md`、Accepted ADR-001〜003
- Reviewed implementation: テスト仕様解析、診断と部分成功、Markdown/HTMLサニタイズ、仕様パス解決、ファイル/カタログ読込、CLI引数解析、Razor PagesホストとWebシェル
- 将来スコープであるWebへのカタログ接続、結果入力、SQLite永続化、履歴表示は今回の欠陥判定対象外とした。

## Findings

### Major: localhost限定がWebホストで強制されていない

- Affected location: `src/Testman.Web/Program.cs:1`
- Violated requirement:
  - `docs/requirements.md`: V0.1のWebサーバーはlocalhostで待ち受け、同一PCからだけ利用する。
  - `docs/web-ui.md`: V0.1の待受アドレスはlocalhostに限定する。
  - Accepted ADR-001: LAN公開は行わない。
- Explanation / reproduction:
  - `WebApplication.CreateBuilder(args)`が標準のKestrel設定をそのまま受け付け、コード内でloopbackへの制限を行っていない。
  - 現在のWebプロジェクトは、たとえば`--urls http://0.0.0.0:5000`や`ASPNETCORE_URLS`により全インターフェースへバインドできる。
  - Web統合テストはページ内容だけを確認し、待受アドレスの制約を検証していない。
- Impact:
  - 認証・認可を持たないV0.1を誤ってLANへ公開でき、Acceptedとなったセキュリティ境界を破る。
- Expected state:
  - 利用者設定や追加引数にかかわらずV0.1はloopbackだけで待ち受けるか、loopback以外の指定を明示的に拒否して終了する。
  - 実行時設定で外部バインドできないことを自動テストで確認する。

### Major: HTML属性の許可リストが要素ごとに制限されていない

- Affected location: `src/Testman.Core/Rendering/SafeMarkdownRenderer.cs:19`, `src/Testman.Core/Rendering/SafeMarkdownRenderer.cs:40-41`
- Violated requirement:
  - `docs/test-format.md`: `a`では`href`と`title`、`img`では`src`、`alt`、`title`、`width`、`height`、表要素では`colspan`と`rowspan`だけを許可する。
  - `docs/requirements.md`: 許可リスト方式で危険なHTML、属性、URLを除去する。
- Explanation / reproduction:
  - 実装は全許可属性を`sanitizer.AllowedAttributes`へ一括登録しており、属性と要素の組み合わせを制限していない。
  - そのため、例として`<p title="x">`、`<a src="https://example.com/image">`、`<img colspan="2">`のような、仕様ではその要素に許可されていない属性が残り得る。
  - 現在のテストは危険なイベント属性やURLの除去を確認するが、許可属性が不正な要素に付いた場合を確認していない。
- Impact:
  - 明示したサニタイズ境界より広いHTMLをブラウザへ出力する。現時点で直ちにスクリプト実行へつながる組み合わせは確認していないが、許可リストの保証と将来のライブラリ変更に対する安全余裕が損なわれる。
- Expected state:
  - 許可属性をタグとの組み合わせで検査し、仕様にない組み合わせは除去する。
  - 各タグの許可・不許可属性を自動テストで固定する。

### Major: UTF-8 BOM付きMarkdownを有効なUTF-8仕様として扱えない

- Affected location: `src/Testman.Core/Specifications/SpecificationFileLoader.cs:17-22`
- Violated requirement:
  - `docs/test-format.md`: 入力はUTF-8のMarkdownファイルとする。
  - `docs/requirements.md`: テスト仕様ソースを読み込み、解析エラーを利用者が特定できる形で扱う。
- Explanation / reproduction:
  - `StreamReader`で`detectEncodingFromByteOrderMarks: false`を指定しているため、UTF-8 BOM (`EF BB BF`) が先頭のU+FEFFとして本文に残る。
  - BOM付きファイルの先頭が見かけ上`Testman-Format-Version: 1`でも、ヘッダーパーサーは先頭一致に失敗し、形式バージョン欠落と診断して全行の結果登録を無効化する。
  - ファイルローダーのテストはBOMなしUTF-8と不正バイト列だけを扱っている。
- Impact:
  - UTF-8として妥当で、Windows系エディタ等から生成され得る仕様ファイルを正常に利用できない。
- Expected state:
  - UTF-8 BOMあり・なしをともに受理するか、BOM禁止を製品仕様として明記する。現在の「UTF-8」という仕様に従うなら、BOMを検出・除去して先頭行を解析する。

### Minor: シンボリックリンク／ジャンクション無視の自動テストがない

- Affected location: `tests/Testman.Core.Tests/SpecificationPathResolverTests.cs`
- Requirement at risk:
  - `docs/requirements.md`および`docs/test-format.md`: シンボリックリンクとWindowsのジャンクションをたどらない。
- Explanation:
  - 実装は`FileAttributes.ReparsePoint`を確認しているが、明示されたこの境界について、ルートがリンクの場合・配下のファイルリンク・配下のディレクトリリンク／ジャンクションのテストがない。
- Impact:
  - OS差異や探索処理の変更によって、指定範囲外のファイルを読み込む退行を検出できない。
- Expected state:
  - 実行環境で作成可能なリンク種別について、リンク先がカタログへ入らず探索もされないことを自動テストする。権限等で作成不能な環境では、その条件を明示してテストをスキップする。

## Verification results

- `dotnet build Testman.sln --no-restore`: 成功、警告0、エラー0
- `dotnet test Testman.sln --no-build --no-restore`: 成功
  - `Testman.Core.Tests`: 69 passed
  - `Testman.Web.Tests`: 2 passed
- Git管理状態:
  - レビュー開始時点で`main`は`origin/main`と同期
  - 追跡対象に`bin/`、`obj/`、テスト結果、coverage、SQLite実DBは見つからなかった
  - `.gitignore`には.NET生成物、テスト成果物、秘密設定候補、SQLite DBとsidecarの除外規則がある

## Confirmed behavior

- 正式な形式バージョン、必須セクション順、固定表列、必須セル、Test ID形式とファイル内重複を解析している。
- 不正な行は結果登録不可とし、正常titleや正常ファイルを残す部分成功のテストがある。
- `script`、イベント属性、`javascript:` URLの除去、および許可URLの基本テストがある。
- 単一ファイル、相対パス、再帰ディレクトリ、不存在、対象Markdownなしを扱っている。
- CLI引数解析はV0.1の`serve --specs <path>`以外を拒否する。
- Razor PagesホストとローカルBootstrap資産の基本HTTPテストがある。

## Residual risks

- Webシェルはまだ仕様カタログやCLI起動処理へ接続されていないため、診断表示・正常title表示・CLI終了コードはエンドツーエンドでは未検証である。これは現時点の後続実装範囲として扱い、欠陥には数えていない。
- 結果登録、CSRF、SQLite追記、DB障害、Git SHA、履歴保持は未実装のため今回評価できない。
- Markdownサニタイズは代表例のテストに留まる。URLの難読化、プロトコル相対URL、壊れたHTML、属性の大文字小文字などの境界ケースは、Web出力へ接続する前に追加の安全性テストが望ましい。
