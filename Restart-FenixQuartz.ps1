$destDir = "X:\Full\Path"
Stop-Process -Name FenixQuartz -ErrorAction SilentlyContinue
Sleep(3)
Start-Process -FilePath ($destDir + "\FenixQuartz.exe") -WorkingDirectory $destDir