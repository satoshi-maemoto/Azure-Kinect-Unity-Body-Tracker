// dllmain.cpp : DLL アプリケーションのエントリ ポイントを定義します。
#include "pch.h"
#include "tchar.h"
#include "K4AUnityBTPlugin.h"
#include "Utils.h"

static DebugLogCallbackPtr debugLogCallback = nullptr;
static string lastErrorMessage = string();
static KinectBodyTracker* tracker = nullptr;
static unsigned int outputDepthTextureId = 0;
static unsigned int outputColorTextureId = 0;
static unsigned int outputTransformedDepthTextureId = 0;
static BodyRecognizedCallbackPtr bodyRecognizedCallback = nullptr;

static ColorImageToDepthSpaceCallbackPtr colorImageToDepthSpaceCallback = nullptr;
static DepthImageToPointCloudCallbackPtr depthImageToPointCloudCallback = nullptr;

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
		break;
    case DLL_PROCESS_DETACH:
		_RPT0(_CRT_WARN, "K4ABTPlugin FreeLibrary DLL_PROCESS_DETACH Start\n");

		auto releaseTargets = 
		{
			_T("K4AUnityBTPlugin.dll")
		};
		for (auto target : releaseTargets)
		{
			HMODULE moduleHandle;
			if (GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, target, &moduleHandle)) {
				//FreeLibraryAndExitThread(moduleHandle, 0);
				_RPTWN(_CRT_WARN, _T("K4ABTPlugin FreeLibrary %s\n"), target);
			}
		}

		_RPT0(_CRT_WARN, "K4ABTPlugin FreeLibrary DLL_PROCESS_DETACH End\n");
		break;
    }
    return TRUE;
}

void DebugLog(const char* message)
{
	if (debugLogCallback != nullptr)
	{
		debugLogCallback(message);
	}
	printf("%s\n", message);
}

void K4ABT_SetDebugLogCallback(DebugLogCallbackPtr callback)
{
	debugLogCallback = callback;
	if (tracker != nullptr)
	{
		tracker->SetDebugLogCallback(debugLogCallback);
	}
}

__declspec(dllexport) void K4ABT_GetLastErrorMessage(LPWSTR buffer, DWORD bufferSize)
{
	wstring message = Utils::StringToWString(lastErrorMessage);
	wcscpy_s(buffer, bufferSize, message.c_str());
}

__declspec(dllexport) bool K4ABT_Start(unsigned int depthTextureId, unsigned int coloTextureId, unsigned int transformedDepthTextureId, 
	k4a_depth_mode_t depthMode, bool cpuOnly)
{
	DebugLog("K4ABT_Start()\n");

	try
	{
		if (tracker == nullptr)
		{
			tracker = new KinectBodyTracker();
			tracker->SetDebugLogCallback(debugLogCallback);
			tracker->SetBodyRecognizedCallback(bodyRecognizedCallback);
		}
		outputDepthTextureId = depthTextureId;
		outputColorTextureId = coloTextureId;
		outputTransformedDepthTextureId = transformedDepthTextureId;

		auto deviceConfig = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
		deviceConfig.depth_mode = depthMode;
		deviceConfig.color_resolution = K4A_COLOR_RESOLUTION_1080P;
		deviceConfig.color_format = K4A_IMAGE_FORMAT_COLOR_BGRA32;

		auto trackerConfig = K4ABT_TRACKER_CONFIG_DEFAULT;
		trackerConfig.processing_mode = cpuOnly ? K4ABT_TRACKER_PROCESSING_MODE_CPU : K4ABT_TRACKER_PROCESSING_MODE_GPU;
		tracker->Start(deviceConfig, trackerConfig);
	}
	catch (exception exception)
	{
		DebugLog(exception.what());
		lastErrorMessage = exception.what();
		return false;
	}
	return true;
}

__declspec(dllexport) bool K4ABT_End()
{
	DebugLog("K4ABT_End()\n");

	if (tracker != nullptr)
	{
		if (tracker != nullptr)
		{
			tracker->Stop();
			delete tracker;
			tracker = nullptr;
		}

		outputDepthTextureId = 0;
		outputColorTextureId = 0;
		outputTransformedDepthTextureId = 0;
	}
	return true;
}

bool K4ABT_GetBody(void* buffer, int numBodies)
{
	if (numBodies > K4ABT_MAX_BODY)
	{
		return false;
	}
	memcpy(buffer, tracker->bodies, sizeof(KinectBodyTracker::Body) * numBodies);
	return true;
}

void K4ABT_SetBodyRecognizedCallback(BodyRecognizedCallbackPtr callback)
{
	bodyRecognizedCallback = callback;
	if (tracker != nullptr)
	{
		tracker->SetBodyRecognizedCallback(bodyRecognizedCallback);
	}
}

void K4ABT_SetCalibratedJointPointAvailability(bool availability)
{
	if (tracker != nullptr)
	{
		tracker->SetCalibratedJointPointAvailability(availability);
	}
}

bool K4ABT_GetImuData(void* buffer)
{
	memcpy(buffer, &tracker->imuData , sizeof(KinectBodyTracker::ImuData));
	return true;
}

void K4ABT_SetDepthImageToPointCloudCallback(DepthImageToPointCloudCallbackPtr callback)
{
	depthImageToPointCloudCallback = callback;
	if (tracker != nullptr) 
	{
		tracker->SetDepthImageToPointCloudCallback(depthImageToPointCloudCallback);
	}
}

void K4ABT_SetColorImageToDepthSpaceCallback(ColorImageToDepthSpaceCallbackPtr callback) 
{
	colorImageToDepthSpaceCallback = callback;
	if (tracker != nullptr)
	{
		tracker->SetColorImageToDepthSpaceCallback(colorImageToDepthSpaceCallback);
	}
}

void OnTextureUpdate(int eventId, void* pData)
{
	const auto event = static_cast<UnityRenderingExtEventType>(eventId);

	if (event == kUnityRenderingExtEventUpdateTextureBeginV2)
	{
		auto* pParams = reinterpret_cast<UnityRenderingExtTextureUpdateParamsV2*>(pData);
		const auto id = pParams->userData;
		if ((outputDepthTextureId == id))
		{
			//DebugLog(::to_string(tracker->depth[288 * 640 + 320 + 0]).c_str());
			pParams->texData = tracker->depth;
			//DebugLog(("Depth Timestamp: " + ::to_string(tracker->depthTimestamp)).c_str());
		}
		if ((outputColorTextureId == id))
		{
			pParams->texData = tracker->color;
			//DebugLog(("Color Timestamp: " + ::to_string(tracker->colorTimestamp)).c_str());
		}
		if ((outputTransformedDepthTextureId == id))
		{
			pParams->texData = tracker->transformedDepth;
			//DebugLog(("Transformed Depth Timestamp: " + ::to_string(tracker->transformedDepthTimestamp)).c_str());
		}
	}
	else if (event == kUnityRenderingExtEventUpdateTextureEndV2)
	{
		auto* pParams = reinterpret_cast<UnityRenderingExtTextureUpdateParamsV2*>(pData);
		pParams->texData = nullptr;
	}
}

UNITY_INTERFACE_EXPORT UnityRenderingEventAndData UNITY_INTERFACE_API GetTextureUpdateCallback()
{
	return OnTextureUpdate;
}