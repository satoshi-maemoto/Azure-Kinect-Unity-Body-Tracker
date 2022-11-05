#pragma once
#include "KinectBodyTracker.h"
#include "IUnityRenderingExtensions.h"

extern "C"
{
	__declspec(dllexport) void K4ABT_SetDebugLogCallback(DebugLogCallbackPtr callback);
	__declspec(dllexport) void K4ABT_GetLastErrorMessage(LPWSTR buffer, DWORD bufferSize);
	__declspec(dllexport) bool K4ABT_Start(unsigned int depthTextureId, unsigned int coloTextureId, unsigned int transformedDepthTextureId,
		k4a_depth_mode_t depthMode, bool cpuOnly);
	__declspec(dllexport) bool K4ABT_End();
	__declspec(dllexport) bool K4ABT_GetBody(void* buffer, uint32_t numBodies);
	__declspec(dllexport) void K4ABT_SetBodyRecognizedCallback(BodyRecognizedCallbackPtr callback);
	__declspec(dllexport) void K4ABT_SetCalibratedJointPointAvailability(bool availability);
	__declspec(dllexport) bool K4ABT_GetImuData(void* buffer);
	__declspec(dllexport) void K4ABT_SetColorImageToDepthSpaceCallback(ColorImageToDepthSpaceCallbackPtr callback);
	__declspec(dllexport) void K4ABT_SetDepthImageToPointCloudCallback(DepthImageToPointCloudCallbackPtr callback);

	UNITY_INTERFACE_EXPORT UnityRenderingEventAndData UNITY_INTERFACE_API GetTextureUpdateCallback();
}
