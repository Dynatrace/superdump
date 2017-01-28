cd /d %~dp0

set msb="C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
@IF NOT EXIST %msb% @ECHO COULDN'T FIND MSBUILD: %msb% (Is .NET 4 installed?)

%msb% msbuild.targets /l:FileLogger,Microsoft.Build.Engine;logfile=msbuild.log