del /F /Q /S *.CodeAnalysisLog.xml

"..\.nuget\NuGet.exe" pack -sym PoolSharp.nuspec -BasePath .\
pause