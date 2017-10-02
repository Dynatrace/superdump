@echo off
set VS_REGKEY="HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7"
set VS_VERSION="15.0"

cd /d %~dp0

for /F "usebackq skip=2 tokens=1-2*" %%A in (`REG QUERY %VS_REGKEY% /v %VS_VERSION% 2^>nul`) do (
    set VS_PATH=%%C
)

if not defined VS_PATH (
    echo No Visual Studio installation found!
    exit /B 1
)
echo Using Visual Studio installation found at %VS_PATH% for build

set MSBUILD="%VS_PATH%\MSBuild\15.0\Bin\msbuild.exe"
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
