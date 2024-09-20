@echo off
cd C:\Program Files\Azure Kinect SDK v1.4.2\tools
k4arecorder.exe --device %2 --external-sync master --imu OFF %1