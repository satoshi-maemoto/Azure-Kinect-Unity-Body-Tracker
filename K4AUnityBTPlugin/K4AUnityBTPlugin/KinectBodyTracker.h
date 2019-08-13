#pragma once
#include <thread>
#include <k4a/k4a.h>
#include <k4abt.h>

#define K4ABT_MAX_BODY 6

typedef void(*DebugLogFuncPtr)(const char*);

class KinectBodyTracker
{
public:
	void Start();
	void Stop();
	void SetDebugLogFunction(DebugLogFuncPtr fp);

	k4abt_body_t bodies[K4ABT_MAX_BODY];
	unsigned long* color;
	uint64_t colorTimestamp;
	unsigned short* depth;
	uint64_t depthTimestamp;
	unsigned short* transformedDepth;
	uint64_t transformedDepthTimestamp;

private:
	void DebugLog(const char* message);

	k4a_device_t device;
	k4abt_tracker_t tracker;
	bool isRunning;
	std::thread workerThread;
	DebugLogFuncPtr debugPrintFunction = nullptr;
};

