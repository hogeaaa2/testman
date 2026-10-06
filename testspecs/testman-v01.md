Testman-Format-Version: 1

# 仕様パスからの起動

## Overview

`serve --specs` に指定した単一ファイルまたはディレクトリから、対象の Markdown 仕様を決定できることを確認する。存在しないパスと対象ファイルのないディレクトリは起動時に拒否する。

## Preconditions

- testman の実行ファイルを用意する。以下の相対パスは、試験用の空の作業ディレクトリをカレントディレクトリとして解釈する。
- 作業ディレクトリに `specs/one.md`、`specs/nested/two.md`、空の `empty/` を作成する。`one.md` の内容は次のとおりとする。`two.md` はこの内容の title `起動確認一` を `起動確認二` に置き換えて保存する。

  ```text
  Testman-Format-Version: 1

  # 起動確認一

  ## Overview

  仕様の読込を確認する。

  ## Preconditions

  なし

  ## Common steps

  なし

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 読込 | - | - | - | 起動確認一が表示される。 |
  ```
- 各ケースでは新しい DB パスを使い、使用するポートは実行環境で利用可能な値を選ぶ。同時に試験用サーバーを起動しない。

## Common steps

1. 行別 Steps のコマンドを作業ディレクトリから実行する。`<testman>` は用意した実行ファイルへのパス、`<port>` は選んだ空きポートに置き換える。
2. Web サーバーが起動したケースではブラウザで `http://localhost:<port>/` を開き、画面に表示された仕様ファイルと title を確認してからサーバーを正常に停止する。起動しないケースではターミナルのエラーと終了コードを確認する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-1 | 入力対象 | 単一ファイル | 相対パス | 1. `<testman> serve --specs ./specs/one.md --db ./one.db --port <port>` を実行する。 | Web 画面に `one.md` の title とケースが表示され、`two.md` の title とケースは表示されない。 |
| TC-2 | 入力対象 | ディレクトリ | サブディレクトリ | 1. `<testman> serve --specs ./specs --db ./directory.db --port <port>` を実行する。 | Web 画面に `one.md` と `nested/two.md` の両方の title とケースが表示される。 |
| TC-3 | 起動エラー | 指定パス | 不存在 | 1. `<testman> serve --specs ./missing --db ./missing.db --port <port>` を実行する。 | ターミナルのエラー出力に `./missing: Specification path was not found.` という1行が表示され、終了コードは `1` となる。`./missing` は `--specs` に入力した文字列そのものである。Web サーバーは起動しない。 |
| TC-4 | 起動エラー | ディレクトリ | 対象ファイルなし | 1. `<testman> serve --specs ./empty --db ./empty.db --port <port>` を実行する。 | ターミナルのエラー出力に `./empty: Specification path contains no Markdown specification files.` という1行が表示され、終了コードは `1` となる。`./empty` は `--specs` に入力した文字列そのものである。Web サーバーは起動しない。 |

# 仕様の閲覧

## Overview

同じ仕様をテストパターン表示と検証実施表示で確認し、表示する情報が承認済みの画面方針に従うことを確認する。対象は有効な仕様ファイル1件である。

## Preconditions

- `specs/view.md` に次の内容を UTF-8 で保存する。

  ```text
  Testman-Format-Version: 1

  # 表示確認

  ## Overview

  表示方法を確認する。

  ## Preconditions

  試験用データを使用する。

  ## Common steps

  対象画面を開く。

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 入力 | 文字列 | 空欄 | 1. 空欄のまま送信する。 | 入力欄は空欄のままである。 |
  | TC-2 | 入力 | 文字列 | 入力あり | 1. abcを入力して送信する。 | 入力欄にabcが表示される。 |
  ```
- testman を `view.md` と新しい試験用 DB を指定して起動する。各ケースは画面を開き直して開始する。

## Common steps

1. ブラウザで testman のルートURLを開き、画面に `表示確認` の title があることを確認する。
2. 行別 Steps に従って画面上部の表示モードのリンクを選び、表示された内容を確認する。仕様内の手順は実行しない。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-5 | 表示モード | テストパターン | 概要と分類 | 1. 画面上部の `Test patterns` を選択する。 | `表示確認`、`表示方法を確認する`、両行の `入力`、`文字列`、`空欄`、`入力あり` が表示される。`TC-1`、`TC-2` と `試験用データを使用する` は表示されない。 |
| TC-6 | 表示モード | 検証実施 | 手順と期待結果 | 1. 画面上部の `Verification` を選択する。 | `TC-1` と `TC-2` について `試験用データを使用する`、`対象画面を開く`、それぞれの行別 Steps と Expected result、および結果入力欄が表示される。 |
| TC-7 | 表示モード | 切替 | ケース保持 | 1. 画面上部の `Test patterns` を選択する。<br>2. 画面上部の `Verification` を選択する。<br>3. 画面上部の `Test patterns` を選択する。 | 切替前後で `表示確認` に属するケースは `TC-1` と `TC-2` の2件のままであり、各行の分類値は変わらない。 |

# 結果の登録と履歴

## Overview

有効な2ケースの仕様を使い、結果入力の初期状態、登録対象の選択、検証実施者名、追記履歴を確認する。各ケースは独立した試験用 DB から開始し、行別 Steps に履歴作成がある場合だけ同じ DB へ続けて登録する。

## Preconditions

- 試験用の Git リポジトリを作り、次の内容を `specs/result.md` として UTF-8 で保存してコミットする。各ケースの開始時点で、このファイルのインデックスと作業ツリーを HEAD と一致させる。試験用 DB はリポジトリ外に置く。

  ```text
  Testman-Format-Version: 1

  # 結果確認

  ## Overview

  2ケースの結果登録を確認する。

  ## Preconditions

  なし

  ## Common steps

  行別Stepsに従う。

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 入力 | 文字列 | 空欄 | 1. 空欄を送信する。 | 入力欄は空欄のままである。 |
  | TC-2 | 入力 | 文字列 | 入力あり | 1. abcを入力して送信する。 | 入力欄にabcが表示される。 |
  ```

- testman をこの `result.md` と各ケース専用の新しい DB で起動する。検証実施者名には、特記がなければ `Tester A`、テスト対象名には `testman.exe` を使う。コメントが必要な場合は `確認メモ` を使う。
- 登録前に `git log -1 --format=%H -- specs/result.md` で対象ファイルを最後に変更したコミットのSHAを控える。DB の内容を照合するケースでは SQLite を参照できる手段を用意する。

## Common steps

1. ブラウザで testman のルートURLを開き、画面上部の `Verification` を選択する。`結果確認` の title を画面内で探す。
2. 行別 Steps に従って結果と検証実施者名を入力し、指定されたボタンを選択する。
3. 登録後の画面、ケースごとの前回結果と履歴、必要に応じて試験用 DB を確認する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-8 | 初期表示 | 結果入力 | 未選択 | 1. 何も入力せず、`TC-1` と `TC-2` の前回結果と結果入力欄を見る。 | 両ケースの前回結果は `Not Tested` と表示され、両ケースの結果入力欄は未選択である。 |
| TC-9 | 登録 | 全件選択 | Pass と Fail | 1. `TC-1` の結果入力欄で `Pass`、`TC-2` の結果入力欄で `Fail` を選択する。<br>2. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力する。<br>3. `Save results` を選択する。<br>4. `TC-1` と `TC-2` の `History (1)` を開く。 | 確認操作を挟まず、登録後の画面に `Results saved.` と表示される。`TC-1` の前回結果は `Pass`、`TC-2` は `Fail` となり、両ケースの History に今回の実施者名、テスト対象名 `testman.exe`、結果が表示される。 |
| TC-10 | 登録 | 一部選択 | 確認後に登録 | 1. `TC-1` の結果入力欄で `Blocked` を選択し、`TC-2` は未選択のままにする。<br>2. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力する。<br>3. `Save results` を選択する。<br>4. 確認文の下に表示される `Confirm selected results` を選択する。 | 登録前に `Not all test cases have a result. Confirm saving only the selected cases.` と表示される。承認後は `TC-1` のみ新しい履歴が1件増え、`TC-2` の前回結果は `Not Tested` のままで履歴は増えない。 |
| TC-11 | 登録 | 一部選択 | 確認を中止 | 1. `TC-1` の結果入力欄で `N/A` を選択し、`TC-2` は未選択のままにする。<br>2. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力する。<br>3. `Save results` を選択する。<br>4. `Confirm selected results` を選択せず、画面上部の `Verification` を選択する。 | 登録前に `Not all test cases have a result. Confirm saving only the selected cases.` と表示される。中止後、`TC-1` と `TC-2` の履歴はいずれも増えず、前回結果はいずれも `Not Tested` のままである。 |
| TC-12 | 登録 | 全件未選択 | 入力エラー | 1. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力し、両ケースの結果入力欄は未選択のままにする。<br>2. `Save results` を選択する。 | 登録操作後の画面に `Select a result for at least one test case.` と表示され、両ケースの履歴は増えない。 |
| TC-13 | 登録 | 実施者名 | 空白のみ | 1. `TC-1` の結果入力欄で `Pass` を選択する。<br>2. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に半角スペース3文字だけを入力する。<br>3. `Save results` を選択する。 | 登録操作後の画面に `Executor name is required.` と表示され、両ケースの履歴は増えない。 |
| TC-14 | 登録 | コメント | 任意入力 | 1. `TC-1` と `TC-2` の結果入力欄で `Pass` を選択する。<br>2. `TC-1` のコメント欄に `確認メモ` を入力し、`TC-2` のコメント欄は空欄にする。<br>3. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力する。<br>4. `Save results` を選択する。<br>5. `TC-1` と `TC-2` の `History (1)` を開く。 | `TC-1` の履歴には `確認メモ` が表示され、`TC-2` の履歴にはコメントが表示されない。両ケースに今回の `Pass` の履歴が1件ずつ追加される。 |
| TC-15 | 履歴 | 同一ケース | 追記 | 1. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力し、`TC-1` の結果入力欄で `Pass` を選択する。<br>2. `Save results` を選択し、確認文の下の `Confirm selected results` を選択する。<br>3. 画面上部の `Verification` を選択し、画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力して `TC-1` の結果入力欄で `Fail` を選択する。<br>4. `Save results` を選択し、確認文の下の `Confirm selected results` を選択する。<br>5. `TC-1` の `History (2)` を開く。 | 2回目の登録後、`TC-1` の前回結果は `Fail` である。`TC-1` の履歴には1回目の `Pass` と2回目の `Fail` が別の実施記録として残る。 |
| TC-16 | 履歴 | 再表示 | 入力初期値 | 1. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力し、`TC-1` の結果入力欄で `Pass` を選択する。<br>2. `Save results` を選択し、確認文の下の `Confirm selected results` を選択する。<br>3. 画面上部の `Verification` を選択する。 | `TC-1` の前回結果は `Pass` と表示される。`TC-1` と `TC-2` の結果入力欄はどちらも未選択である。 |
| TC-17 | Git 管理 | 仕様ファイル | 未コミット変更 | 1. `result.md` の `2ケースの結果登録を確認する。` を `変更中` に変更して保存する。<br>2. 画面上部の `Verification` を選択する。<br>3. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力し、`TC-1` の結果入力欄で `Pass` を選択する。<br>4. `Save results` を選択する。<br>5. `Not all test cases have a result. Confirm saving only the selected cases.` と表示されたら、確認文の下の `Confirm selected results` を選択する。 | 変更後の仕様は閲覧できる。承認後の画面に `Results can only be saved for committed, unchanged Git specification files.` と表示され、`TC-1` と `TC-2` の履歴は増えない。 |

