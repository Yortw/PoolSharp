@echo off
echo Press any key to publish
pause
"..\.nuget\NuGet.exe" push PoolSharp.2.0.3.nupkg
pause