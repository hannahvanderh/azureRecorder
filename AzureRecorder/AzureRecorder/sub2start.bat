@echo off
cd C:\Program Files\Azure Kinect SDK v1.4.1\tools
k4arecorder.exe --device 2 --external-sync subordinate --imu OFF -e -8 -r 5 --sync-delay 360 "C:\Users\pmhansen\Desktop\kinect test files\output-2.mkv"

