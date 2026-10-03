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

- testman をこの `result.md` と各ケース専用の新しい DB で起動する。検証実施者名には、特記がなければ `Tester A` を使う。コメントが必要な場合は `確認メモ` を使う。
- 登録前のリポジトリの HEAD commit SHA を控える。DB の内容を照合するケースでは SQLite を参照できる手段を用意する。

## Common steps

1. ブラウザで testman のルートURLを開き、画面上部の `Verification` を選択する。`結果確認` の title を画面内で探す。
2. 行別 Steps に従って結果と検証実施者名を入力し、指定されたボタンを選択する。
3. 登録後の画面、ケースごとの前回結果と履歴、必要に応じて試験用 DB を確認する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-8 | 初期表示 | 結果入力 | 未選択 | 1. 何も入力せず、`TC-1` と `TC-2` の前回結果と結果入力欄を見る。 | 両ケースの前回結果は `Not Tested` と表示され、両ケースの結果入力欄は未選択である。 |
| TC-9 | 登録 | 全件選択 | Pass と Fail | 1. `TC-1` の結果入力欄で `Pass`、`TC-2` の結果入力欄で `Fail` を選択する。<br>2. 画面下部の `Executed by` に `Tester A` を入力する。<br>3. `Save results` を選択する。 | 確認操作を挟まず、登録後の画面に `Results saved.` と表示される。`TC-1` の前回結果は `Pass`、`TC-2` は `Fail` となり、両ケースの履歴に今回の実施者名、結果、控えた Git commit SHA が表示される。 |
| TC-10 | 登録 | 一部選択 | 確認後に登録 | 1. `TC-1` の結果入力欄で `Blocked` を選択し、`TC-2` は未選択のままにする。<br>2. 画面下部の `Executed by` に `Tester A` を入力する。<br>3. `Save results` を選択する。<br>4. 確認文の下に表示される `Confirm selected results` を選択する。 | 登録前に `Not all test cases have a result. Confirm saving only the selected cases.` と表示される。承認後は `TC-1` のみ新しい履歴が1件増え、`TC-2` の前回結果は `Not Tested` のままで履歴は増えない。 |
| TC-11 | 登録 | 一部選択 | 確認を中止 | 1. `TC-1` の結果入力欄で `N/A` を選択し、`TC-2` は未選択のままにする。<br>2. 画面下部の `Executed by` に `Tester A` を入力する。<br>3. `Save results` を選択する。<br>4. `Confirm selected results` を選択せず、画面上部の `Verification` を選択する。 | 登録前に `Not all test cases have a result. Confirm saving only the selected cases.` と表示される。中止後、`TC-1` と `TC-2` の履歴はいずれも増えず、前回結果はいずれも `Not Tested` のままである。 |
| TC-12 | 登録 | 全件未選択 | 入力エラー | 1. 画面下部の `Executed by` に `Tester A` を入力し、両ケースの結果入力欄は未選択のままにする。<br>2. `Save results` を選択する。 | 登録操作後の画面に `Select a result for at least one test case.` と表示され、両ケースの履歴は増えない。 |
| TC-13 | 登録 | 実施者名 | 空白のみ | 1. `TC-1` の結果入力欄で `Pass` を選択する。<br>2. 画面下部の `Executed by` に半角スペース3文字だけを入力する。<br>3. `Save results` を選択する。 | 登録操作後の画面に `Executor name is required.` と表示され、両ケースの履歴は増えない。 |
| TC-14 | 登録 | コメント | 任意入力 | 1. `TC-1` と `TC-2` の結果入力欄で `Pass` を選択する。<br>2. `TC-1` のコメント欄に `確認メモ` を入力し、`TC-2` のコメント欄は空欄にする。<br>3. 画面下部の `Executed by` に `Tester A` を入力する。<br>4. `Save results` を選択する。 | `TC-1` の履歴には `確認メモ` が表示され、`TC-2` の履歴にはコメントが表示されない。両ケースに今回の `Pass` の履歴が1件ずつ追加される。 |
| TC-15 | 履歴 | 同一ケース | 追記 | 1. 画面下部の `Executed by` に `Tester A` を入力し、`TC-1` の結果入力欄で `Pass` を選択する。<br>2. `Save results` を選択し、確認文の下の `Confirm selected results` を選択する。<br>3. 画面上部の `Verification` を選択し、画面下部の `Executed by` に `Tester A` を入力して `TC-1` の結果入力欄で `Fail` を選択する。<br>4. `Save results` を選択し、確認文の下の `Confirm selected results` を選択する。 | 2回目の登録後、`TC-1` の前回結果は `Fail` である。`TC-1` の履歴には1回目の `Pass` と2回目の `Fail` が別の実施記録として残る。 |
| TC-16 | 履歴 | 再表示 | 入力初期値 | 1. 画面下部の `Executed by` に `Tester A` を入力し、`TC-1` の結果入力欄で `Pass` を選択する。<br>2. `Save results` を選択し、確認文の下の `Confirm selected results` を選択する。<br>3. 画面上部の `Verification` を選択する。 | `TC-1` の前回結果は `Pass` と表示される。`TC-1` と `TC-2` の結果入力欄はどちらも未選択である。 |
| TC-17 | Git 管理 | 仕様ファイル | 未コミット変更 | 1. `result.md` の `2ケースの結果登録を確認する。` を `変更中` に変更して保存する。<br>2. 画面上部の `Verification` を選択する。<br>3. 画面下部の `Executed by` に `Tester A` を入力し、`TC-1` の結果入力欄で `Pass` を選択する。<br>4. `Save results` を選択する。<br>5. `Not all test cases have a result. Confirm saving only the selected cases.` と表示されたら、確認文の下の `Confirm selected results` を選択する。 | 変更後の仕様は閲覧できる。承認後の画面に `Results can only be saved for committed, unchanged Git specification files.` と表示され、`TC-1` と `TC-2` の履歴は増えない。 |

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

