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
		}

		TEST_METHOD(RunTest)
		{
			K4ABT_SetDebugFunction(DebugLog);

			K4ABT_Start(-1, -1, -1);

			for (auto i = 0; i < 10; i++)
			{
				this_thread::sleep_for(chrono::seconds(1));

				k4abt_body_t bodies[K4ABT_MAX_BODY];
				K4ABT_GetBodies(bodies, K4ABT_MAX_BODY);
				for (auto i = 0; i < K4ABT_MAX_BODY; i++)
				{
					for (auto j = 0; j < K4ABT_JOINT_COUNT; j++)
					{
						DebugLog((
							"ID:" + to_string(bodies[i].id) + " J:" +
							to_string(j) + " (" + 
							to_string(bodies[i].skeleton.joints[j].position.xyz.x) + "," +
							to_string(bodies[i].skeleton.joints[j].position.xyz.y) + "," +
							to_string(bodies[i].skeleton.joints[j].position.xyz.z) + ") (" +
							to_string(bodies[i].skeleton.joints[j].orientation.wxyz.x) + "," +
							to_string(bodies[i].skeleton.joints[j].orientation.wxyz.y) + "," +
							to_string(bodies[i].skeleton.joints[j].orientation.wxyz.z) + "," +
							to_string(bodies[i].skeleton.joints[j].orientation.wxyz.w) + ")"
							).c_str());
					}
				}
			}

			K4ABT_End();
		}
	};
}
