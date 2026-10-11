@echo off
if not exist "%~dp0Builds\CoreboundWindows\Corebound.exe" (
  echo Build the game first using Corebound ^> Build Windows in Unity.
  pause
  exit /b 1
)
start "" "%~dp0Builds\CoreboundWindows\Corebound.exe"
