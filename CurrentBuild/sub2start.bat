@echo off
cd C:\Program Files\Azure Kinect SDK v1.4.1\tools
k4arecorder.exe --device %2 --external-sync subordinate --imu OFF --sync-delay 360 %1

