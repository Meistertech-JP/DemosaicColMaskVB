# Demosaic colMask VB

[English README](README.md)

[trimmerpop/Demosaic_colMask](https://github.com/trimmerpop/Demosaic_colMask) に着想を得た、VB.NET / WinForms による独立実装です。Unity の asset ファイルから Shader オブジェクトを探し、選択した Shader の `colMask` を `0` にしてモザイク系描画を無効化するためのツールです。

厳密な意味での GitHub fork ではありません。元プロジェクトは Python/UnityPy 製ですが、この版は VB.NET で作り直した別実装で、Unity asset の元構造を壊しにくい保存方式を重視しています。

![メイン画面](docs/images/main-window.png)

## 主な特徴

- Unity asset フォルダまたはファイルをスキャンする WinForms GUI。
- タイトルバーにバージョンを表示し、スクリーンショットや報告から使用ビルドを確認できます。
- `mos`, `moz`, `masi`, `maz`, `censor`, `ピクセル`, `モザイク` など、設定可能なモザイクShader名キーワードで自動選択。
- `pixel` は誤検出の可能性があるため自動選択せず、薄いオレンジ色で「要確認」として表示。
- シェーダー一覧の列ソートと、置換元候補の推奨ハイライト。
- 書き込み前に、対象ファイル、PathID、バックアップ有無、予定される保存方式を確認する Dry Run 表示。
- loose `.assets` ファイルでは Unity バージョンを問わず raw byte patch を優先し、ファイル長、オブジェクト配置、メタデータ、パディング、未知バイトを可能な限り保持。
- `.bak` バックアップ対応と、`.bak` から元ファイルへ戻す復元ボタン。
- Texture2D / Sprite / Material 名から、2D・画像焼き込み型モザイクの可能性を診断。
- UI言語: 英語、日本語、繁体字中国語、簡体字中国語、韓国語。
- APKの再パック/署名処理もありますが、主な検証対象は loose `.assets` ファイルです。

## raw patch を優先する理由

Unity 6000 かつ TypeTreeなしの asset などでは、ツールで asset 全体を読み直して保存するだけで、元のパディングや未知の末尾バイトが落ち、ゲームが起動しなくなることがあります。

`colMask` の変更は、多くの場合「同じサイズの数値変更」だけで済みます。そのため、このツールでは asset ファイル全体を再シリアライズせず、元のShaderオブジェクト内の該当バイトだけを書き換える raw patch を優先します。

raw patch の安全判定に失敗した場合、元ファイルは上書きしません。

## 必要環境

- Windows
- .NET 8 SDK または runtime
- 実行ファイル横、またはプロジェクト内の `classdata.tpk`

APKモードを使う場合:

- `PATH` 上の Java
- 実行ファイル横の `uber-apk-signer*.jar`

## ビルド

```powershell
dotnet build --no-restore
```

デバッグ実行ファイルは次に生成されます。

```text
bin\Debug\net8.0-windows\DemosaicColMaskVB.exe
```

リリース用パッケージはローカルでも作成できます。

```powershell
dotnet publish -c Release -o .\artifacts\framework-dependent
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o .\artifacts\self-contained
```

GitHub Actions でも、`main` へのpush時に両方のパッケージをビルドし、`v*` タグではRelease assetを自動作成します。

## パッケージ

- `windows-framework-dependent.zip`: 小さめです。.NET 8 Windows Desktop Runtime が必要です。
- `windows-x64-self-contained.zip`: 大きめですが、Windows x64 向け .NET runtime を同梱します。

## 使い方

1. `DemosaicColMaskVB.exe` を起動します。
2. Unity asset フォルダをドラッグ&ドロップするか、パス欄をダブルクリックして選択します。
3. 検出された Shader 一覧を確認します。
4. 必要なら **キーワード** で自動選択キーワードを調整します。
5. 必要なら **診断** をクリックし、Texture2D / Sprite / Material 名から焼き込み型・2Dモザイクの可能性を確認します。
6. 処理したい Shader だけを Selected 側へ移動します。
7. **モザイク除去開始** をクリックし、Dry Run 確認で対象ファイルとPathIDが正しいことを確認してから続行します。
8. ゲーム起動と描画を確認するまで `.bak` は残してください。
9. 戻したい場合は **復元** で `.bak` を元ファイルへコピーできます。

## 注意

- 必ずコピー環境で試すか、バックアップを残してください。
- 書き込み前のDry Run確認を必ず確認してください。ファイル変更前の最後のアプリ内チェックポイントです。
- loose `.assets` では可能な限り raw patch を使います。
- bundle / APK は、asset自体だけでなく、圧縮、署名、コンテナ構造の変化で壊れる可能性があります。
- Shader置換はサイズや構造変更が必要になることがあり、`colMask` 編集より危険です。
- 2Dゲームや画像に焼き込まれたモザイクは、見た目が変わらない場合があります。このツールは Shader の `colMask` を編集するもので、Texture / Sprite / 画像素材そのものに焼き込まれた表現は除去できません。
- 復元は `.bak` から元ファイルへ上書きしますが、`.bak` 自体は削除しません。
- このツールはローカル調査・互換性確認向けです。法令やコンテンツルールを守って責任ある範囲で使用してください。

## クレジット

Unity Shader の `colMask` を編集する Python/UnityPy ツール [trimmerpop/Demosaic_colMask](https://github.com/trimmerpop/Demosaic_colMask) に着想を得ています。
