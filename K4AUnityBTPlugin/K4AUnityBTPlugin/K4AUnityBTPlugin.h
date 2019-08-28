#pragma once
#include "KinectBodyTracker.h"
#include "IUnityRenderingExtensions.h"

extern "C"
{
	__declspec(dllexport) void K4ABT_SetDebugLogCallback(DebugLogCallbackPtr callback);
	__declspec(dllexport) void K4ABT_GetLastErrorMessage(LPWSTR buffer, DWORD bufferSize);
	__declspec(dllexport) bool K4ABT_Start(unsigned int depthTextureId, unsigned int coloTextureId, unsigned int transformedDepthTextureId);
	__declspec(dllexport) bool K4ABT_End();
	__declspec(dllexport) bool K4ABT_GetBodies(void* buffer, int numBodies);
	__declspec(dllexport) void K4ABT_SetBodyRecognizedCallback(BodyRecognizedCallbackPtr callback);
	UNITY_INTERFACE_EXPORT UnityRenderingEventAndData UNITY_INTERFACE_API GetTextureUpdateCallback();
}
