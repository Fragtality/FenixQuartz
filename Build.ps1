$projdir = "C:\Users\Fragtality\source\repos\FenixQuartz"
$instBinDir = $projdir + "\Installer\bin\Release\app.publish"
$version = "v1.8.0"

#Create Lock
cd $projdir
if (-not (Test-Path -Path "build.lck")) {
	"lock" | Out-File -File "build.lck"
}
else {
	exit 0
}

cd "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64"
#WT2GSX
Write-Host "Building FenixQuartz"
.\msbuild.exe ($projdir + "\FenixQuartz.sln") /t:FenixQuartz:rebuild /p:Configuration="Release" /p:BuildProjectReferences=false | Out-Null

#Installer
Write-Host "Building Installer"
.\msbuild.exe ($projdir + "\FenixQuartz.sln") /t:Installer:rebuild /p:Configuration="Release" /p:BuildProjectReferences=false | Out-Null

Copy-Item -Path ($instBinDir + "\Installer.exe") -Destination ($projdir + "\Releases\FenixQuartz-Installer-" + $version + ".exe") -Force

#Remove lock
cd $projdir
if ((Test-Path -Path "build.lck")) {
	Remove-Item -Path "build.lck"
}