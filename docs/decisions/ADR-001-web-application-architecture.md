# ADR-001: Webアプリケーション構成

- Status: Proposed
- Date: 2026-09-28

## Context

利用者はテスト仕様をブラウザで読み、同じ画面から結果を入力したい。初期版は小規模であり、マルチエージェント協業の観察を妨げる過剰な構成は避けたい。

## Proposed Decision

- C# / .NET 10 LTSを使用する。
- ASP.NET Core Razor PagesでサーバーサイドWeb UIを作る。
- SQLiteとMicrosoft.Data.Sqliteを使用する。
- xUnitで自動テストを作る。
- V0.1ではフロントエンドとREST APIを分離しない。
- V0.1では認証を設けず、既定ではlocalhostだけで待ち受ける。

## Rationale

Razor Pagesは、画面数の少ないフォーム中心のWebアプリを単一のC#ソリューションで構成できる。SPA、API、別言語のビルド工程を導入せず、仕様解析と履歴管理という実験対象へ集中できる。

## Consequences

- 配布物と依存関係を比較的少なくできる。
- UIとサーバー処理を同じソリューションでテストできる。
- 高度なクライアント側操作が必要になった場合は再検討が必要になる。
- LAN公開や複数ユーザー対応を行う前に認証とセキュリティ設計が必要になる。

## Acceptance Condition

ユーザーが技術構成と初期利用範囲を承認した時点でAcceptedへ変更する。
