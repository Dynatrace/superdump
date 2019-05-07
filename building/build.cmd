@echo off
cd /d %~dp0

set MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\amd64\MSBuild.exe"
set target="Windows"
if not "%1" == "" ( set target="%1" )

if exist %MSBUILD% (
    %MSBUILD% msbuild.targets /l:FileLogger,Microsoft.Build.Engine;logfile=msbuild.log /target:%target%
    REM ugly hack for a bug in VS2017 (SuperDumpService.xml documentation not included in publishing)
    REM missing SuperDumpService.xml breaks swashbuckle
    copy "..\src\SuperDumpService\bin\Release\netcoreapp2.0\SuperDumpService.xml" "..\build\bin\SuperDumpService\SuperDumpService.xml"
) else (
    echo Could not find msbuild.exe at %MSBUILD% (Is Visual Studio installed?)
    exit /B 1
)
