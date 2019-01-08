FROM microsoft/dotnet-framework:4.6.2

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Install .NET Core SDK
ENV ASPNETCORE_VERSION 2.2.0
ENV ASPNETCORE_DOWNLOAD_URL https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/$ASPNETCORE_VERSION/aspnetcore-runtime-$ASPNETCORE_VERSION-win-x64.zip
ENV ASPNETCORE_DOWNLOAD_SHA 0159f27762a0dd1fb7f7e4f85259c145d8e6964289f7477d6e9d5c03898afb38dca010f900e3ccb28e282514835a66d5546bbc1542b9da8c92dd3d2759c507de

RUN Invoke-WebRequest $Env:ASPNETCORE_DOWNLOAD_URL -OutFile dotnet.zip; \
    if ((Get-FileHash dotnet.zip -Algorithm sha512).Hash -ne $Env:ASPNETCORE_DOWNLOAD_SHA) { \
        Write-Host 'CHECKSUM VERIFICATION FAILED!'; \
        exit 1; \
    }; \
    \
    Expand-Archive dotnet.zip -DestinationPath $Env:ProgramFiles\dotnet; \
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
