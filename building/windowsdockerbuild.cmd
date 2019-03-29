@echo off

set MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe"
set target="Windows"

if exist %MSBUILD% (
    dotnet restore ..\src
    %MSBUILD% msbuild.targets /l:FileLogger,Microsoft.Build.Engine;logfile=msbuild.log /target:%target%
    REM ugly hack for a bug in VS2017 (SuperDumpService.xml documentation not included in publishing)
    REM missing SuperDumpService.xml breaks swashbuckle
    REM copy "..\src\SuperDumpService\bin\Release\netcoreapp2.0\SuperDumpService.xml" "..\build\bin\SuperDumpService\SuperDumpService.xml"
) else (
    echo Could not find msbuild.exe at %MSBUILD% (Is Visual Studio installed?)
    exit /B 1
)