# ディレクトリ内の各仕様の閲覧と登録

## Overview

ディレクトリ指定で複数の Markdown 仕様を読み込んだとき、各ファイルのケースを閲覧して結果登録できることを確認する。両ファイルに同じ Test ID を使い、結果が仕様ファイルごとに区別されることも確認する。

## Preconditions

- 試験用の Git リポジトリに `specs/one.md` と `specs/nested/two.md` を UTF-8 で作り、両ファイルをコミットする。各ケースの開始時点で両ファイルのインデックスと作業ツリーを HEAD と一致させる。
- `one.md` の内容を次のとおりとする。

  ```text
  Testman-Format-Version: 1

  # 仕様一

  ## Overview

  一つ目の仕様を確認する。

  ## Preconditions

  なし

  ## Common steps

  行別Stepsに従う。

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 仕様一 | - | - | 1. 仕様一を確認する。 | 仕様一が表示される。 |
  ```

- `two.md` の内容を次のとおりとする。

  ```text
  Testman-Format-Version: 1

  # 仕様二

  ## Overview

  二つ目の仕様を確認する。

  ## Preconditions

  なし

  ## Common steps

  行別Stepsに従う。

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 仕様二 | - | - | 1. 仕様二を確認する。 | 仕様二が表示される。 |
  ```

- 試験用リポジトリのルートをカレントディレクトリとして、`<testman> serve --specs ./specs --db <db-path> --port <port>` で起動する。`<testman>` は用意した実行ファイル、`<db-path>` は各ケース専用の新しいリポジトリ外の DB の絶対パス、`<port>` は利用可能なポートに置き換える。登録前に `git log -1 --format=%H -- specs/one.md` と `git log -1 --format=%H -- specs/nested/two.md` で各ファイルを最後に変更したコミットのSHAを控える。

## Common steps

1. ブラウザで testman のルートURLを開く。
2. 行別 Steps に従って、指定した仕様ファイルの閲覧と結果登録を行う。
3. 登録後の検証実施表示と、試験用 DB の今回の結果を確認する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-18 | 複数ファイル | 仕様一 | 閲覧と登録 | 1. 画面上部の `Test patterns` を選択し、`one.md` の `仕様一` と分類 `仕様一` を確認する。<br>2. 画面上部の `Verification` を選択し、`one.md` の `TC-1`、Steps `1. 仕様一を確認する。`、Expected result `仕様一が表示される。` を確認する。<br>3. `one.md` の `TC-1` の結果入力欄で `Pass` を選択し、`two.md` の `TC-1` は未選択のままにする。<br>4. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力し、`Save results` を選択する。<br>5. `Not all test cases have a result. Confirm saving only the selected cases.` と表示されたら、確認文の下の `Confirm selected results` を選択する。<br>6. 登録したファイルの `TC-1` の `History (1)` を開く。 | `one.md` の指定内容が両表示で確認でき、登録後に `Results saved.` と表示される。`one.md` の `TC-1` の前回結果は `Pass` となり、History に `Tester A` と `testman.exe` が表示される。`two.md` の `TC-1` の前回結果は `Not Tested` のままである。DB の今回の結果の `source_file` は `specs/one.md` である。 |
| TC-19 | 複数ファイル | 仕様二 | 閲覧と登録 | 1. 画面上部の `Test patterns` を選択し、`two.md` の `仕様二` と分類 `仕様二` を確認する。<br>2. 画面上部の `Verification` を選択し、`two.md` の `TC-1`、Steps `1. 仕様二を確認する。`、Expected result `仕様二が表示される。` を確認する。<br>3. `two.md` の `TC-1` の結果入力欄で `Pass` を選択し、`one.md` の `TC-1` は未選択のままにする。<br>4. 画面下部の `Test target name` に `testman.exe` を入力し、画面下部の `Executed by` に `Tester A` を入力し、`Save results` を選択する。<br>5. `Not all test cases have a result. Confirm saving only the selected cases.` と表示されたら、確認文の下の `Confirm selected results` を選択する。<br>6. 登録したファイルの `TC-1` の `History (1)` を開く。 | `two.md` の指定内容が両表示で確認でき、登録後に `Results saved.` と表示される。`two.md` の `TC-1` の前回結果は `Pass` となり、History に `Tester A` と `testman.exe` が表示される。`one.md` の `TC-1` の前回結果は `Not Tested` のままである。DB の今回の結果の `source_file` は `specs/nested/two.md` である。 |

# CLI の入力と配布物

## Overview

仕様パス、DB パス、ポートの指定と起動失敗時の終了状態を確認する。ポートの受入範囲は下限 `1`、中間 `32768`、上限 `65535` を個別に検証する。シンボリックリンクとジャンクションは入力対象から除外する。

## Preconditions

- Windows の試験用作業ディレクトリを作り、その中に `specs/a.md`、`specs/nested/b.md`、`specs/no.txt`、空の `empty/` を置く。`a.md` と `b.md` は次の内容を保存し、`b.md` の title だけを `CLI B` に置き換える。`no.txt` の内容は `not markdown` とする。

  ```text
  Testman-Format-Version: 1

  # CLI A

  ## Overview

  起動対象を確認する。

  ## Preconditions

  なし

  ## Common steps

  なし

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 起動 | - | - | - | CLI A が表示される。 |
  ```

- `<testman>` は実行ファイルの絶対パス、`<root>` は試験用作業ディレクトリの絶対パスとする。各ケースは作業ディレクトリをカレントディレクトリとし、新しい DB を使用する。ポート `1`、`5000`、`32768`、`65535` は試験環境で利用可能な状態にする。リンクの作成には必要な権限を用意する。
- サーバーが起動したケースは指定ポートの `http://localhost:<port>/` で画面を開き、確認後に通常の終了操作で停止する。起動に失敗するケースは標準エラー出力と終了コードを確認する。正常終了時の終了コードも記録する。

## Common steps

1. 行別 Steps のコマンドまたはファイル操作を作業ディレクトリから行う。
2. サーバーが起動した場合は指定ポートの画面と終了状態を確認する。起動しない場合はターミナルの表示と終了コードを確認する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-20 | CLI引数 | 必須 | `--specs` なし | 1. `<testman> serve` を実行する。 | 標準エラー出力の1行は `Usage: testman serve --specs <path> [--db <path>] [--port <1-65535>]` であり、終了コードは `1`。サーバーは起動しない。 |
| TC-21 | CLI引数 | 不正 | 未定義オプション | 1. `<testman> serve --specs ./specs/a.md --config ./config.json` を実行する。 | 標準エラー出力の1行は `Usage: testman serve --specs <path> [--db <path>] [--port <1-65535>]` であり、終了コードは `1`。サーバーは起動しない。 |
| TC-22 | 仕様パス | 絶対パス | 単一ファイル | 1. `<testman> serve --specs <root>/specs/a.md --db ./absolute-file.db --port 32768` を実行する。 | `http://localhost:32768/` に `CLI A` のケースだけが表示され、`CLI B` は表示されない。通常終了後の終了コードは `0`。 |
| TC-23 | 仕様パス | 絶対パス | ディレクトリ | 1. `<testman> serve --specs <root>/specs --db ./absolute-directory.db --port 32768` を実行する。 | `http://localhost:32768/` に `CLI A` と `CLI B` の両方のケースが表示される。通常終了後の終了コードは `0`。 |
| TC-24 | 仕様パス | ファイル | `.md`以外 | 1. `<testman> serve --specs ./specs/no.txt --db ./wrong-extension.db --port 32768` を実行する。 | 標準エラー出力の1行は `./specs/no.txt: Specified file is not a Markdown specification.` であり、終了コードは `1`。サーバーは起動しない。 |
| TC-25 | DBパス | 既定値 | カレントディレクトリ | 1. `<testman> serve --specs ./specs/a.md` を実行する。<br>2. `http://localhost:5000/` を開いてからサーバーを通常終了する。 | `CLI A` が表示され、作業ディレクトリの `.testman/testman.db` が SQLite DB として作成される。通常終了後の終了コードは `0`。 |
| TC-26 | DBパス | 指定値 | 相対パス | 1. `<testman> serve --specs ./specs/a.md --db ./data/custom.db --port 32768` を実行する。 | `CLI A` が表示され、作業ディレクトリの `data/custom.db` が作成される。`.testman/testman.db` は作成されない。 |
| TC-27 | DBパス | 指定値 | 絶対パス | 1. `<testman> serve --specs ./specs/a.md --db <root>/data/absolute.db --port 32768` を実行する。 | `CLI A` が表示され、`<root>/data/absolute.db` が作成される。`.testman/testman.db` は作成されない。 |
| TC-28 | ポート | 受入範囲 | 下限 `1` | 1. `<testman> serve --specs ./specs/a.md --db ./port1.db --port 1` を実行する。 | `http://localhost:1/` で `CLI A` が表示され、通常終了後の終了コードは `0`。 |
| TC-29 | ポート | 受入範囲 | 中間 `32768` | 1. `<testman> serve --specs ./specs/a.md --db ./port32768.db --port 32768` を実行する。 | `http://localhost:32768/` で `CLI A` が表示され、通常終了後の終了コードは `0`。 |
| TC-30 | ポート | 受入範囲 | 上限 `65535` | 1. `<testman> serve --specs ./specs/a.md --db ./port65535.db --port 65535` を実行する。 | `http://localhost:65535/` で `CLI A` が表示され、通常終了後の終了コードは `0`。 |
| TC-31 | ポート | 範囲外 | `0` | 1. `<testman> serve --specs ./specs/a.md --port 0` を実行する。 | 標準エラー出力の1行は `Usage: testman serve --specs <path> [--db <path>] [--port <1-65535>]` であり、終了コードは `1`。サーバーは起動しない。 |
| TC-32 | ポート | 範囲外 | `65536` | 1. `<testman> serve --specs ./specs/a.md --port 65536` を実行する。 | 標準エラー出力の1行は `Usage: testman serve --specs <path> [--db <path>] [--port <1-65535>]` であり、終了コードは `1`。サーバーは起動しない。 |
| TC-33 | リンク | ファイル | シンボリックリンク | 1. `specs/link.md` を `specs/a.md` へのシンボリックリンクとして作成する。<br>2. `<testman> serve --specs ./specs --db ./file-link.db --port 32768` を実行する。 | `CLI A` と `CLI B` は各1件表示され、`link.md` の仕様は表示されない。 |
| TC-34 | リンク | ディレクトリ | シンボリックリンク | 1. `specs/link-dir` を `specs/nested` へのディレクトリシンボリックリンクとして作成する。<br>2. `<testman> serve --specs ./specs --db ./dir-link.db --port 32768` を実行する。 | `CLI A` と `CLI B` は各1件表示され、`link-dir/b.md` からのケースは表示されない。 |
| TC-35 | リンク | ディレクトリ | ジャンクション | 1. `specs/junction-dir` を `specs/nested` への Windows ジャンクションとして作成する。<br>2. `<testman> serve --specs ./specs --db ./junction.db --port 32768` を実行する。 | `CLI A` と `CLI B` は各1件表示され、`junction-dir/b.md` からのケースは表示されない。 |

