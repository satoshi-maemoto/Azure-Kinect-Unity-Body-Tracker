// dllmain.cpp : DLL アプリケーションのエントリ ポイントを定義します。
#include "pch.h"
#include "K4AUnityBTPlugin.h"
#include "Utils.h"

static DebugLogCallbackPtr debugLogCallback = nullptr;
static string lastErrorMessage = string();
static KinectBodyTracker* tracker = nullptr;
static unsigned int outputDepthTextureId = 0;
static unsigned int outputColorTextureId = 0;
static unsigned int outputTransformedDepthTextureId = 0;
static BodyRecognizedCallbackPtr bodyRecognizedCallback = nullptr;


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
    case DLL_PROCESS_DETACH:
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

__declspec(dllexport) bool K4ABT_Start(unsigned int depthTextureId, unsigned int coloTextureId, unsigned int transformedDepthTextureId)
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

		tracker->Start();
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