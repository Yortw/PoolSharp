@echo off
echo Press any key to publish
pause
"..\.nuget\NuGet.exe" push PoolSharp.3.0.1.nupkg -Source https://www.nuget.org/api/v2/package
pause