@echo off
set "CORE_RUNNER_PROJECT=%~dp0"
set "CORE_RUNNER_PROJECT=%CORE_RUNNER_PROJECT:~0,-1%"
start "" "D:\unity\6000.0.23f1\Editor\Unity.exe" -projectPath "%CORE_RUNNER_PROJECT%"
