@echo off
taskkill /FI "WindowTitle eq sub1*" /T /F
taskkill /FI "WindowTitle eq sub2*" /T /F
taskkill /FI "WindowTitle eq master*" /T /F