# テスト仕様の構造と診断

## Overview

正式形式のヘッダー、title、セクション、表、セル、Test IDを受け入れ、構造違反をファイルまたはtitle単位で診断することを確認する。各行では変更した条件だけを変え、ほかの要素は基準仕様に固定する。

## Preconditions

- 試験用 Git リポジトリに、次の UTF-8 の `base.md` を用意する。各ケースは `base.md` を `trial.md` にコピーしてから開始し、行別 Steps の編集後に `trial.md` をコミットする。DB はケースごとに新しいファイルを使う。

  ```text
  Testman-Format-Version: 1

  # 基準

  ## Overview

  基準ケースを確認する。

  ## Preconditions

  なし

  ## Common steps

  行別Stepsに従う。

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 入力 | 文字列 | 空欄 | 1. 空欄を送信する。 | 空欄が表示される。 |
  | TC-2 | 入力 | 文字列 | 入力あり | 1. abcを送信する。 | abcが表示される。 |
  ```

- `<testman>` は testman の実行ファイル、`<port>` は使用可能なポートとする。診断のパスは `trial.md` の絶対パスとし、行番号は編集後の `trial.md` を先頭行から1として数える。各ケースでサーバー起動後に `http://localhost:<port>/?mode=verification` を開く。
- 行別 Steps で「2番目のtitleを追加する」とある場合は、次の行をファイル末尾へ追加する。

  ```text

  # 第二

  ## Overview

  第二の概要。

  ## Preconditions

  なし

  ## Common steps

  なし

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-3 | 第二 | - | - | - | 第二の結果。 |
  ```

## Common steps

1. `base.md` を `trial.md` にコピーし、行別 Steps の編集を適用して保存する。
2. 編集済みの `trial.md` を Git にコミットし、`<testman> serve --specs ./trial.md --db <case-db> --port <port>` を試験用リポジトリのルートから実行する。`<case-db>` はケースごとの新しい DB の絶対パスに置き換える。
3. ブラウザで `http://localhost:<port>/?mode=verification` を開き、診断欄、title、行、結果入力欄を確認する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-36 | ファイル | 文字コード | UTF-8 BOM | 1. `trial.md` を UTF-8 BOM 付きで保存する。 | `Format version 1`、`基準` の `TC-1` と `TC-2` が表示され、診断欄に `No parsing diagnostics.` と表示される。先頭の BOM は画面に表示されない。 |
| TC-37 | ファイル | 文字コード | 不正なUTF-8 | 1. `trial.md` の末尾へバイト `FF` を1個追加する。 | 画面上部の診断に `trial.md` の絶対パスと `Specification file is not valid UTF-8.` が表示される。`trial.md` の title とケースは表示されない。 |
| TC-38 | title | 複数 | レベル1見出し | 1. 2番目のtitleを追加する。 | `基準` の `TC-1`・`TC-2` と `第二` の `TC-3` が別のtitleとして表示される。診断欄は `No parsing diagnostics.` である。 |
| TC-39 | title | 必須 | レベル1見出し欠落 | 1. `# 基準` の行を削除する。 | 画面上部の診断に `trial.md` の絶対パスと `A title block is required.` が表示され、ケースは表示されない。 |
| TC-40 | セクション | 必須 | Overview欠落 | 1. `## Overview` と直後の `基準ケースを確認する。` を削除する。 | 診断欄に `trial.md` の絶対パス、編集後のtitle見出しの行番号、`Required section 'Overview' is missing.` が表示され、`基準` のケースは表示されない。 |
| TC-41 | セクション | 必須 | 順序違反 | 1. `## Overview` とその本文を `## Preconditions` とその本文の後へ移動する。 | 診断欄に `trial.md` の絶対パス、title見出しの行番号、`Required sections are not in the approved order.` が表示され、`基準` のケースは表示されない。 |
| TC-42 | セクション | 本文 | 空のPreconditions | 1. `## Preconditions` の直後の `なし` を削除し、次の `## Common steps` との間は空行だけにする。 | 診断欄に `trial.md` の絶対パス、title見出しの行番号、`Required section 'Preconditions' must not be empty.` が表示され、`基準` のケースは表示されない。 |
| TC-43 | 表 | 個数 | 欠落 | 1. `trial.md` の表ヘッダーから最終行までを削除する。 | 診断欄に `trial.md` の絶対パス、title見出しの行番号、`The title must contain exactly one test case table after Common steps.` が表示され、`基準` のケースは表示されない。 |
| TC-44 | 表 | 個数 | 2表 | 1. `trial.md` の表の後に空行と同じ表をもう1つ追加する。 | 診断欄に `trial.md` の絶対パス、title見出しの行番号、`The title must contain exactly one test case table after Common steps.` が表示され、`基準` のケースは表示されない。 |
| TC-45 | 表 | 列 | 列名違い | 1. 表ヘッダーの `Expected result` を `Expected` に置き換える。 | 診断欄に `trial.md` の絶対パス、表の開始行番号、`Test case table columns do not match the approved names and order.` が表示され、`基準` のケースは表示されない。 |
| TC-46 | 表 | 行 | 0件 | 1. `trial.md` の `TC-1` と `TC-2` の行を削除する。 | 診断欄に `trial.md` の絶対パス、表の開始行番号、`Test case table must contain one or more test case rows.` が表示され、`基準` のケースは表示されない。 |
| TC-47 | セル | 必須 | Expected result空欄 | 1. `TC-1` の Expected result セルの `空欄が表示される。` を削除して空セルにする。 | 診断欄に `trial.md` の絶対パス、`TC-1` の行番号、`Required cell 'Expected result' must not be empty.` が表示される。`TC-1` の結果登録欄は使えず、`TC-2` の結果登録欄は使える。 |
| TC-48 | セル | 分類 | `-` | 1. `TC-1` の Major item、Middle item、Minor item をそれぞれ `-` にする。 | `TC-1` の分類3列はそれぞれ `-` と表示され、空セルとしては扱われない。診断欄は `No parsing diagnostics.` である。 |
| TC-49 | セル | Steps | 空欄 | 1. `TC-1` の Steps セルを空欄にする。 | `TC-1` の Steps 欄は空欄で、診断欄は `No parsing diagnostics.` である。`TC-1` の結果入力欄は使える。 |
| TC-50 | セル | Steps | `-` | 1. `TC-1` の Steps セルを `-` にする。 | `TC-1` の Steps 欄には `-` が表示され、診断欄は `No parsing diagnostics.` である。 |
| TC-51 | セル | 記法 | 縦棒と改行 | 1. `TC-1` の Expected result セルを、`A`、バックスラッシュ1文字、縦棒1文字、`B<br>C` を順に連結した文字列に変更する。 | `TC-1` の Expected result 欄の1行目に `A`、縦棒、`B` がこの順に表示され、改行後に `C` が表示される。表の列数は6列のままであり、診断欄は `No parsing diagnostics.` である。 |
| TC-52 | ID | 形式 | 先頭ゼロ | 1. `TC-1` を `TC-01` に変更する。 | 診断欄に `trial.md` の絶対パス、変更した行番号、`Invalid Test ID: TC-01` が表示され、変更した行の結果登録欄は使えない。 |
| TC-53 | ID | 重複 | 同一ファイル | 1. `TC-2` を `TC-1` に変更する。 | 診断欄に `trial.md` の絶対パスと `Duplicated ID detected: TC-1` が表示され、両方の `TC-1` の結果登録欄は使えない。 |
| TC-54 | ヘッダー | 必須 | 欠落 | 1. 先頭の `Testman-Format-Version: 1` を削除する。 | 診断欄に `trial.md` の絶対パス、行番号 `1`、`Format version is missing from the first line.` が表示される。解析できる `基準` は表示されるが、そのファイルの結果登録欄は使えない。 |
| TC-55 | ヘッダー | 値 | 数値以外 | 1. 先頭行を `Testman-Format-Version: abc` に変更する。 | 診断欄に `trial.md` の絶対パス、行番号 `1`、`Format version must be a positive integer.` が表示される。解析できる `基準` は表示されるが、そのファイルの結果登録欄は使えない。 |
| TC-56 | ヘッダー | 値 | 未知の正整数 | 1. 先頭行を `Testman-Format-Version: 7` に変更する。 | 画面には `Format version 7` と `基準` の `TC-1`・`TC-2` が表示され、診断欄は `No parsing diagnostics.` である。バージョン7用の別構造を要求しない。 |
| TC-57 | 部分表示 | titleごと | 無効な第一title | 1. `## Preconditions` と直後の `なし` を削除する。<br>2. 2番目のtitleを追加する。 | 診断欄に `Required section 'Preconditions' is missing.` が表示され、`基準` のケースは表示されない。`第二` の `TC-3` は表示される。 |

