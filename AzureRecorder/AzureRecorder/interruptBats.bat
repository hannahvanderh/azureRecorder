@echo off
::auto-answering the terminate prompt
rem Bypass "Terminate Batch Job" prompt.
if "%~1"=="-FIXED_CTRL_C" (
   REM Remove the -FIXED_CTRL_C parameter
   SHIFT
) ELSE (
   REM Run the batch with <NUL and -FIXED_CTRL_C
   CALL <NUL %0 -FIXED_CTRL_C %*
   GOTO :EOF
)
::go to location of windows-kill.exe
cd C:\ProgramData\chocolatey\lib\windows-kill\tools\windows-kill_x64_1.1.4_lib_release
::get the PIDs of k4arecorder.exe and call windows-kill on them
for /F "tokens=2" %%K in ('
   tasklist /FI "ImageName eq k4arecorder.exe" /FO LIST ^| findstr /B "PID:"
') do (
   ::echo %%K
   windows-kill -SIGINT %%K
   ::echo
)
::close cmd windows
PING localhost -n 6 >NUL
taskkill /FI "WindowTitle eq sub1*" /T
taskkill /FI "WindowTitle eq sub2*" /T
taskkill /FI "WindowTitle eq master*" /T