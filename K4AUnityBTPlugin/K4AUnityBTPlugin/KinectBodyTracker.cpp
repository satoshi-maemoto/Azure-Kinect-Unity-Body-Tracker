#include "pch.h"
#include "KinectBodyTracker.h"
#include "Utils.h"

using namespace std;

void Verify(KinectBodyTracker* self, k4a_result_t result, string error)
{
	if (result != K4A_RESULT_SUCCEEDED)
	{
		auto lastError = GetLastError();
		LPTSTR lastErrorMessage = nullptr;
		FormatMessage(
			FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_ARGUMENT_ARRAY | FORMAT_MESSAGE_ALLOCATE_BUFFER,
			nullptr, lastError, MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US), (LPWSTR)&lastErrorMessage, 0, nullptr);
		printf("%s \n - (File: %s, Function: %s, Line: %d)\n", error.c_str(), __FILE__, __FUNCTION__, __LINE__);
		self->Stop();
		throw exception((error + string(" : ") + Utils::WStringToString(wstring(lastErrorMessage))).c_str());
	}
}

void KinectBodyTracker::Start()
{
	auto deviceConfig = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
	deviceConfig.depth_mode = K4A_DEPTH_MODE_NFOV_UNBINNED;
	deviceConfig.color_resolution = K4A_COLOR_RESOLUTION_1080P;
	deviceConfig.color_format = K4A_IMAGE_FORMAT_COLOR_BGRA32;
	this->Start(deviceConfig, K4ABT_TRACKER_CONFIG_DEFAULT);
}