# MarkdownとHTMLの安全な表示

## Overview

Markdownと許可された生HTMLが表示され、許可外の要素・属性・URLとスクリプト実行が除去されることを確認する。画面全体のHTMLではなく、試験用仕様のOverviewとケースセルのDOMを観察する。

## Preconditions

- 各ケース専用の Git リポジトリと新しい DB を用意する。`safe.md` に次の有効な仕様を UTF-8 で保存し、行別 Steps で変更した後にコミットする。ブラウザはケースごとに新しいプロファイルを使う。

  ```text
  Testman-Format-Version: 1

  # 表示安全性

  ## Overview

  表示用本文。

  ## Preconditions

  なし

  ## Common steps

  なし

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 描画 | - | - | - | 表示を確認する。 |
  ```

- 次の3つの置換内容を使う。`GFM` は次の本文とする。

  ````markdown
  ## 見出し

  段落。**太字** *斜体* ~~削除~~ [相対リンク](./detail) `inline` <https://example.invalid/x>

  1. 番号付き

  - 箇条書き

  > 引用

  ![画像](./image.png)

  ```text
  コードブロック
  ```

  | 列A | 列B |
  |---|---|
  | A | B |
  ````

- `許可HTML` は次の内容とする。

  ```html
  <h1>見出し1</h1><h2>見出し2</h2><h3>見出し3</h3><h4>見出し4</h4><h5>見出し5</h5><h6>見出し6</h6>
  <p><strong>強調</strong><em>斜体</em><del>削除</del><code>code</code><kbd>K</kbd><sub>sub</sub><sup>sup</sup></p>
  <pre>pre</pre><blockquote>引用</blockquote><ul><li>項目</li></ul><ol><li>番号</li></ol><hr><br>
  <details><summary>詳細</summary>本文</details>
  <a href="./detail" title="説明">リンク</a><img src="./image.png" alt="画像" title="説明" width="12" height="13">
  <table><thead><tr><th colspan="2">見出し</th></tr></thead><tbody><tr><td rowspan="1">A</td><td>B</td></tr></tbody></table>
  ```

- `危険HTML` は次の内容とする。

  ```html
  <script>window.__testman_probe = 1</script><style>body{display:none}</style>
  <iframe src="./detail"></iframe><object data="./detail"></object><embed src="./detail">
  <form><input value="x"><button>押す</button></form><svg><circle /></svg>
  <a href="javascript:alert(1)" onclick="window.__testman_probe=2" style="color:red">危険リンク</a>
  <img src="javascript:alert(1)" onerror="window.__testman_probe=3" alt="危険画像">
  <img src="mailto:test@example.invalid" alt="mail画像">
  ```

## Common steps

1. 行別 Steps に従って `safe.md` の Overview または Expected result を編集し、ファイルを Git にコミットする。
2. `<testman> serve --specs ./safe.md --db <case-db> --port <port>` を起動し、ブラウザで `http://localhost:<port>/?mode=verification` を開く。`<case-db>` は新しいDBの絶対パス、`<port>` は使用可能なポートに置き換える。
3. `表示安全性` の本文・ケースセルを見て、必要に応じてブラウザの開発者ツールでそのDOMと `window.__testman_probe` を調べる。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-58 | Markdown | GFM | 表示 | 1. Overviewの `表示用本文。` を `GFM` に置き換える。 | `表示安全性` の本文に見出し、段落、strong、em、del、相対リンク、自動リンク、インラインコード、ol、ul、blockquote、img、pre/code、tableがそれぞれDOM要素として表示される。 |
| TC-59 | HTML | 許可要素 | 本文 | 1. Overviewの `表示用本文。` を `許可HTML` に置き換える。 | `表示安全性` の本文内に h1、h2、h3、h4、h5、h6、p、strong、em、del、code、kbd、sub、sup、pre、blockquote、ul、ol、li、hr、br、details、summary、a、img、table、thead、tbody、tr、th、td がDOM要素として残る。 |
| TC-60 | HTML | 許可属性 | URLと表 | 1. Overviewの `表示用本文。` を `許可HTML` に置き換える。 | 本文内のaにhref `./detail` とtitle `説明`、imgにsrc `./image.png`、alt `画像`、title `説明`、width `12`、height `13`、thにcolspan `2`、tdにrowspan `1` が残る。 |
| TC-61 | HTML | 許可URL | 種類 | 1. Overviewの `表示用本文。` を、hrefが `./detail`、`http://example.invalid/x`、`https://example.invalid/x`、`mailto:test@example.invalid` の4つのaと、srcが `./image.png`、`http://example.invalid/image.png`、`https://example.invalid/image.png` の3つのimgを並べたHTMLに置き換える。 | 本文内の4つのaのhrefと3つのimgのsrcが指定した値のまま残る。 |
| TC-62 | HTML | 許可外要素 | 除去 | 1. Overviewの `表示用本文。` を `危険HTML` に置き換える。 | `表示安全性` の本文DOMにscript、style、iframe、object、embed、form、input、button、svg、circle要素が存在しない。開発者ツールで `window.__testman_probe` を評価した結果は `undefined` であり、ページ本文は表示される。 |
| TC-63 | HTML | 許可外属性・URL | 除去 | 1. Overviewの `表示用本文。` を `危険HTML` に置き換える。 | 本文内のaとimgには `onclick`、`onerror`、`style`、`javascript:` で始まるhrefまたはsrcが残らず、`mail画像` のimgに `mailto:test@example.invalid` のsrcは残らない。開発者ツールで `window.__testman_probe` を評価した結果は `undefined` である。 |

# 結果入力と実施履歴

## Overview

1ファイルに2ケースを持つ仕様で、選択可能な結果、検証実施者名、テスト対象名、コメント、実施時刻、追記履歴を確認する。各ケースは新しいDBから始め、行別Stepsに複数回の登録がある場合だけ履歴を引き継ぐ。

## Preconditions

- 試験用の Git リポジトリに次の `specs/run.md` を UTF-8 で作成してコミットする。各ケースの開始時点で同ファイルのインデックスと作業ツリーを HEAD に一致させる。`git log -1 --format=%H -- specs/run.md` の出力を `<spec-sha>` として控える。

  ```text
  Testman-Format-Version: 1

  # 実施確認

  ## Overview

  結果を記録する。

  ## Preconditions

  なし

  ## Common steps

  行別Stepsに従う。

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 実施 | - | 一番目 | 1. 一番目を確認する。 | 一番目が表示される。 |
  | TC-2 | 実施 | - | 二番目 | 1. 二番目を確認する。 | 二番目が表示される。 |
  ```

- testman を `--specs ./specs/run.md --db <case-db> --port <port>` で試験用リポジトリから起動する。`<case-db>` は各ケース専用の新しいDBの絶対パス、`<port>` は利用可能なポートとする。SQLで確認するケースではDBビューアを用意し、参照対象の `result_submissions` と `test_results` は同じDBとする。
- 登録操作で特記がなければ、`Test target name` は `testman.exe`、`Executed by` は `Tester A`、他方のケースの結果は未選択とする。一方のケースだけを登録するときは、`Save results` の後に `Not all test cases have a result. Confirm saving only the selected cases.` を確認し、`Confirm selected results` を選択する。

## Common steps

