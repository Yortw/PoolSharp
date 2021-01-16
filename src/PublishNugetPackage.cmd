@echo off
echo Press any key to publish
pause
"..\.nuget\NuGet.exe" push PoolSharp.3.0.0.nupkg
pause