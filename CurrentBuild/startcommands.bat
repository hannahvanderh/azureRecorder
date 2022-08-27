@echo off
start "sub1" cmd /k call "sub1start.bat"
start "sub2" cmd /k call "sub2start.bat"
start "master" cmd /k call masterstart.bat