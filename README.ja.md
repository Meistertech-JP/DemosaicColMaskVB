# Demosaic colMask VB

[English README](README.md)

[trimmerpop/Demosaic_colMask](https://github.com/trimmerpop/Demosaic_colMask) に着想を得た、VB.NET / WinForms による独立実装です。Unity の asset ファイルから Shader オブジェクトを探し、選択した Shader の `colMask` を `0` にしてモザイク系描画を無効化するためのツールです。

厳密な意味での GitHub fork ではありません。元プロジェクトは Python/UnityPy 製ですが、この版は VB.NET で作り直した別実装で、Unity asset の元構造を壊しにくい保存方式を重視しています。

![メイン画面](docs/images/main-window.png)

## 主な特徴

- Unity asset フォルダまたはファイルをスキャンする WinForms GUI。
- `mos`, `moz`, `masi`, `maz`, `censor`, `ピクセル`, `モザイク` などの一般的なモザイクShader名を自動選択。
- `pixel` は誤検出の可能性があるため自動選択せず、薄いオレンジ色で「要確認」として表示。
- loose `.assets` ファイルでは Unity バージョンを問わず raw byte patch を優先し、ファイル長、オブジェクト配置、メタデータ、パディング、未知バイトを可能な限り保持。
- `.bak` バックアップ対応。
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

## 使い方

1. `DemosaicColMaskVB.exe` を起動します。
2. Unity asset フォルダをドラッグ&ドロップするか、パス欄をダブルクリックして選択します。
3. 検出された Shader 一覧を確認します。
4. 処理したい Shader だけを Selected 側へ移動します。
5. **Start Demosaic** をクリックします。
6. ゲーム起動と描画を確認するまで `.bak` は残してください。

## 注意

- 必ずコピー環境で試すか、バックアップを残してください。
- loose `.assets` では可能な限り raw patch を使います。
- bundle / APK は、asset自体だけでなく、圧縮、署名、コンテナ構造の変化で壊れる可能性があります。
- Shader置換はサイズや構造変更が必要になることがあり、`colMask` 編集より危険です。
- このツールはローカル調査・互換性確認向けです。法令やコンテンツルールを守って責任ある範囲で使用してください。

## クレジット

Unity Shader の `colMask` を編集する Python/UnityPy ツール [trimmerpop/Demosaic_colMask](https://github.com/trimmerpop/Demosaic_colMask) に着想を得ています。
