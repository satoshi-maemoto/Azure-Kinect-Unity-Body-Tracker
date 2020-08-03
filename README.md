# Azure-Kinect-Unity-Body-Tracker

## Overview

AzureKinectのBodyTrackingをUnityで試すために作りました。
汎用的なものではないです。

This project is for the experiments of Body Tracking of Azure Kinect on the Unity.
Not for General purpose.

## Environment

* Unity2019.4.x
* Azure Kinect Body Tracking SDK v1.0.1 provided NuGet
* Latest VisualC++ runtime for x64.  
  https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads

## Setup to run on Unity Editor

1. 以下のURLと同じ手順に従って、NuGetからパッケージを取得し、ファイルを配置します。  
Follow the same procedure as the URL below to get the package from NuGet and place the file.  
https://github.com/microsoft/Azure-Kinect-Samples/tree/master/body-tracking-samples/sample_unity_bodytracking#sample-unity-body-tracking-application

2. 下記ファイルを "Assets/Plugins" から　"Assets\K4AUnityBT\Plugins\x86_64" へ移動します。  
Move following files from "Assets/Plugins" to  "Assets\K4AUnityBT\Plugins\x86_6".

* cublas64_100.dll
* cudart64_100.dll
* depthengine_2_0.dll
* k4a.dll
* k4abt.dll
* k4arecord.dll
* onnxruntime.dll
* vcomp140.dll

## Setup to run the builded binary

Dependency files are will deploy automatically on building the exe. Thanks to @sakamoto-systemfriend
https://github.com/satoshi-maemoto/Azure-Kinect-Unity-Body-Tracker/pull/40


## Setup to build the plugin (Optionaly)

VisualStudioのプロパティマネージャーで下記ユーザーマクロを設定してください。

Setup following user macros in Property Manager of Visual Studio.

![image](https://user-images.githubusercontent.com/530182/61995780-d7fa5b00-b0c7-11e9-9efd-8d7d3534c5eb.png)
