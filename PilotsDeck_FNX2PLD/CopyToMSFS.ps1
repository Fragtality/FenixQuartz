# powershell -ExecutionPolicy Unrestricted -file "$(ProjectDir)CopyToMSFS.ps1" $(ConfigurationName)
$buildConfiguration = $args[0]
$bindir = "C:\Users\Fragtality\source\repos\PilotsDeck_FNX\PilotsDeck_FNX2PLD\bin\Release\net6.0-windows"
$destDir = "F:\MSFS2020\PilotsDeck_FNX"

if ($buildConfiguration -eq "Release") {
	Write-Host "Copy Binaries ..."
	Copy-Item -Path ($bindir + "\*") -Destination $destDir -Recurse -Force
	#Start-Process -FilePath ($destDir + "\PilotsDeck_FNX2PLD.exe") -WorkingDirectory $destDir
}


exit 0
