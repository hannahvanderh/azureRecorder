@echo off
for /l %%x in (0,1,10000) do (
echo %%x
TIMEOUT 1 /nobreak
)