cd /d %~dp0

set msb="C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
@IF NOT EXIST %msb% @ECHO COULDN'T FIND MSBUILD: %msb% (Is VS2017 installed?)

%msb% msbuild.targets /l:FileLogger,Microsoft.Build.Engine;logfile=msbuild.log

REM ugly hack for a bug in VS2017 (SuperDumpService.xml documentation not included in publishing)
REM missing SuperDumpService.xml breaks swashbuckle
copy "..\src\SuperDumpService\bin\Release\net46\SuperDumpService.xml" "..\build\bin\SuperDumpService\SuperDumpService.xml"