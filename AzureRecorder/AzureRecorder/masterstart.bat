@echo off
cd C:\Program Files\Azure Kinect SDK v1.4.1\tools
k4arecorder.exe --device %2 --external-sync master --imu OFF %1