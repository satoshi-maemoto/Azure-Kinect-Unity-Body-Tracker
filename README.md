# Azure-Kinect-Unity-Body-Tracker

## Overview

AzureKinectのBodyTrackingをUnityで試すために作りました。
This project created for experiments Body Tracking of Azure Kinect on Unity.

## Environment

* Unity2021.3.x
* Azure Kinect Body Tracking SDK v1.1.2 provided NuGet
* Latest VisualC++ runtime for x64.  
https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads

## Setup to run on Unity Editor

1. 以下のURLの手順に従って、NuGetからパッケージを取得し、ファイルを配置します。  
Get NuGet package and place files following step.  
https://www.nuget.org/packages/Microsoft.Azure.Kinect.BodyTracking/1.1.2  
https://github.com/microsoft/Azure-Kinect-Samples/tree/master/body-tracking-samples/sample_unity_bodytracking#sample-unity-body-tracking-application

2. "Assets/Plugins" 内の全てのファイルを "Assets\K4AUnityBT\Plugins\x86_64" へ移動します。  
Move all files contained "Assets/Plugins" to  "Assets\K4AUnityBT\Plugins\x86_64".


## Build binary

Dependency files are deploy automatically on building exe. Thanks to @sakamoto-systemfriend
https://github.com/satoshi-maemoto/Azure-Kinect-Unity-Body-Tracker/pull/40

