@echo off
cd C:\Program Files\Azure Kinect SDK v1.4.1\tools
k4arecorder.exe --device 0 --external-sync master --imu OFF -e -8 -r 5 %1