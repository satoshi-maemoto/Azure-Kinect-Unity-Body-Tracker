#pragma once
#include <thread>
#include <k4a/k4a.h>
#include <k4abt.h>

class KinectBodyTracker
{
public:
	void Start();
	void Stop();

	float skeleton[3 * (K4ABT_JOINT_COUNT - 1)];
	unsigned long* color;
	unsigned short* depth;
	unsigned short* transformedDepth;

private:
	k4a_device_t device;
	k4abt_tracker_t tracker;
	bool isRunning;
	std::thread workerThread;
};

