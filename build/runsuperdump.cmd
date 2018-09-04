cd /d %~dp0

REM adapt port
SET ASPNETCORE_URLS=https://*:5001;http://*:5000

REM adapt asp.netcore environment. "Development" yields better error pages, "Production" is recommended otherwise.
SET ASPNETCORE_ENVIRONMENT=Development

REM adapt _NT_SYMBOL_PATH here to include internal symbol servers
REM be aware that CLRMD is buggy parsing more than two symbolpaths. (see https://github.com/Microsoft/clrmd/issues/52)
REM a working symbolpath with three servers can work like that: _NT_SYMBOL_PATH=srv*c:\symbols*\\my-server-1\symstore;srv*c:\symbols*\\my-server-1\symstore;srv*c:\symbols*https://msdl.microsoft.com/download/symbols
set _NT_SYMBOL_PATH=srv*c:\symbols*https://msdl.microsoft.com/download/symbols

cd bin
REM Elastic Search & Gotty Setup
REM set ELASTIC_DIR=C:\superdump\data\elastic
REM set DUMP_DIR=C:\superdump\data\dumps
REM set DEBUG_SYMBOL_DIR=C:\superdump\data\debug

REM Please note that source file handling is Dynatrace specific. You'll probably need to make modifications to use it.
REM set REPOSITORY_URL=
REM set REPOSITORY_AUTH=
REM docker-compose -f docker-compose-elastic.yaml -p elastic up -d
REM docker-compose -f docker-compose-gotty.yaml -p gotty up -d

cd SuperDumpService
call dotnet SuperDumpService.dll