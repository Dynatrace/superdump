REM adapt port
SET ASPNETCORE_URLS=http://*:5000

REM adapt asp.netcore environment. "Development" yields better error pages, "Production" is recommended otherwise.
SET ASPNETCORE_ENVIRONMENT=Development

REM adapt _NT_SYMBOL_PATH here to include internal symbol servers
REM be aware that CLRMD is buggy parsing more than two symbolpaths. (see https://github.com/Microsoft/clrmd/issues/52)
REM a working symbolpath with three servers can work like that: _NT_SYMBOL_PATH=srv*c:\symbols*\\my-server-1\symstore;srv*c:\symbols*\\my-server-1\symstore;srv*c:\symbols*https://msdl.microsoft.com/download/symbols
set _NT_SYMBOL_PATH=srv*c:\symbols*https://msdl.microsoft.com/download/symbols

cd bin\SuperDumpService
call dotnet SuperDumpService.dll