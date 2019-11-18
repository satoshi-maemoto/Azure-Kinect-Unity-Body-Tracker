# Azure-Kinect-Unity-Body-Tracker

## Overview

AzureKinectのBodyTrackingをUnityで試すために作りました。
汎用的なものではないです。

This project is for the experiments of Body Tracking of Azure Kinect on the Unity.
Not for General purpose.

## Environment

* Unity2019.2.x
* Azure Kinect SDK v1.3.0
* Azure Kinect Body Tracking SDK v0.9.4
* Latest VisualC++ runtime for x64.  
  https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads

## Setup to run

下記ファイルを Azure-Kinect-Unity-Body-Tracker\K4AUnityBT 配下に配置してください。

Put following files at "Azure-Kinect-Unity-Body-Tracker\K4AUnityBT".

* dnn_model_2_0.onnx
* onnxruntime.dll
* cublas64_100.dll
* cudart64_100.dll
* cudnn64_7.dll

下記ファイルを Azure-Kinect-Unity-Body-Tracker\K4AUnityBT\Assets\K4AUnityBT\Plugins\x86_64 配下に配置してください。

Put following files at "Azure-Kinect-Unity-Body-Tracker\K4AUnityBT\Assets\K4AUnityBT\Plugins\x86_64".

* depthengine_2_0.dll

## Setup to build the plugin

VisualStudioのプロパティマネージャーで下記ユーザーマクロを設定してください。

Setup following user macros in Property Manager of Visual Studio.

![image](https://user-images.githubusercontent.com/530182/61995780-d7fa5b00-b0c7-11e9-9efd-8d7d3534c5eb.png)