- 試験用リポジトリのルートをカレントディレクトリとして、`<testman> serve --specs ./specs --db <db-path> --port <port>` で起動する。`<testman>` は用意した実行ファイル、`<db-path>` は各ケース専用の新しいリポジトリ外の DB の絶対パス、`<port>` は利用可能なポートに置き換える。登録前の HEAD commit SHA を控える。

## Common steps

1. ブラウザで testman のルートURLを開く。
2. 行別 Steps に従って、指定した仕様ファイルの閲覧と結果登録を行う。
3. 登録後の検証実施表示と、試験用 DB の今回の結果を確認する。

| ID | Major item | Middle item | Minor item | Steps | Expected result |
|---|---|---|---|---|---|
| TC-18 | 複数ファイル | 仕様一 | 閲覧と登録 | 1. 画面上部の `Test patterns` を選択し、`one.md` の `仕様一` と分類 `仕様一` を確認する。<br>2. 画面上部の `Verification` を選択し、`one.md` の `TC-1`、Steps `1. 仕様一を確認する。`、Expected result `仕様一が表示される。` を確認する。<br>3. `one.md` の `TC-1` の結果入力欄で `Pass` を選択し、`two.md` の `TC-1` は未選択のままにする。<br>4. 画面下部の `Executed by` に `Tester A` を入力し、`Save results` を選択する。<br>5. `Not all test cases have a result. Confirm saving only the selected cases.` と表示されたら、確認文の下の `Confirm selected results` を選択する。 | `one.md` の指定内容が両表示で確認でき、登録後に `Results saved.` と表示される。`one.md` の `TC-1` の前回結果は `Pass` となり、履歴に `Tester A` と控えた Git commit SHA が表示される。`two.md` の `TC-1` の前回結果は `Not Tested` のままである。DB の今回の結果の `source_file` は `specs/one.md` である。 |
| TC-19 | 複数ファイル | 仕様二 | 閲覧と登録 | 1. 画面上部の `Test patterns` を選択し、`two.md` の `仕様二` と分類 `仕様二` を確認する。<br>2. 画面上部の `Verification` を選択し、`two.md` の `TC-1`、Steps `1. 仕様二を確認する。`、Expected result `仕様二が表示される。` を確認する。<br>3. `two.md` の `TC-1` の結果入力欄で `Pass` を選択し、`one.md` の `TC-1` は未選択のままにする。<br>4. 画面下部の `Executed by` に `Tester A` を入力し、`Save results` を選択する。<br>5. `Not all test cases have a result. Confirm saving only the selected cases.` と表示されたら、確認文の下の `Confirm selected results` を選択する。 | `two.md` の指定内容が両表示で確認でき、登録後に `Results saved.` と表示される。`two.md` の `TC-1` の前回結果は `Pass` となり、履歴に `Tester A` と控えた Git commit SHA が表示される。`one.md` の `TC-1` の前回結果は `Not Tested` のままである。DB の今回の結果の `source_file` は `specs/nested/two.md` である。 |
