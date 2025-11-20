@echo off
:: =========================================================================
:: Respire Python Script Launcher for Windows
:: =========================================================================
:: This script launches the Python process for the Respire project.
:: The Python process will continue running independently from Unity.
::
:: Usage: launch_python.bat
:: =========================================================================

echo Starting Python Script for Respire...

:: Python script path: C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\src\gdx_respire.py
:: Conda environment: C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda

:: Set up environment variables
set "PATH=C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda;C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda\Scripts;C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda\Library\bin;%PATH%"

:: Check if Python is available
if not exist "C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda\python.exe" (
    echo ERROR: Python not found in conda environment.
    echo Expected path: C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda\python.exe
    pause
    exit /b 1
)

:: Check if the Python script exists
if not exist "C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\src\gdx_respire.py" (
    echo ERROR: Python script not found.
    echo Expected path: C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\src\gdx_respire.py
    pause
    exit /b 1
)

:: Run the Python script with arguments
echo Launching: "C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda\python.exe" "C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\src\gdx_respire.py" --barebone --verbose
"C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda\python.exe" "C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\src\gdx_respire.py" --barebone --verbose

:: If we get an error, pause to show the message
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Python script exited with error code %ERRORLEVEL%
    pause
)

echo Python script launched successfully. It will continue running in the background.
exit /b 0