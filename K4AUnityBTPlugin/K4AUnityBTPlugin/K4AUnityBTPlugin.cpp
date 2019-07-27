// dllmain.cpp : DLL アプリケーションのエントリ ポイントを定義します。
#include "pch.h"
#include "K4AUnityBTPlugin.h"
#include "Utils.h"
#include "KinectBodyTracker.h"

static FuncPtr debugPrintFunction = nullptr;
static string lastErrorMessage = string();
static KinectBodyTracker* tracker = nullptr;
static unsigned int outputDepthTextureId = 0;
static unsigned int outputColorTextureId = 0;
static unsigned int outputTransformedDepthTextureId = 0;


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
	if (debugPrintFunction != nullptr)
	{
		debugPrintFunction(message);
	}
	printf("%s\n", message);
}

void K4ABT_SetDebugFunction(FuncPtr fp)
{
	debugPrintFunction = fp;
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
		tracker->Stop();
		delete tracker;
		tracker = nullptr;

		outputDepthTextureId = 0;
		outputColorTextureId = 0;
		outputTransformedDepthTextureId = 0;
	}
	return true;
}

bool K4ABT_GetSkeleton(float* buffer, int length)
{
	memcpy(buffer, tracker->skeleton, sizeof(float) * length);
	return true;
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
		}
		if ((outputColorTextureId == id))
		{
			pParams->texData = tracker->color;
		}
		if ((outputTransformedDepthTextureId == id))
		{
			pParams->texData = tracker->transformedDepth;
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