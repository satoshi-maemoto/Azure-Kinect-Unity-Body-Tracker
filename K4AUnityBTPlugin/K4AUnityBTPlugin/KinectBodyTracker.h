#pragma once
#include <thread>
#include <k4a/k4a.h>
#include <k4abt.h>

#define K4ABT_MAX_BODY 6

class KinectBodyTracker
{
public:
	void Start();
	void Stop();

	k4abt_body_t bodies[K4ABT_MAX_BODY];
	unsigned long* color;
	unsigned short* depth;
	unsigned short* transformedDepth;

private:
	k4a_device_t device;
	k4abt_tracker_t tracker;
	bool isRunning;
	std::thread workerThread;
};

