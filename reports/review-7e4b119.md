# Review: 7e4b119 `build: package self-contained Windows release`

## 結論

指摘なし。コミット `7e4b119` は、今回確認した範囲で要求、CLI方針、関連Accepted ADRに適合している。静的なプロジェクト設定テストに加え、実際のRelease publishとzip生成も成功した。

## 指定観点の確認結果

### Windows x64自己完結publish

- `CreateDistributionArchive=true` の場合だけ `RuntimeIdentifier=win-x64` と `SelfContained=true` を設定する。
- 実行したコマンド:
  - `dotnet publish src/Testman.Web/Testman.Web.csproj -c Release -p:CreateDistributionArchive=true --no-restore`
- publishは成功し、`artifacts/publish/win-x64/` に次を含む成果物を生成した。
  - `testman.exe`、`testman.dll`、deps/runtimeconfig
  - `hostfxr.dll`、`hostpolicy.dll`、`coreclr.dll`、CLR/JITおよび.NET runtime assemblies
  - `e_sqlite3.dll` とSQLite managed dependencies
  - ASP.NET Core runtime assemblies、アプリ依存DLL、static web assets
- .NETランタイムの事前インストールを要求しない自己完結配布として必要なホスト・runtime・native dependencyが含まれている。
- `AssemblyName=testman` によりCLI仕様どおり `testman.exe` が生成される。

### zip生成

- `CreateDistributionArchive` targetは `AfterTargets="Publish"` かつ同名propertyがtrueの場合だけ実行する。
- `ZipDirectory` はpublish directory全体を `artifacts/testman-win-x64.zip` へ圧縮し、既存zipを上書きする。
- 実publishログでzip targetの実行を確認した。
- zipは実在し、サイズは57,563,291 bytesだった。
- publish/zip出力先の `artifacts/` はgitignore対象であり、配布生成物をGit管理へ混入させない。

### サテライト言語制限

- 配布時だけ `SatelliteResourceLanguages=en;ja;zh-CN` を設定する。
- 繁体字中国語やその他言語は許可リストに含まれない。
- 実publish成果物には許可外文化名のサテライトディレクトリは確認されなかった。
- 現在の依存関係ではen/ja/zh-CNを含めサテライトresource directory自体が生成されていないが、仕様は存在しないリソースの生成を要求せず、配布へ含める場合の言語を限定するものであるため不適合ではない。

### 通常buildへの影響

- RID、SelfContained、PublishDir、archive pathはすべて `CreateDistributionArchive=true` のconditional property group内にある。
- 通常の `dotnet build Testman.sln --no-restore` は成功し、Web成果物は従来どおり `bin/Debug/net10.0/` に生成された。win-x64 publish directoryやzip targetは通常buildで有効にならない。
- `AssemblyName=testman` は通常buildにも適用されるが、project reference・テスト実行・Web host起動に問題はなく、配布CLI名を `testman` にする目的と一致する。

### テスト妥当性

- `DistributionConfigurationTests` は次を固定している。
  - win-x64 RID
  - SelfContained=true
  - en/ja/zh-CNのサテライト言語許可リスト
  - `testman` assembly name
  - Publish後かつ明示フラグ時だけzip targetを実行すること
  - zip source/destination propertyの接続
- これらは設定の意図しない削除・変更を軽量に検出できる。
- XML構造テストだけでは実際のruntime pack解決やzip task実行を保証しないが、今回のレビューでは実publishを追加実行し、成果物生成まで確認した。

## その他の確認

- 配布設定はWeb projectだけに置かれ、Coreやtestsの通常target framework/RIDを固定しない。
- schema/migrationやアプリ動作へ変更はない。
- runtime DB、秘密情報、テスト結果はコミット対象に追加されていない。
- 別作業の未追跡 `.agents/skills/write-test-spec/` は確認・変更対象に含めていない。

## 確認範囲

- `AGENTS.md`
- `.agents/reviewer.md`
- `.agents/skills/review-implementation/SKILL.md`
- `docs/requirements.md`
- `docs/cli.md`
- Accepted ADR-001、ADR-002、ADR-003
- コミット `7e4b119`
- `src/Testman.Web/Testman.Web.csproj`
- `tests/Testman.Web.Tests/DistributionConfigurationTests.cs`
- 実publish成果物

## 検証結果

- `dotnet build Testman.sln --no-restore`: 成功、0 warnings、0 errors
- `dotnet test Testman.sln --no-build --no-restore`: 成功
- Testman.Core.Tests: 116 passed
- Testman.Web.Tests: 34 passed
- 合計: 150 passed、0 failed、0 skipped
- Windows x64 self-contained Release publish: 成功
- zip archive生成: 成功

## 残存リスク

自動テストはcsprojの静的構成を検証しており、CI上で実publishしたzip内容を検査するテストではない。SDK/runtime packの更新、依存関係追加、MSBuild zip taskの環境差による配布回帰を継続的に検出するには、将来のrelease workflowで実publish後に `testman.exe`、hostfxr/hostpolicy、SQLite native DLL、許可外サテライトディレクトリ不在、zip展開成功を検査するのが安全である。