1. ブラウザで `http://localhost:<port>/` を開き、画面上部の `Verification` を選択する。
2. 行別Stepsに従って、`実施確認` の結果入力欄と画面下部の入力欄を操作する。
3. 保存後は `Previous result` と必要な `History (n)` を開き、行別Stepsで指定したDBの照合を行う。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-64 | 保存値 | 結果 | Pass | 1. `TC-1` に `Pass` を選択し、`Test target name` に `testman.exe`、`Executed by` に `Tester A` を入力する。<br>2. `Save results`、続いて `Confirm selected results` を選択する。<br>3. DBの `test_results` を参照する。 | `TC-1` の前回結果は `Pass`。DBに `test_case_id=TC-1`、`result=pass` の行が1件追加され、`TC-2` の行はない。 |
| TC-65 | 保存値 | 結果 | Fail | 1. `TC-1` に `Fail` を選択し、`Test target name` に `testman.exe`、`Executed by` に `Tester A` を入力する。<br>2. `Save results`、続いて `Confirm selected results` を選択する。<br>3. DBの `test_results` を参照する。 | `TC-1` の前回結果は `Fail`。DBに `test_case_id=TC-1`、`result=fail` の行が1件追加される。 |
| TC-66 | 保存値 | 結果 | Blocked | 1. `TC-1` に `Blocked` を選択し、`Test target name` に `testman.exe`、`Executed by` に `Tester A` を入力する。<br>2. `Save results`、続いて `Confirm selected results` を選択する。<br>3. DBの `test_results` を参照する。 | `TC-1` の前回結果は `Blocked`。DBに `test_case_id=TC-1`、`result=blocked` の行が1件追加される。 |
| TC-67 | 保存値 | 結果 | N/A | 1. `TC-1` に `N/A` を選択し、`Test target name` に `testman.exe`、`Executed by` に `Tester A` を入力する。<br>2. `Save results`、続いて `Confirm selected results` を選択する。<br>3. DBの `test_results` を参照する。 | `TC-1` の前回結果は `N/A`。DBに `test_case_id=TC-1`、`result=not_applicable` の行が1件追加される。 |
| TC-68 | 入力 | テスト対象名 | 空白のみ | 1. `TC-1` に `Pass` を選択し、`Test target name` に半角スペース3文字、`Executed by` に `Tester A` を入力する。<br>2. `Save results` を選択する。 | 画面に `Test target name is required.` と表示され、DBの `result_submissions` と `test_results` に行は追加されない。 |
| TC-69 | 入力 | テスト対象名 | 前後の空白 | 1. `TC-1` に `Pass` を選択し、`Test target name` に `  testman.exe  `、`Executed by` に `  Tester A  ` を入力する。<br>2. `Save results`、続いて `Confirm selected results` を選択する。<br>3. DBの今回の `result_submissions` 行を参照する。 | `test_target_name` は `testman.exe`、`executed_by` は `Tester A` と保存され、前後の空白は残らない。 |
| TC-70 | 入力 | コメント | 空欄と入力あり | 1. `TC-1` と `TC-2` に `Pass` を選択し、`TC-1` の `Optional comment` に `確認メモ` を入力し、`TC-2` は空欄にする。<br>2. `Test target name` に `testman.exe`、`Executed by` に `Tester A` を入力し、`Save results` を選択する。<br>3. 両ケースの `History (1)` とDBの `comment` を参照する。 | `TC-1` の履歴に `確認メモ` が表示され、DBの `comment` は `確認メモ`。`TC-2` の履歴にはコメントが表示されず、DBの `comment` はNULLまたは空文字列である。 |
| TC-71 | 履歴 | 同一ID | 追記と最新 | 1. `TC-1` を `Pass`、テスト対象を `build-A.exe`、実施者を `Tester A` で登録し、部分登録を承認する。<br>2. 画面上部の `Verification` を選択し、`TC-1` を `Fail`、テスト対象を `build-B.exe`、実施者を `Tester B` で登録し、部分登録を承認する。<br>3. `TC-1` の `History (2)` とDBの2行を参照する。 | `Previous result` のバッジは `Fail`。Historyには `Pass`・`build-A.exe`・`Tester A` と `Fail`・`build-B.exe`・`Tester B` の2件が別に残る。DBの2行の内部IDは異なり、先の行は上書きされていない。 |
| TC-72 | 履歴 | 時刻 | UTCとローカル | 1. OSのローカルタイムゾーンと登録直前のUTC時刻を記録する。<br>2. `TC-1` に `Pass`、対象名と実施者名に既定値を入力し、`Save results` と `Confirm selected results` を選択する。<br>3. DBの `executed_at_utc` と `TC-1` の `History (1)` に表示される日時を比較する。 | DBの `executed_at_utc` は登録前後のUTC時刻の範囲に入るISO 8601 round-trip形式で末尾が `Z`。Historyに表示される日時は、そのUTC時刻を試験環境のローカルタイムゾーンへ変換した値と一致する。 |
| TC-73 | 履歴 | 要約と展開 | 表示項目 | 1. `TC-1` に `Pass`、対象名と実施者名に既定値を入力し、コメントに `確認メモ` を入力して登録し、部分登録を承認する。<br>2. `TC-1` の `Previous result` 列を見てから `History (1)` を開く。 | 展開前の `Previous result` 列には `Pass` バッジと `History (1)` だけがあり、日時・実施者名・テスト対象名・コメントはない。展開後は日時、`Pass`、`確認メモ`、`Tester A`、`testman.exe` が表示され、仕様Git SHAと過去の仕様本文は表示されない。 |
| TC-74 | 履歴 | 仕様SHA | 対象ファイルの最終変更 | 1. `TC-1` に `Pass`、対象名と実施者名に既定値を入力し、登録と部分登録の承認を行う。<br>2. Test listの `specs/run.md` の表示と、DBの `test_results.specification_revision` を照合する。 | Test listのパスの後に丸括弧付きで `<spec-sha>` が表示され、DBの `specification_revision` は同じ `<spec-sha>` である。Historyには `<spec-sha>` は表示されない。 |
| TC-75 | DB | Not Tested | 非保存値 | 1. 画面で `TC-2` が `Not Tested` であることを確認する。<br>2. `TC-1` に `Pass`、対象名と実施者名に既定値を入力し、登録と部分登録の承認を行う。<br>3. DBの `test_results` を参照する。 | DBに `TC-2` の行はなく、`TC-1` の `result` は `pass`。DBに `Not Tested` という `result` 値は存在せず、画面の `TC-2` は `Not Tested` のままである。 |
| TC-76 | 登録 | 全件 | 再送信防止 | 1. `TC-1` と `TC-2` に `Pass` を選択し、対象名と実施者名に既定値を入力する。<br>2. `Save results` を選択し、結果画面でブラウザの再読み込みを行う。<br>3. DBの `result_submissions` と `test_results` の件数を確認する。 | 登録直後は `Results saved.` と表示され、ブラウザの再読み込み後も登録操作は再実行されない。DBの submission は1件、result は `TC-1` と `TC-2` の2件であり、両resultの `submission_id` は同じである。 |

# フォルダ集計とGitによる結果の識別

## Overview

同じTest IDを含む2ファイルをフォルダから読み、ファイルごとの最新結果と未テスト数を確認する。仕様本文の変更・削除、異なるGitリポジトリ、他ファイルの未コミット変更が、現在表示と保存済み履歴へ与える影響も確認する。

## Preconditions

- 試験用Gitリポジトリに `specs/a.md` と `specs/nested/b.md` を作り、両方をコミットする。`a.md` の内容は次のとおりとする。`b.md` は同じ内容で title `仕様A` を `仕様B` に、Overviewの `Aの概要。` を `Bの概要。` に置き換える。各ケースはこのコミット済み状態と新しいDBから開始する。

  ```text
  Testman-Format-Version: 1

  # 仕様A

  ## Overview

  Aの概要。

  ## Preconditions

  なし

  ## Common steps

  行別Stepsに従う。

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | A | - | 第一 | 1. 第一を確認する。 | 第一が表示される。 |
  | TC-2 | A | - | 第二 | 1. 第二を確認する。 | 第二が表示される。 |
  ```

- `<testman>` は実行ファイル、`<port>` は使用可能なポート、`<case-db>` は各ケース専用の新しいSQLite DBの絶対パスとする。`<repo-root>` は試験用Gitリポジトリルートの正規化済み絶対パスとする。testmanをそのルートから `serve --specs ./specs --db <case-db> --port <port>` で起動する。
- 登録の際は、特記がなければ画面下部の `Test target name` に `testman.exe`、`Executed by` に `Tester A` を入力する。ケースを1件だけ選ぶ場合は `Save results` 後の `Not all test cases have a result. Confirm saving only the selected cases.` を確認し、`Confirm selected results` を選択する。ブラウザでは `Test patterns` と `Verification` の表示を使う。

## Common steps