void KinectBodyTracker::Start(k4a_device_configuration_t deviceConfig, k4abt_tracker_configuration_t trackerConfiguration)
{
	Verify(this, k4a_device_open(0, &this->device), "Open K4A Device failed!");
	Verify(this, k4a_device_start_cameras(this->device, &deviceConfig), "Start K4A cameras failed!");

	k4a_calibration_t calibration;
	Verify(this, k4a_device_get_calibration(this->device, deviceConfig.depth_mode, deviceConfig.color_resolution, &calibration),
		"Get depth camera calibration failed!");

	Verify(this, k4abt_tracker_create(&calibration, trackerConfiguration, &this->tracker), "Body tracker initialization failed!");

	Verify(this, k4a_device_start_imu(this->device), "Start IMU failed!");

	this->depth = nullptr;
	this->color = nullptr;
	this->transformedDepth = nullptr;

	this->isRunning = true;
	this->workerThread = thread([this, calibration]()
		{
			size_t depthImageSize = -1;
			size_t colorImageSize = -1;
			size_t transformedDepthSize = -1;
			int colorImageWidth = -1;
			int colorImageHeight = -1;
			k4a_image_t transformedDepthImage = nullptr;
			auto transformation = k4a_transformation_create(&calibration);
			int validCalibratedPoint;
			this->imuData.integralGyro = { 0, 0, 0 };
			uint64_t prevGyroTimestampUsec = 0;

			do
			{
				k4a_capture_t capture;
				if (k4a_device_get_capture(this->device, &capture, 0) == K4A_WAIT_RESULT_SUCCEEDED)
				{
					auto queueCaptureResult = k4abt_tracker_enqueue_capture(this->tracker, capture, 0);
					k4a_capture_release(capture);
					if (queueCaptureResult == K4A_WAIT_RESULT_FAILED)
					{
						this->DebugLog("Error! Add capture to tracker process queue failed!\n");
					}

					k4abt_frame_t bodyFrame = nullptr;
					if (k4abt_tracker_pop_result(this->tracker, &bodyFrame, 0) == K4A_WAIT_RESULT_SUCCEEDED)
					{
						auto numBodies = k4abt_frame_get_num_bodies(bodyFrame);
						memset(this->bodies, 0, sizeof(Body) * K4ABT_MAX_BODY);
						for (auto i = 0; i < numBodies; ++i)
						{
							this->bodies[i].body.id = k4abt_frame_get_body_id(bodyFrame, i);
							k4abt_frame_get_body_skeleton(bodyFrame, i, &this->bodies[i].body.skeleton);
							if (this->calibratedJointPointAvailability)
							{
								for (auto j = 0; j < K4ABT_JOINT_COUNT; j++)
								{
									k4a_calibration_3d_to_2d(&calibration, &this->bodies[i].body.skeleton.joints[j].position,
										K4A_CALIBRATION_TYPE_DEPTH, K4A_CALIBRATION_TYPE_COLOR,
										&this->bodies[i].calibratedJointPoints[j], &validCalibratedPoint);
								}
							}
						}

						auto capture = k4abt_frame_get_capture(bodyFrame);
						auto colorImage = k4a_capture_get_color_image(capture);
						if (colorImage != nullptr)
						{
							if (colorImageSize == -1)
							{
								colorImageSize = k4a_image_get_size(colorImage);
								colorImageWidth = k4a_image_get_width_pixels(colorImage);
								colorImageHeight = k4a_image_get_height_pixels(colorImage);
							}
							if (this->color == nullptr)
							{
								this->color = (unsigned long*)malloc(colorImageSize);
							}
							if (this->color != nullptr)
							{
								memcpy(this->color, k4a_image_get_buffer(colorImage), colorImageSize);
								this->colorTimestamp = k4a_image_get_device_timestamp_usec(colorImage);
							}
							k4a_image_release(colorImage);
						}

						auto depthImage = k4a_capture_get_depth_image(capture);
						if (depthImage != nullptr)
						{
							if (depthImageSize == -1)
							{
								depthImageSize = k4a_image_get_size(depthImage);
							}
							if (this->depth == nullptr)
							{
								this->depth = (unsigned short*)malloc(depthImageSize);
							}
							if (this->depth != nullptr)
							{
								memcpy(this->depth, k4a_image_get_buffer(depthImage), depthImageSize);
								this->depthTimestamp = k4a_image_get_device_timestamp_usec(depthImage);
							}

							if ((transformedDepthImage == nullptr) && ((colorImageWidth != -1) && (colorImageHeight != -1)))
							{
								k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, colorImageWidth, colorImageHeight, colorImageWidth * (int)sizeof(uint16_t), &transformedDepthImage);
							}
							if (transformedDepthImage != nullptr)
							{
								if (k4a_transformation_depth_image_to_color_camera(transformation, depthImage, transformedDepthImage) == K4A_RESULT_SUCCEEDED)
								{
									if (transformedDepthSize == -1)
									{
										transformedDepthSize = k4a_image_get_size(transformedDepthImage);
									}
									if (this->transformedDepth == nullptr)
									{
										this->transformedDepth = (unsigned short*)malloc(transformedDepthSize);
									}
									if (this->transformedDepth != nullptr)
									{
										memcpy(this->transformedDepth, k4a_image_get_buffer(transformedDepthImage), transformedDepthSize);
										this->transformedDepthTimestamp = k4a_image_get_device_timestamp_usec(transformedDepthImage);
									}
								}
							}
							k4a_image_release(depthImage);
						}
						k4abt_frame_release(bodyFrame);
						k4a_capture_release(capture);

						if (this->bodyRecognizedCallback != nullptr)
						{
							this->bodyRecognizedCallback((int)numBodies);
						}
					}
				}
				k4a_device_get_imu_sample(this->device, &this->imuData.imuSample, 0);
				if (prevGyroTimestampUsec > 0)
				{
					auto timeDiff = (float)((this->imuData.imuSample.gyro_timestamp_usec - prevGyroTimestampUsec) / 1000000.0);
					this->imuData.integralGyro.xyz.x += this->imuData.imuSample.gyro_sample.xyz.x * timeDiff;
					this->imuData.integralGyro.xyz.y += this->imuData.imuSample.gyro_sample.xyz.y * timeDiff;
					this->imuData.integralGyro.xyz.z += this->imuData.imuSample.gyro_sample.xyz.z * timeDiff;
				}
				prevGyroTimestampUsec = this->imuData.imuSample.gyro_timestamp_usec;

			} while (this->isRunning);

			if (this->tracker != nullptr)
			{
				k4abt_tracker_shutdown(this->tracker);
			}

			if (transformedDepthImage != nullptr)
			{
				k4a_image_release(transformedDepthImage);
			}
			if (transformation != nullptr)
			{
				k4a_transformation_destroy(transformation);
			}

			if (this->depth != nullptr)
			{
				free(this->depth);
				this->depth = nullptr;
			}
			if (this->color != nullptr)
			{
				free(this->color);
				this->color = nullptr;
			}
			if (this->transformedDepth != nullptr)
			{
				free(this->transformedDepth);
				this->transformedDepth = nullptr;
			}

			this->DebugLog("Finished worker thread\n");
		}
	);
}

void KinectBodyTracker::Stop()
{
	this->DebugLog("Started body tracking processing!\n");

	this->isRunning = false;
	if (this->workerThread.joinable())
	{
		this->workerThread.join();
	}

	if (this->tracker != nullptr)
	{
		k4abt_tracker_destroy(this->tracker);
		this->tracker = nullptr;
	}
	if (this->device != nullptr)
	{
		k4a_device_stop_imu(this->device);
		k4a_device_stop_cameras(this->device);
		k4a_device_close(this->device);
		this->device = nullptr;
	}

	this->DebugLog("Finished body tracking processing!\n");
}

void KinectBodyTracker::SetDebugLogCallback(DebugLogCallbackPtr callback)
{
	this->debugLogCallback = callback;
}

void KinectBodyTracker::DebugLog(const char* message)
{
	if (this->debugLogCallback != nullptr)
	{
		this->debugLogCallback(message);
	}
	printf("%s\n", message);
}

void KinectBodyTracker::SetBodyRecognizedCallback(BodyRecognizedCallbackPtr callback)
{
	this->bodyRecognizedCallback = callback;
}

void KinectBodyTracker::SetCalibratedJointPointAvailability(bool availability)
{
	this->calibratedJointPointAvailability = availability;
}
