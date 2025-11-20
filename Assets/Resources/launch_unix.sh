#!/bin/bash
# ==========================================================================
# Respire Python Script Launcher for macOS and Linux
# ==========================================================================
# This script launches the Python process for the Respire project.
# The Python process will continue running independently from Unity.
#
# Usage: ./launch_python.sh
# ==========================================================================

echo "Starting Python Script for Respire..."

# Python script path: C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\src\gdx_respire.py
# Conda environment: C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda

# Set up environment variables
export PATH="C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda/bin:$PATH"
export PYTHONPATH="C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda/lib:$PYTHONPATH"

# Check if Python is available
if [ ! -f "C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda\bin\python" ]; then
    echo "ERROR: Python not found in conda environment."
    echo "Expected path: C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda\bin\python"
    read -p "Press any key to continue..."
    exit 1
fi

# Check if the Python script exists
if [ ! -f "C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\src\gdx_respire.py" ]; then
    echo "ERROR: Python script not found."
    echo "Expected path: C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\src\gdx_respire.py"
    read -p "Press any key to continue..."
    exit 1
fi

# Run the Python script with arguments
echo "Launching: 'C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda\bin\python' 'C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\src\gdx_respire.py' --barebone --verbose"
"C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\.conda\bin\python" "C:\Users\win11\sfu_breathing\upm_vernier_sensor\respire2_python\src\gdx_respire.py" --barebone --verbose

# If we get an error, pause to show the message
if [ $? -ne 0 ]; then
    echo "ERROR: Python script exited with error code $?"
    read -p "Press any key to continue..."
    exit 1
fi

echo "Python script launched successfully. It will continue running in the background."
exit 0