1. ブラウザで `http://localhost:<port>/` を開き、画面上部の `Verification` を選択する。
2. 行別Stepsの入力、保存、仕様ファイルの編集を実施する。
3. 行別Stepsが指定した画面の `Test list`、ファイルごとの `Current test status`、`Previous result`、`History (n)` とDBを比較する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-77 | 初期表示 | Test list | 2ファイル | 1. `Test list` と各ファイルの `Current test status` を確認する。 | Test listには `specs/a.md` と `specs/nested/b.md` の各 `TC-1`・`TC-2` が、Test ID、タイトル、所属ファイル、最新結果 `Not Tested`、最終実施日時 `—` とともに4行表示される。各ファイルの状況は `Total 2`、`Not Tested 2`、4結果区分は各 `0`。 |
| TC-78 | 集計 | ファイル別 | 4結果 | 1. 4ケースの結果入力欄でAの `TC-1=Pass`、`TC-2=Fail`、Bの `TC-1=Blocked`、`TC-2=N/A` を選択する。<br>2. `Test target name` と `Executed by` に既定値を入力し、`Save results` を選択する。<br>3. 両ファイルの `Current test status` を確認する。 | Aは `Total 2`、`Pass 1`、`Fail 1`、`Blocked 0`、`N/A 0`、`Not Tested 0`。Bは `Total 2`、`Pass 0`、`Fail 0`、`Blocked 1`、`N/A 1`、`Not Tested 0`。確認画面を挟まず `Results saved.` が表示される。 |
| TC-79 | 集計 | 最新結果 | 対象名で絞らない | 1. Aの `TC-1=Pass` を選択し、`Test target name` に `build-A.exe`、`Executed by` に `Tester A` を入力して `Save results` と `Confirm selected results` を選択する。<br>2. Aの `TC-1=Fail` を選択し、`Test target name` に `build-B.exe`、`Executed by` に `Tester B` を入力して同じ登録操作を行う。<br>3. Aの `Current test status`、Test listのAの `TC-1`、および `History (2)` を確認する。 | Aの状況は `Total 2`、`Fail 1`、`Not Tested 1`、`Pass 0`。`TC-1` の前回結果は `Fail` で、Historyには異なるテスト対象名の2件が残る。Test listのAの `TC-1` は最新結果 `Fail` と2回目の最終実施日時を表示する。対象名で集計や前回結果が分割されない。 |
| TC-80 | 集計 | 仕様変更 | Test ID削除 | 1. Aの `TC-2=Pass` を既定の対象名・実施者名で登録し、部分登録を承認する。<br>2. `specs/a.md` の `TC-2` 行を削除してコミットし、画面上部の `Verification` を選択する。<br>3. Aの `Current test status` とDBを確認する。 | Aの状況は `Total 1`、`Not Tested 1`、`Pass 0` となる。DBには削除済み `TC-2` の実施結果が残り、その `repository_root`、`source_file`、`test_case_id`、`specification_revision` を照合できる。 |
| TC-81 | 集計 | 不正な行 | 重複ID除外 | 1. `specs/a.md` の `TC-2` を `TC-1` に変更してコミットする。<br>2. 画面上部の `Verification` を選択し、AとBの `Current test status` を確認する。 | 画面上部に `Duplicated ID detected: TC-1` が表示される。Aの `Total 0`、Bの `Total 2` であり、重複行はAの通常集計に含まれない。 |
| TC-82 | 仕様再読込 | 本文変更 | 履歴保持 | 1. Aの `TC-1=Pass` を既定の対象名・実施者名で登録し、部分登録を承認する。<br>2. `specs/a.md` の Overviewを `Aの更新概要。` に変更してコミットし、画面上部の `Verification` を選択する。<br>3. Aの `TC-1` の `History (1)` を開き、DBを確認する。 | 画面には `Aの更新概要。` が表示され、Aの `TC-1` の前回結果は `Pass`。HistoryとDBに変更前の実施結果1件が残り、保存済み `specification_revision` は変更前のコミットSHAのままである。 |
| TC-83 | 仕様再読込 | ファイル削除 | 履歴保持 | 1. Aの `TC-1=Pass` を既定の対象名・実施者名で登録し、部分登録を承認する。<br>2. `specs/a.md` をGitから削除してコミットし、画面上部の `Verification` を選択する。<br>3. Test listとDBを確認する。 | Test listはBの2ケースだけになり、Aの現在状況は表示されない。DBにはAの `TC-1` の結果が残り、`specs/a.md` と保存時のコミットSHAで追跡できる。 |
| TC-84 | Git状態 | 対象仕様 | インデックス変更 | 1. `specs/a.md` の Overviewを `Aのステージ済み概要。` に変更し、`git add specs/a.md` を行うがコミットしない。<br>2. 画面上部の `Verification` を選択する。<br>3. Aの `TC-1=Pass` を既定の対象名・実施者名で登録し、部分登録を承認する。 | 変更後の概要は閲覧できるが、画面に `Results can only be saved for committed, unchanged Git specification files.` と表示され、DBに結果は追加されない。 |
| TC-85 | Git状態 | 別ファイル | 未コミット変更 | 1. `README.md` をリポジトリに作成しコミットする。<br>2. `README.md` を変更して保存し、`specs/a.md` は変更しない。<br>3. Aの `TC-1=Pass` を既定の対象名・実施者名で登録し、部分登録を承認する。 | `Results saved.` と表示され、Aの `TC-1` の前回結果は `Pass`。`README.md` の未コミット変更だけでは登録が拒否されない。 |
| TC-86 | Git状態 | 仕様リビジョン | HEADより前 | 1. `git log -1 --format=%H -- specs/a.md` のSHAを控える。<br>2. `README.md` を作って別コミットにし、HEADを進める。<br>3. 画面上部の `Verification` を選択してTest listのAのパス表示を確認する。<br>4. Aの `TC-1=Pass` を既定の対象名・実施者名で登録し、部分登録を承認してDBを確認する。 | Test listの `specs/a.md` の直後の丸括弧には控えたSHAが表示され、後のREADMEコミットのHEAD SHAは表示されない。DBの `specification_revision` は控えたSHAであり、`Test target name` の値とは別の列に保存される。 |
| TC-87 | Git状態 | Git管理外 | 未追跡ファイル | 1. `specs/c.md` を `specs/a.md` のコピーとして作り、titleを `仕様C` に変更するがGitへ追加しない。<br>2. 画面上部の `Verification` を選択する。<br>3. Cの `TC-1=Pass` を既定の対象名・実施者名で登録し、部分登録を承認する。 | Cのケースは閲覧できるが、画面には `Results can only be saved for committed, unchanged Git specification files.` と表示され、DBにCの結果は追加されない。 |
| TC-88 | Git同一性 | ファイル改名 | 再関連付けなし | 1. Aの `TC-1=Pass` を既定の対象名・実施者名で登録し、部分登録を承認する。<br>2. `git mv specs/a.md specs/c.md` を行ってコミットし、画面上部の `Verification` を選択する。<br>3. `specs/c.md` の `TC-1` とDBを確認する。 | `specs/c.md` の `TC-1` は `Not Tested`。DBの以前の結果は `source_file=specs/a.md` のまま残り、新しいパスの前回結果へ自動的に関連付けられない。 |
| TC-89 | Git同一性 | リポジトリ | 別ルート | 1. 別の試験用Gitリポジトリに同じ相対パス `specs/a.md` と同じ Test ID を持つ仕様をコミットする。<br>2. 元のリポジトリでAの `TC-1=Pass` を既定の対象名・実施者名で登録し、部分登録を承認する。<br>3. 元のtestmanを停止する。<br>4. 同じDBを指定し、別リポジトリからその `specs/a.md` でtestmanを起動して `Verification` を選択する。 | 別リポジトリの `TC-1` は `Not Tested`。DBの元の結果の `repository_root` は元の `<repo-root>` で、別リポジトリのルートとは異なるため、前回結果へ混入しない。 |

# SQLite schemaと一括保存

## Overview

初期migrationで作る3表、制約、索引、再起動時の履歴保持をDBビューアで確認する。保存中に2件目の挿入を失敗させ、一括登録が全件ロールバックされることも確認する。

## Preconditions

- 試験用Gitリポジトリに次の `specs/db.md` を作成してコミットする。各ケースは新しいDBから始め、testmanを `--specs ./specs/db.md --db <case-db> --port <port>` で起動する。`<case-db>` は新しいDBの絶対パス、`<port>` は使用可能なポートとする。

  ```text
  Testman-Format-Version: 1

  # DB確認

  ## Overview

  DB保存を確認する。

  ## Preconditions

  なし

  ## Common steps

  行別Stepsに従う。

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 保存 | - | 第一 | 1. 第一を確認する。 | 第一が表示される。 |
  | TC-2 | 保存 | - | 第二 | 1. 第二を確認する。 | 第二が表示される。 |
  ```

- DBビューアで `<case-db>` の `sqlite_master`、`PRAGMA table_info`、`PRAGMA foreign_key_list`、`PRAGMA index_info` と表の行を参照できるようにする。DBビューアの操作はtestmanの保存処理と別のSQLite接続で行う。

## Common steps

1. testmanを起動してからブラウザで `http://localhost:<port>/` を開き、画面上部の `Verification` を選択する。
2. 行別Stepsの画面操作とDB照合を行う。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-90 | schema | submission | 列と制約 | 1. DBビューアで `PRAGMA table_info(result_submissions)` と `sqlite_master` の同表定義を参照する。 | `id` はINTEGER PRIMARY KEY AUTOINCREMENT、`executed_at_utc`・`executed_by`・`test_target_name` はTEXT NOT NULL。実施者名と対象名にはtrim後の非空CHECKがある。 |
| TC-91 | schema | result | 列と制約 | 1. DBビューアで `PRAGMA table_info(test_results)` と `sqlite_master` の同表定義を参照する。 | `id` はINTEGER PRIMARY KEY AUTOINCREMENT。`submission_id`、`repository_root`、`source_file`、`test_case_id`、`result`、`specification_revision` はNOT NULL、`comment` はNULL可。`result` のCHECKは `pass`、`fail`、`blocked`、`not_applicable` の4値だけを許す。 |
| TC-92 | schema | FK・索引 | 関連と検索 | 1. `PRAGMA foreign_key_list(test_results)` と `PRAGMA index_xinfo(ix_test_results_case_history)`、`PRAGMA index_info(ix_test_results_submission)` を参照する。 | `test_results.submission_id` は `result_submissions.id` を参照し、削除時の動作はCASCADEではない。case_history索引のキー列順は `repository_root, source_file, test_case_id, id` で、最後の `id` の `desc` 値は1。submission索引のキー列は `submission_id` である。 |
| TC-93 | schema | migration | 初回と再起動 | 1. `schema_migrations` の列、件数、version、name、applied_at_utcを記録する。<br>2. `TC-1=Pass` と `TC-2=Fail` を選択し、`Test target name` に `testman.exe`、`Executed by` に `Tester A` を入力して `Save results` を選択する。<br>3. testmanを停止し、同じDBで再起動して `schema_migrations` と両結果を再確認する。 | `schema_migrations` は `version INTEGER PRIMARY KEY`、`name TEXT NOT NULL`、`applied_at_utc TEXT NOT NULL` を持ち、versionは単調増加の1件、適用日時はUTC。再起動後もmigration行は増えず、2件の結果は削除されない。 |
| TC-94 | 保存 | 一括登録 | submission共有 | 1. `TC-1=Pass` と `TC-2=Fail` を選択し、`Test target name` に `testman.exe`、`Executed by` に `Tester A` を入力して `Save results` を選択する。<br>2. DBの2表の今回の行を確認する。 | `result_submissions` に内部IDを持つ1行、`test_results` に異なる内部IDを持つ2行が挿入される。2行の `submission_id` は同じ親ID、`source_file` は `specs/db.md`、`test_case_id` はそれぞれ `TC-1` と `TC-2`、`test_target_name` は親の `testman.exe` である。 |
| TC-95 | 保存 | 失敗 | 全件ロールバック | 1. DBビューアで `CREATE TRIGGER reject_tc2 BEFORE INSERT ON test_results WHEN NEW.test_case_id='TC-2' BEGIN SELECT RAISE(ABORT, 'forced failure'); END;` を実行する。<br>2. 画面で `TC-1=Pass` と `TC-2=Fail` を選択し、`Test target name` に `testman.exe`、`Executed by` に `Tester A` を入力して `Save results` を選択する。<br>3. DBの2表を確認する。 | 画面に `Results could not be saved because the database is unavailable.` と表示され、`Results saved.` と内部例外・スタックトレースは表示されない。DBの `result_submissions` と `test_results` はともに0行で、1件目だけが残る状態にならない。 |
| TC-96 | DB内容 | 仕様本文 | 非保存 | 1. DBの `sqlite_master` にある表名と各表の列名を確認する。<br>2. `specs/db.md` のOverviewを `新しいDB確認。` に変更してコミットし、画面上部の `Verification` を選択する。<br>3. DBの表と画面のOverviewを再確認する。 | 画面には `新しいDB確認。` が表示される。DBには `schema_migrations`、`result_submissions`、`test_results` の製品表があり、仕様本文または集計スナップショットを保存する製品表・列はない。 |
| TC-97 | Git管理 | 生成物 | DB除外 | 1. リポジトリの `.gitignore`、`migrations/001_create_result_history.sql`、`git ls-files` を確認する。<br>2. testmanのDBをリポジトリ内の `./.testman/testman.db` に指定して起動し、`git status --short` を確認する。 | migrationのSQLはGit追跡対象で、実行時DBは作成されてもGitの追跡候補へ表示されない。`.gitignore` は `.db` とSQLiteのsidecarファイルを除外する。 |
| TC-98 | 製品操作 | 履歴 | 更新・削除なし | 1. `TC-1=Pass` を対象名・実施者名の既定値で登録し、部分登録を承認する。<br>2. `Verification` の画面とCLIの利用コマンドを確認する。<br>3. `TC-1=Fail` を同じ方法で登録し、DBの履歴を確認する。 | Web画面とCLIに保存済みresultを更新または削除する製品操作はなく、DBには`Pass`と`Fail`に対応する別々の2行が残る。 |

