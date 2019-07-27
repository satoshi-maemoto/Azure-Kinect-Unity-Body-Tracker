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

	this->depth = (unsigned short*)malloc(640 * 576 * sizeof(unsigned short));
	this->color = (unsigned long*)malloc(1920 * 1080 * sizeof(unsigned int));
	this->transformedDepth = (unsigned short*)malloc(1920 * 1080 * sizeof(unsigned short));
	k4a_image_t transformedDepthImage = nullptr;
	VERIFY(k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, 1920, 1080, 1920 * (int)sizeof(uint16_t), &transformedDepthImage), "Failed to create transformed depth image\n");
	auto transformation = k4a_transformation_create(&calibration);

	this->isRunning = true;

	this->workerThread = thread([this, transformation, transformedDepthImage]()
		{

			do
			{
				k4a_capture_t capture;
				if (k4a_device_get_capture(this->device, &capture, 0) == K4A_WAIT_RESULT_SUCCEEDED)
				{
					auto depthImage = k4a_capture_get_depth_image(capture);
					if (depthImage != nullptr)
					{
						auto size = k4a_image_get_size(depthImage);
						memcpy(this->depth, k4a_image_get_buffer(depthImage), size);

						if (K4A_RESULT_SUCCEEDED == k4a_transformation_depth_image_to_color_camera(transformation, depthImage, transformedDepthImage))
						{
							size = k4a_image_get_size(transformedDepthImage);
							memcpy(this->transformedDepth, k4a_image_get_buffer(transformedDepthImage), size);
						}

						k4a_image_release(depthImage);
					}

					auto colorImage = k4a_capture_get_color_image(capture);
					if (colorImage != nullptr)
					{
						auto size = k4a_image_get_size(colorImage);
						memcpy(this->color, k4a_image_get_buffer(colorImage), size);
						k4a_image_release(colorImage);
					}

					auto queueCaptureResult = k4abt_tracker_enqueue_capture(this->tracker, capture, K4A_WAIT_INFINITE);
					k4a_capture_release(capture);
					if (queueCaptureResult == K4A_WAIT_RESULT_FAILED)
					{
						printf("Error! Add capture to tracker process queue failed!\n");
					}

					k4abt_frame_t bodyFrame = nullptr;
					auto pop_frame_result = k4abt_tracker_pop_result(this->tracker, &bodyFrame, K4A_WAIT_INFINITE);
					if (pop_frame_result == K4A_WAIT_RESULT_SUCCEEDED)
					{
						auto num_bodies = k4abt_frame_get_num_bodies(bodyFrame);
						printf("%zu bodies are detected!\n", num_bodies);

						memset(this->skeleton, 0, sizeof(this->skeleton));
						for (auto i = 0; i < num_bodies; ++i)
						{
							k4abt_body_t body;
							k4abt_frame_get_body_skeleton(bodyFrame, i, &body.skeleton);

							for (auto j = 0; j < (K4ABT_JOINT_COUNT - 1); j++)
							{
								this->skeleton[j * 3 + 0] = body.skeleton.joints[j].position.xyz.x;
								this->skeleton[j * 3 + 1] = body.skeleton.joints[j].position.xyz.y;
								this->skeleton[j * 3 + 2] = body.skeleton.joints[j].position.xyz.z;
							}
							break;
						}

						k4abt_frame_release(bodyFrame);
					}
				}
			} while (this->isRunning);

			k4a_image_release(transformedDepthImage);
			if (transformation != nullptr)
			{
				k4a_transformation_destroy(transformation);
			}
		}
	);
}

void KinectBodyTracker::Stop()
{
	this->isRunning = false;
	this->workerThread.join();

	free(this->depth);
	free(this->color);
	free(this->transformedDepth);

	k4abt_tracker_shutdown(this->tracker);
	k4abt_tracker_destroy(this->tracker);
	k4a_device_stop_cameras(this->device);
	k4a_device_close(this->device);

	printf("Finished body tracking processing!\n");
}
