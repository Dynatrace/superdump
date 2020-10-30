FROM mcr.microsoft.com/dotnet/framework/runtime:4.8

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Install .NET Core SDK (x86)
ENV DOTNET_DOWNLOAD_URL https://download.visualstudio.microsoft.com/download/pr/4503f532-215f-4300-b3b4-eb3ccd3c6ff3/79045e530ca8368c02ba9a2b764be28a/dotnet-sdk-5.0.100-rc.2.20479.15-win-x86.zip

RUN Invoke-WebRequest $Env:DOTNET_DOWNLOAD_URL -OutFile dotnet.zip; \
    Expand-Archive dotnet.zip -DestinationPath 'C:\Program Files (x86)\dotnet'; \
    Remove-Item -Force dotnet.zip

# Install .NET Core SDK (x64)
ENV ASPNETCORE_DOWNLOAD_URL https://download.visualstudio.microsoft.com/download/pr/5208bb4a-0704-44c2-b8c1-95871f23f2c9/7c0c1fded168bb35ff4ddd5c3fb85736/aspnetcore-runtime-5.0.0-rc.2.20475.17-win-x64.zip

RUN Invoke-WebRequest $Env:ASPNETCORE_DOWNLOAD_URL -OutFile dotnet.zip; \
    Expand-Archive -Force dotnet.zip -DestinationPath $Env:ProgramFiles\dotnet; \
    Remove-Item -Force dotnet.zip

RUN setx /M PATH $($Env:PATH + ';' + $Env:ProgramFiles + '\dotnet')

# Install Windows Debugging Tools
ENV WINDBG_DOWNLOAD_URL https://download.microsoft.com/download/F/9/1/F91B5312-4385-4476-9688-055E3B1ED10F/windowssdk/winsdksetup.exe

RUN Invoke-WebRequest $Env:WINDBG_DOWNLOAD_URL -OutFile winsdksetup.exe; \
    start-Process winsdksetup.exe -ArgumentList '/features OptionId.WindowsDesktopDebuggers /q' -Wait ; \
    Remove-Item -Force winsdksetup.exe

# Install DebugDiag 2.2
ENV DEBUGDIAG_DOWNLOAD_URL https://download.microsoft.com/download/D/C/9/DC98BD0E-5A9A-4D8A-B313-22BC3604FB05/DebugDiagx64.msi

RUN Invoke-WebRequest $Env:DEBUGDIAG_DOWNLOAD_URL -OutFile DebugDiagx64.msi; \
    start-Process DebugDiagx64.msi -ArgumentList '/qn' -Wait ; \
    Remove-Item -Force DebugDiagx64.msi

# default MS symbol servers. override this with your own list if necessary.
RUN New-Item -ItemType directory -Path C:\symbols
ENV _NT_SYMBOL_PATH srv*c:\\symbols*https://msdl.microsoft.com/download/symbols;srv*c:\\symbols*http://referencesource.microsoft.com/symbols
# needed, otherwise symbol servers are not reachable
RUN "reg.exe" "add \"HKLM\Software\Microsoft\Symbol Server\" /v NoInternetProxy /t REG_DWORD /d 1 /f"

# setup superdump
RUN mkdir C:/superdump
COPY . C:/superdump

# volumes
VOLUME C:/superdump/data/dumps

# run
ENV ASPNETCORE_URLS http://*:80
WORKDIR C:/superdump/bin/SuperDumpService
CMD dotnet SuperDumpService.dll