# 起動構成と配布成果物

## Overview

localhostへの待受、サーバー側の画面生成、仕様本文の直接読込、配布ZIP、自動テストを成果物と実行結果で確認する。配布ZIPの実行は.NETランタイムを事前導入していないWindows x64環境を使用する。

## Preconditions

- Git管理済みの有効な仕様ファイル `specs/release.md` を試験用リポジトリに用意する。内容は次のとおりとし、testmanの実行ファイルと新しいDBを指定して起動できる状態にする。

  ```text
  Testman-Format-Version: 1

  # 配布確認

  ## Overview

  配布された画面を確認する。

  ## Preconditions

  なし

  ## Common steps

  なし

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 配布 | - | - | - | 配布確認が表示される。 |
  ```

- `<port>` は試験用PCで使用可能なポートとし、リポジトリのソース、テストプロジェクト、配布ZIPを参照できるようにする。ZIPを実行するケースでは、配布物以外のtestmanファイルを試験用PCへコピーしない。

## Common steps

1. 行別Stepsに記したコマンドまたは成果物の確認を実施する。
2. サーバーを起動した場合は `http://localhost:<port>/` を開いて結果を確認し、終了後に試験用DBだけを保持する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-99 | Web構成 | HTML生成 | Razor Pages | 1. `src/Testman.Web/Testman.Web.csproj` と `src/Testman.Web/Pages/Index.cshtml` を参照する。<br>2. testmanを `serve --specs ./specs/release.md --db <case-db> --port <port>` で起動し、ブラウザのページソースを表示する。 | WebプロジェクトはASP.NET Core Web SDKとRazor Pagesの `.cshtml` を使用し、HTTP応答のHTMLに `配布確認` とケース表が含まれる。 |
| TC-100 | 待受 | localhost | 外部非公開 | 1. testmanを `serve --specs ./specs/release.md --db <case-db> --port <port>` で起動する。<br>2. OSのTCP待受一覧で `<port>` のLocal Addressを調べる。<br>3. 同じPCのブラウザで画面を開く。 | `<port>` の待受はloopbackアドレスだけで、`0.0.0.0` やPCのLANアドレスでは待ち受けない。同じPCのブラウザでは `配布確認` を閲覧できる。 |
| TC-101 | 配布物 | Windows x64 | 自己完結ZIP | 1. `dotnet publish src/Testman.Web/Testman.Web.csproj -c Release -p:CreateDistributionArchive=true` を実行する。<br>2. 生成された `artifacts/testman-win-x64.zip` と試験用の `specs/release.md` を.NETランタイム未導入のWindows x64 PCへ移し、ZIPを展開する。<br>3. 展開された `testman.exe` で `serve --specs <absolute-release-md> --db <case-db> --port <port>` を実行する。`<absolute-release-md>` は移した `release.md` の絶対パスとする。 | ZIPにWindows x64用の `testman.exe` と実行に必要な自己完結型ファイルが含まれ、対象PCへ.NETランタイムを追加せず `配布確認` の画面が開く。 |
| TC-102 | 配布物 | サテライト言語 | 許可・除外 | 1. `dotnet publish src/Testman.Web/Testman.Web.csproj -c Release -p:CreateDistributionArchive=true` を実行する。<br>2. 生成された `artifacts/testman-win-x64.zip` の `*.resources.dll` を格納した言語ディレクトリ名をすべて列挙する。 | 存在するサテライトリソースの言語ディレクトリ名は `en`、`ja`、`zh-CN` のいずれかであり、`zh-Hant`、`zh-TW`、`zh-HK` とその他の言語ディレクトリはない。 |
| TC-103 | CLI機能 | import・設定ファイル | 対象外 | 1. `<testman> import --specs ./specs/release.md` を実行する。<br>2. `<testman> serve --specs ./specs/release.md --config ./config.json` を実行する。<br>3. 有効な `serve` で起動し、仕様のOverviewをファイル上だけで変更して画面を再読み込みする。 | 1と2の標準エラー出力はそれぞれ `Usage: testman serve --specs <path> [--db <path>] [--port <1-65535>]`、終了コードは `1`。有効なserveの画面には変更後のOverviewが表示され、DBに仕様本文のimportはない。 |
| TC-104 | 自動テスト | 主要領域 | 実行 | 1. リポジトリの `tests/` 内に仕様解析、結果保存、Web操作を検証するテストケースがあることを確認する。<br>2. リポジトリルートで `dotnet test` を実行する。 | 3領域の自動テストがテストプロジェクトに存在し、`dotnet test` の終了コードは `0`。 |

# 表示と解析の追加境界

## Overview

検証表の配置、分類セルの結合、ファイル単位の解析継続、および残る入力境界を確認する。

## Preconditions

- 各ケースは専用の試験用Gitリポジトリと新しいDBで実行する。次の `specs/a.md` をUTF-8で保存し、変更後にコミットする。`<case-db>` はケース専用DBの絶対パス、`<port>` は使用可能なポートに置き換える。

  ```text
  Testman-Format-Version: 1

  # 配置確認

  ## Overview

  配置確認の概要。

  ## Preconditions

  なし

  ## Common steps

  行別Stepsに従う。

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | A | B | X | 1. 確認する。 | 表示する。 |
  | TC-2 | A | B | Y | 1. 確認する。 | 表示する。 |
  | TC-3 | A | C | - | 1. 確認する。 | 表示する。 |
  | TC-4 | - | - | - | 1. 確認する。 | 表示する。 |
  | TC-5 | - | - | - | 1. 確認する。 | 表示する。 |
  ```

- TC-105、TC-106、TC-108は基準ファイルを変更せずに起動する。TC-107とTC-109以降は行別Stepsで編集した後に起動する。起動コマンドは `<testman> serve --specs ./specs/a.md --db <case-db> --port <port>` とする。検証表は `Verification`、分類表は `Test patterns` を選択して観察する。

## Common steps

1. 行別Stepsの編集または操作を行う。
2. 必要な場合は上記コマンドでtestmanを起動し、ブラウザで `http://localhost:<port>/` を開く。
3. 行別Stepsで指定した画面・診断・ファイルを確認する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-105 | Web | 仕様編集 | 操作なし | 1. `Test patterns` と `Verification` の両画面を開き、仕様本文に対する操作を確認する。 | 両画面に仕様Markdownの編集・保存操作はなく、ディスク上の `specs/a.md` は画面操作で変更されない。 |
| TC-106 | Web | 検証表 | 列順と幅 | 1. `Verification` を選択し、ブラウザの開発者ツールでケース表の列順と各列幅を調べる。 | ケース表の列は `Test ID`、`Steps`、`Expected result`、`Previous result`、`Result input`、`Optional comment` の順で、幅はそれぞれ6%、28%、26%、12%、12%、16%。別のtitleの文章量で割合は変わらない。 |
| TC-107 | Web | 検証表 | 狭い画面 | 1. `specs/a.md` のTC-1のExpected resultを `https://example.invalid/abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789` に変更してコミットする。<br>2. `Verification` を選択してブラウザの表示幅を320 CSS pxにし、ページを再読み込みする。<br>3. ケース表と結果入力欄を確認する。 | 指定した長いURLはセル内で折り返され、ケース表は横スクロールできる。結果入力欄は操作できる最小幅を保ち、画面外へ切れて使えなくならない。 |
| TC-108 | Web | 分類表 | 結合と予約値 | 1. `Test patterns` を選択し、分類表のDOMで各分類セルのrowspanを確認する。 | `A` はTC-1からTC-3の3行で、`B` はTC-1とTC-2の2行でそれぞれ結合される。TC-4とTC-5の `-` は各行に別セルとして表示され、結合されない。 |
| TC-109 | 解析 | 必須節 | 空のOverview | 1. `specs/a.md` の `配置確認の概要。` を削除してOverview本文を空にする。 | 上部の診断に `specs/a.md` の絶対パス、title見出しの行番号、`Required section 'Overview' must not be empty.` が表示され、`配置確認` のケースは表示されない。 |
| TC-110 | 解析 | 必須節 | 空のCommon steps | 1. `specs/a.md` の `行別Stepsに従う。` を削除してCommon steps本文を空にする。 | 上部の診断に `specs/a.md` の絶対パス、title見出しの行番号、`Required section 'Common steps' must not be empty.` が表示され、`配置確認` のケースは表示されない。 |
| TC-111 | 解析 | 必須セル | 分類空欄 | 1. `TC-1` の Major itemを空セルにする。 | 上部の診断に `specs/a.md` の絶対パス、`TC-1` の行番号、`Required cell 'Major item' must not be empty.` が表示され、TC-1の結果登録欄は使えない。 |
| TC-112 | 解析 | ID境界 | ゼロ | 1. `TC-1` のIDを `TC-0` に変更する。 | 上部の診断に `specs/a.md` の絶対パス、変更した行番号、`Invalid Test ID: TC-0` が表示され、その行の結果登録欄は使えない。 |
| TC-113 | 解析 | version境界 | ゼロ | 1. 先頭行を `Testman-Format-Version: 0` に変更する。 | 上部の診断に `specs/a.md` の絶対パス、行番号 `1`、`Format version must be a positive integer.` が表示され、そのファイルの結果登録欄は使えない。 |
| TC-114 | 解析 | 複数ファイル | 片方が不正UTF-8 | 1. 基準内容の `specs/a.md` を `specs/b.md` にコピーし、Bのtitleを `正常なB` に変更する。<br>2. `specs/a.md` の末尾にバイト `FF` を追加し、両ファイルをコミットする。<br>3. `--specs ./specs` でtestmanを起動し、画面上部の `Test patterns` と `Verification` を選択する。 | Webサーバーは起動する。画面上部に `specs/a.md` の絶対パスと `Specification file is not valid UTF-8.` が表示され、Aのケースは表示されない。`正常なB` の5ケースは `Test patterns` と `Verification` で閲覧できる。 |
| TC-115 | Git | 作業ツリー | 未ステージ変更 | 1. `specs/a.md` のOverviewを `未ステージの概要。` に変更し、Gitにステージしない。<br>2. `Verification` でTC-1を `Pass`、`Test target name` を `testman.exe`、`Executed by` を `Tester A` にして `Save results` と `Confirm selected results` を選択する。 | 変更後の概要は閲覧できる。画面に `Results can only be saved for committed, unchanged Git specification files.` と表示され、DBに結果は追加されない。 |

