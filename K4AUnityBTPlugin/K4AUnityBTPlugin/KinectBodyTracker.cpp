#include "pch.h"
#include "KinectBodyTracker.h"

using namespace std;

#define VERIFY(result, error)                                                                            \
    if(result != K4A_RESULT_SUCCEEDED)                                                                   \
    {                                                                                                    \
        printf("%s \n - (File: %s, Function: %s, Line: %d)\n", error, __FILE__, __FUNCTION__, __LINE__); \
        throw exception(error);                                                                          \
    }

void KinectBodyTracker::Start()
{
	auto deviceConfig = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
	deviceConfig.depth_mode = K4A_DEPTH_MODE_NFOV_UNBINNED;
	deviceConfig.color_resolution = K4A_COLOR_RESOLUTION_1080P;
	deviceConfig.color_format = K4A_IMAGE_FORMAT_COLOR_BGRA32;

	VERIFY(k4a_device_open(0, &this->device), "Open K4A Device failed!");
	VERIFY(k4a_device_start_cameras(this->device, &deviceConfig), "Start K4A cameras failed!");

	k4a_calibration_t calibration;
	VERIFY(k4a_device_get_calibration(this->device, deviceConfig.depth_mode, deviceConfig.color_resolution, &calibration),
		"Get depth camera calibration failed!");

	this->tracker = nullptr;
	VERIFY(k4abt_tracker_create(&calibration, &this->tracker), "Body tracker initialization failed!");

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

			do
			{
				k4a_capture_t capture;
				if (k4a_device_get_capture(this->device, &capture, 0) == K4A_WAIT_RESULT_SUCCEEDED)
				{
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
							this->colorTimestamp = k4a_image_get_timestamp_usec(colorImage);
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
							this->depthTimestamp = k4a_image_get_timestamp_usec(depthImage);
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
									this->transformedDepthTimestamp = k4a_image_get_timestamp_usec(transformedDepthImage);
								}
							}
						}

						auto queueCaptureResult = k4abt_tracker_enqueue_capture(this->tracker, capture, 0);
						k4a_capture_release(capture);
						if (queueCaptureResult == K4A_WAIT_RESULT_FAILED)
						{
							this->DebugLog("Error! Add capture to tracker process queue failed!\n");
						}

						k4a_image_release(depthImage);
					}

					k4abt_frame_t bodyFrame = nullptr;
					if (k4abt_tracker_pop_result(this->tracker, &bodyFrame, 0) == K4A_WAIT_RESULT_SUCCEEDED)
					{
						auto numBodies = k4abt_frame_get_num_bodies(bodyFrame);
						memset(this->bodies, 0, sizeof(k4abt_body_t) * K4ABT_MAX_BODY);
						for (auto i = 0; i < numBodies; ++i)
						{
							this->bodies[i].id = k4abt_frame_get_body_id(bodyFrame, i);
							k4abt_frame_get_body_skeleton(bodyFrame, i, &this->bodies[i].skeleton);
						}
						k4abt_frame_release(bodyFrame);
					}
				}
			} while (this->isRunning);

			if (transformedDepthImage != nullptr)
			{
				k4a_image_release(transformedDepthImage);
			}
			if (transformation != nullptr)
			{
				k4a_transformation_destroy(transformation);
			}

			this->DebugLog("Finished worker thread\n");
		}
	);
}

void KinectBodyTracker::Stop()
{
	this->isRunning = false;
	k4abt_tracker_shutdown(this->tracker);
	this->workerThread.join();

	free(this->depth);
	free(this->color);
	free(this->transformedDepth);

	k4abt_tracker_destroy(this->tracker);
	k4a_device_stop_cameras(this->device);
	k4a_device_close(this->device);

	this->DebugLog("Finished body tracking processing!\n");
}

void KinectBodyTracker::SetDebugLogFunction(DebugLogFuncPtr fp)
{
	this->debugPrintFunction = fp;
}

void KinectBodyTracker::DebugLog(const char* message)
{
	if (this->debugPrintFunction != nullptr)
	{
		this->debugPrintFunction(message);
	}
	printf("%s\n", message);
}