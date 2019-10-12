#include "pch.h"
#include "CppUnitTest.h"
#include "K4AUnityBTPlugin.h"
#include <thread>
#include <k4abt.h>
#include "KinectBodyTracker.h"


using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace std;

namespace K4AUnityBTPluginTest
{
	TEST_CLASS(K4AUnityBTPluginTest)
	{
	public:
		static void DebugLog(const char* message)
		{
			Logger::WriteMessage(message);
			Logger::WriteMessage("\n");
		}

		static void BodyRecognized(int numBodies)
		{
			KinectBodyTracker::Body bodies[K4ABT_MAX_BODY];
			K4ABT_GetBody(bodies, numBodies);

 			for (auto i = 0; i < numBodies; i++)
			{
				for (auto j = 0; j < K4ABT_JOINT_COUNT; j++)
				{
					DebugLog((
						"ID:" + to_string(bodies[i].body.id) + " J:" +
						to_string(j) + " P(" +
						to_string(bodies[i].body.skeleton.joints[j].position.xyz.x) + "," +
						to_string(bodies[i].body.skeleton.joints[j].position.xyz.y) + "," +
						to_string(bodies[i].body.skeleton.joints[j].position.xyz.z) + ") C(" +
						to_string(bodies[i].calibratedJointPoints[j].xy.x) + "," +
						to_string(bodies[i].calibratedJointPoints[j].xy.y) + ") R(" +
						to_string(bodies[i].body.skeleton.joints[j].orientation.wxyz.x) + "," +
						to_string(bodies[i].body.skeleton.joints[j].orientation.wxyz.y) + "," +
						to_string(bodies[i].body.skeleton.joints[j].orientation.wxyz.z) + "," +
						to_string(bodies[i].body.skeleton.joints[j].orientation.wxyz.w) + ")"
						).c_str());
				}
			}

			KinectBodyTracker::ImuData imuData;
			K4ABT_GetImuData(&imuData);
			DebugLog((
				"IMU: TMP=" + to_string(imuData.imuSample.temperature) + " ACC(" +
				to_string(imuData.imuSample.acc_sample.xyz.x) + "," +
				to_string(imuData.imuSample.acc_sample.xyz.y) + "," +
				to_string(imuData.imuSample.acc_sample.xyz.z) + ") GYRO(" +
				to_string(imuData.imuSample.gyro_sample.xyz.x) + "," +
				to_string(imuData.imuSample.gyro_sample.xyz.y) + "," +
				to_string(imuData.imuSample.gyro_sample.xyz.z) + ")/(" +
				to_string(imuData.integralGyro.xyz.x) + "," +
				to_string(imuData.integralGyro.xyz.y) + "," +
				to_string(imuData.integralGyro.xyz.z) + 
				")"
				).c_str());

		}

		TEST_METHOD(RunTest)
		{
			K4ABT_SetDebugLogCallback(DebugLog);
			K4ABT_SetBodyRecognizedCallback(BodyRecognized);

			KinectBodyTracker::Body b;
			DebugLog((string(" SIZE : ") + std::to_string(sizeof(b))).c_str());
			DebugLog((string(" SIZE : ") + std::to_string(sizeof(b.body))).c_str());
			DebugLog((string(" SIZE : ") + std::to_string(sizeof(b.calibratedJointPoints))).c_str());

			for (int depthMode = K4A_DEPTH_MODE_OFF; depthMode <= K4A_DEPTH_MODE_PASSIVE_IR; depthMode++)
			{
				DebugLog((string(" Depth Mode : ") + std::to_string(depthMode) + string(" on CPU")).c_str());
				K4ABT_Start(-1, -1, -1, (k4a_depth_mode_t)depthMode, true);

				for (auto i = 0; i < 3; i++)
				{
					this_thread::sleep_for(chrono::seconds(1));
				}

				K4ABT_End();

				DebugLog((string(" Depth Mode : ") + std::to_string(depthMode) + string(" on GPU")).c_str());
				K4ABT_Start(-1, -1, -1, (k4a_depth_mode_t)depthMode, false);

				for (auto i = 0; i < 3; i++)
				{
					this_thread::sleep_for(chrono::seconds(1));
				}

				K4ABT_End();
			}
		}
	};
}