# テスト仕様のIDと記述規則

## Overview

テスト仕様を編集するときのID継続、ケースの意図、共通手順の適用をリポジトリ上のファイルとGit履歴で確認する。

## Preconditions

- 各ケースで専用の試験用Gitリポジトリを作り、`specs/author.md` を次の内容でコミットする。`git log --all -- <path>` でそのファイルの履歴を確認できるようにする。ここでの対象はtestmanの実行時バリデーションではなく、テスト仕様を作成・改訂するときの記述規則である。

  ```text
  Testman-Format-Version: 1

  # 入力確認

  ## Overview

  入力の種類と結果をレビューする。

  ## Preconditions

  入力欄を表示する。

  ## Common steps

  1. 入力欄を開く。
  2. 行別Stepsを実行する。
  3. 入力結果を確認する。

  | ID | Major item | Middle item | Minor item | Steps | Expected result |
  |---|---|---|---|---|---|
  | TC-1 | 入力 | 空欄 | - | 1. 空欄のまま送信する。 | 必須入力の表示が出る。 |
  | TC-2 | 入力 | 値あり | - | 1. `abc` を入力して送信する。 | `abc` が表示される。 |
  ```

## Common steps

1. 行別Stepsに従い `specs/author.md` とGit履歴を編集または確認する。
2. 完成したファイルを読み、各行のID、共通手順、行別手順、Expected resultを照合する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-116 | ID | 追加 | 最大番号の次 | 1. TC-2を削除してコミットする。<br>2. 新しい「空白のみ入力」のケースを追加してコミットする。<br>3. Git履歴と現行ファイルのIDを照合する。 | 新ケースのIDは `TC-3` であり、削除済み `TC-2` は再利用されない。現行ファイル内のIDは一意である。 |
| TC-117 | ID | 改訂 | 同じ意図 | 1. TC-1の操作説明をより具体的な表現に直し、空欄送信を調べる意図を保ってコミットする。<br>2. 変更前後の行を照合する。 | 空欄送信を調べるケースのIDは変更前後とも `TC-1` であり、Expected resultは同じ意図の観察だけを扱う。 |
| TC-118 | ID | ファイル名 | 改名 | 1. `git mv specs/author.md specs/renamed.md` を実行してコミットする。<br>2. 改名前後のケース行を照合する。 | 移動先 `specs/renamed.md` の空欄送信と値あり入力のIDはそれぞれ `TC-1`、`TC-2` のままである。 |
| TC-119 | ID | ファイル間 | ケース移動 | 1. 同じ形式・必須節・表を持ち、最後のIDが `TC-5` の有効な仕様 `specs/other.md` を作ってコミットする。<br>2. `specs/author.md` のTC-2を削除し、値あり入力のケースを `specs/other.md` へ新しいIDで追加してコミットする。<br>3. 移動先のGit履歴と現行の両ファイルのIDを照合する。 | `specs/author.md` のTC-2は削除扱いで、その番号は移動元で再利用されない。移動先の値あり入力の行は `TC-6` であり、移動先の既存IDとも重複しない。 |
| TC-120 | 記述 | 共通手順 | 全ケース | 1. TC-1とTC-2の共通手順と行別Stepsを読み、各行の操作順を組み立てる。 | 両ケースとも「入力欄を開く」から始まり、それぞれの入力・送信の後に「入力結果を確認する」が続く。2ケースは別の入力条件という別々の意図を持ち、行別Stepsの長さによってIDを分割しない。 |
| TC-121 | 記述 | Expected result | 単一意図 | 1. TC-2のExpected resultを `送信後にabcが表示される。再表示後もabcが表示される。` に変更し、既存の行別Stepsの後へ2番目の操作 `2. 画面を再表示する。` を加えてコミットする。<br>2. TC-2の操作と期待内容を照合する。 | TC-2のIDはそのままで、Expected resultの2文はいずれも値 `abc` を入力する同一意図に対応する。空欄送信など別の入力条件はTC-2に混在しない。 |

# 保存と診断の補足

## Overview

外部キー、Git同一性、複数の解析診断、未知の形式バージョン、結果画面の入力欄を確認する。

## Preconditions

- 各ケースは専用の試験用Gitリポジトリと新しいDBを使う。有効な仕様 `specs/check.md` は `Testman-Format-Version: 1`、title `補足確認`、空でないOverview、Preconditions、Common steps、6列のテストケース表を持ち、`TC-1` と `TC-2` の2行を含む。`<case-db>` はケース専用DBの絶対パス、`<port>` は使用可能なポートに置き換える。
- ケースでファイルを編集した場合は指示がない限りコミットし、`<testman> serve --specs ./specs/check.md --db <case-db> --port <port>` で起動する。DBの検査にはSQLiteビューアを使用する。

## Common steps

1. 行別Stepsに従って試験環境を準備する。
2. 必要に応じて `Verification` を選択し、行別Stepsで指定した画面、診断またはDBを確認する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-122 | DB | 外部キー | 存在しない親 | 1. testmanを起動して初期migrationを適用する。<br>2. SQLiteビューアで `PRAGMA foreign_keys=ON` を実行する。<br>3. `INSERT INTO test_results(submission_id,repository_root,source_file,test_case_id,result,specification_revision) VALUES(999999,'C:/trial','specs/check.md','TC-1','pass','0000000000000000000000000000000000000000');` を実行する。 | 挿入はSQLiteの外部キー制約違反（拡張結果コード787）として拒否され、DBにその行は残らない。`test_results.submission_id` は実在する `result_submissions.id` を必要とする。 |
| TC-123 | Git | リポジトリ移動 | 再関連付けなし | 1. 元の場所で `specs/check.md` をコミットし、`TC-1=Pass`、`Test target name=testman.exe`、`Executed by=Tester A` を選び `Save results` と `Confirm selected results` を選択する。<br>2. testmanを停止し、Gitリポジトリのフォルダ全体を別の絶対パスへ移す。<br>3. 同じDBを使い移動先の `specs/check.md` でtestmanを起動し、`Verification` とDBを確認する。 | 移動先の `TC-1` は `Not Tested`。既存行の `repository_root` は移動前の正規化済み絶対パスのままであり、新しいルートへ自動関連付けされない。 |
| TC-124 | 解析 | 重複ID | 2種類 | 1. `specs/check.md` の表に、IDが `TC-1` と `TC-2` の行をそれぞれもう1行ずつ追加してコミットする。<br>2. testmanを起動して画面上部の診断と結果入力欄を確認する。 | 診断に `Duplicated ID detected: TC-1, TC-2` が表示され、両IDの重複行は結果登録できない。 |
| TC-125 | 解析 | 未知version | 構造エラー | 1. 先頭行を `Testman-Format-Version: 7` に変更し、表の `Expected result` 列名を `Expected` に変更してコミットする。<br>2. testmanを起動し、画面と診断を確認する。 | `Format version 7` が表示される。診断には `Test case table columns do not match the approved names and order.` が表示され、未知version専用の移行や別の解析規則は適用されない。 |
| TC-126 | 解析 | 必須セル | ID・分類・期待結果 | 1. `specs/check.md` の1行目について、ID、Major item、Middle item、Minor item、Expected resultの各セルを1つずつ空にした5つのコピーを別の試験用ファイルとして作る。<br>2. それぞれを単一ファイル指定で起動し、診断と該当行の結果入力欄を確認する。 | 各ファイルの診断にはその絶対パス、空セルの行番号、および列順に `Required cell 'ID' must not be empty.`、`Required cell 'Major item' must not be empty.`、`Required cell 'Middle item' must not be empty.`、`Required cell 'Minor item' must not be empty.`、`Required cell 'Expected result' must not be empty.` のうち該当する1文が表示される。該当行は結果登録できない。 |
| TC-127 | Web | 登録欄 | 対象と実施者 | 1. testmanを起動し、`Verification` の結果登録欄を確認する。<br>2. `TC-1=Pass`、`Test target name=testman.exe`、`Executed by=Tester A` を入力し、`Save results` と `Confirm selected results` を選択する。<br>3. DBの今回のsubmissionとresultの列を確認する。 | 画面には共通の `Test target name` と `Executed by` の入力欄があり、対象ソフト用のGit SHA・バージョン・ビルド番号・成果物IDの専用欄はない。DBではテスト対象名は `test_target_name`、仕様MarkdownのSHAは別の `specification_revision` に保存される。 |
