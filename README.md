# 顔万華鏡 Unity HDR版

2024/8/31のSNACKS Vol6に展示する作品

[./当日説明.md](./当日説明.md)

## 使っているライブラリ

https://github.com/asus4/tf-lite-unity-sample


## UnityでMediapipeの顔認識を実行する

このプロジェクトに甘える<br>
https://github.com/asus4/tf-lite-unity-sample


コピーするフォルダ
+ Assets/StreamingAssets
+ Assets/Samples/Common
+ Assets/Samples/FaceMesh
+ Packages

オリジナル（asus4/tf-lite-unity-sample）のプロジェクトではGit LFSを使っている。<br>
料金の問題もあるが、複雑になるので、なるべく使いたくない。<br>
モデルデータ（.tflite）の一部がGitHubの上限の100MBを超えるが、今回使うのは`Assets/StreamingAssets/mediapipe/face_detection_front.tflite`と`Assets/StreamingAssets/mediapipe/face_landmark.tflite`のみなので、`Assets/StreamingAssets/`直下のファイルは削除した。



## UnityのURPでHDRでビカビカさせるためのメモ

### プロジェクトの作成
プロジェクトの新規作成でUniversal 3Dを選ぶ

### プロジェクトの設定
プロジェクトの設定でHDRを有効にする

Player > Other Settings > Rendering
+ Allow HDR Display Outputにチェック
+ Use HDR Display Outputにチェック
+ Swap Chain Bit DepthをBit Depth 16に変更（お好みで）

参考: <br>
https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/post-processing/hdr-output.html

### URPのポストプロセスでシェーダーを書く

お作法が複雑なため、ShaderGraphに甘える。
ノードが複雑になる部分はCustom Functionノードを使い、HLSLで記述する。<br>

#### シェーダーグラフを作る
URPのフルスクリーン用シェーダーグラフを作る<br>
Create > Shader Graph > URP > Fullscreen Shader Graph

#### Renderer Featureに追加する

`Add Renderer Feature`から`Full Screen Pass Renderer Feature`を選び追加する。<br>
先ほど作ったシェーダーグラフのマテリアルを設定する<br>
`Inject Point`を`Before Rendering Post Processing`に変更する

### Custom Function Nodeを作る

https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Custom-Function-Node.html

### PlayerでHDRを有効にする

```c#
HDROutputSettings.main.RequestHDRModeChange(true);
```


## 開発で困ったことと対処のメモ

### ShaderGraphのPropertyの値がうまくCustom Functionに渡せない
値を直接指定すると呼び出せる。<br>
Inspectorでは渡せているように見える。<br>
NaNかInfinityが渡されているような変な計算結果になる。

問題が起きているプロパティの名前を`dummy`などに変える。<br>
そのプロパティを使わずに次のプロパティを使うと何故か直る。


### Unityで`.shadergraph`のインポートが終わらないとき
一度プロジェクトの外に出し、インポートし直す

