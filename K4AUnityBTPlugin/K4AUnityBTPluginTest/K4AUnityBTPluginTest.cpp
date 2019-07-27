#include "pch.h"
#include "CppUnitTest.h"
#include "K4AUnityBTPlugin.h"
#include <thread>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

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
				std::this_thread::sleep_for(std::chrono::seconds(1));

				const int bufferLength = 3 * 25;
				float buffer[bufferLength];
				K4ABT_GetSkeleton(buffer, bufferLength);

				for (auto i = 0; i < bufferLength; i++)
				{
					DebugLog(std::to_string(buffer[i]).c_str());
				}
			}

			K4ABT_End();
		}
	};
}
