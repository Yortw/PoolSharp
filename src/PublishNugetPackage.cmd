@echo off
echo Press any key to publish
pause
".nuget\NuGet.exe" push PoolSharp.1.0.0.10.nupkg
pause