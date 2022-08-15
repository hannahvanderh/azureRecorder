@echo off
cd C:\Program Files\Azure Kinect SDK v1.4.1\tools
::k4arecorder.exe --device 1 --external-sync subordinate --imu OFF -e -8 -r 5 --sync-delay 180 "C:\Users\pmhansen\Desktop\kinect test files\output-0.mkv"
k4arecorder.exe --device 1 --external-sync subordinate --imu OFF --sync-delay 180 %1

