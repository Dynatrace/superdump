cd /d %~dp0

set msb="%VSINSTALLDIR%\MSBuild\15.0\Bin\MSBuild.exe"
@IF NOT EXIST %msb% @ECHO COULDN'T FIND MSBUILD: %msb% (Is VS2017 installed?)

set target="Windows"
if not "%1" == "" ( set target="%1" )

%msb% msbuild.targets /l:FileLogger,Microsoft.Build.Engine;logfile=msbuild.log /target:%target%

REM ugly hack for a bug in VS2017 (SuperDumpService.xml documentation not included in publishing)
REM missing SuperDumpService.xml breaks swashbuckle
copy "..\src\SuperDumpService\bin\Release\netcoreapp1.1\SuperDumpService.xml" "..\build\bin\SuperDumpService\SuperDumpService.xml"
