# Review: 10fec77 feat: bind web host to CLI port

## 結論

コミット`10fec77`と修正コミット`87b68f9`の累積状態を再レビューし、Critical、Major、Minorの指摘なし。初回レビューで確認したKestrel endpoint環境設定によるCLIポート上書きは解消された。

## 解消済みの指摘

### Major（解消済み）: Kestrel endpoint設定が`--port`を上書きする

- 初回状態: `UseUrls("http://localhost:<CLI port>")`よりKestrelの`Endpoints`設定が優先され、loopback endpointの環境値があるとCLI指定ポートとは別のポートで起動した。
- 修正: `87b68f9`でWebホストの構成ソースをCLI起動用の空のInMemory構成へ置き換え、ASP.NET CoreのURL・Kestrel環境設定が待受へ影響しないようにした。
- テスト: プロセス結合テストに競合する`Kestrel__Endpoints__Http__Url`を与えたまま、CLI指定ポートへHTTP接続できることを追加確認している。
- 再現確認: `Kestrel__Endpoints__Http__Url=http://localhost:43122`を設定し、`--port 43121`でProduction起動したところ、`http://localhost:43121`で待受となった。
- 判定: `docs/cli.md`の「設定はコマンドライン引数だけ」「ポート変更後もlocalhost限定」を満たす。

## 確認範囲

- `docs/requirements.md`、`docs/cli.md`、`docs/web-ui.md`、`docs/open-questions.md`
- コミット`10fec77`と修正コミット`87b68f9`の累積差分
- CLI引数解析、localhost限定ガード、Webホスト起動処理
- CLI指定ポートと競合環境設定を含むWebプロセス結合テスト
- 環境名未指定のProduction起動
- 既存Web起動と全テスト
- Git管理対象へのDB・生成物混入

## 検証結果

- `dotnet build Testman.sln --no-restore`: 成功（警告0、エラー0）
- `dotnet test tests/Testman.Web.Tests/Testman.Web.Tests.csproj --no-build --no-restore`: 16件成功
- `dotnet test Testman.sln --no-build --no-restore`: Core 99件、Web 16件成功
- `git diff 10fec77^ 87b68f9 --check`: 問題なし
- Git管理対象にDB、`bin`、`obj`、`TestResults`なし
- 競合Kestrel endpoint`43122`、CLI指定ポート`43121`、`ASPNETCORE_ENVIRONMENT`未指定での実プロセス確認: Productionとして`43121`で待受

## 残存リスク

- プロセス結合テストは競合Kestrel endpoint環境値を明示してCLIポート優先を検証している。一方、テスト自身は`ASPNETCORE_ENVIRONMENT=Development`を指定する。環境名未指定のProduction起動については今回手動確認しており、この変更範囲での問題は確認されなかった。
