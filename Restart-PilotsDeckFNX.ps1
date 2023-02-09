$destDir = "X:\Full\Path"
Stop-Process -Name PilotsDeck_FNX2PLD -ErrorAction SilentlyContinue
Sleep(3)
Start-Process -FilePath ($destDir + "\PilotsDeck_FNX2PLD.exe") -WorkingDirectory $destDir