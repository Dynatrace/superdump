FROM microsoft/dotnet-framework:4.6.2

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Install .NET Core SDK
ENV DOTNET_SDK_VERSION 2.0.0
ENV DOTNET_SDK_DOWNLOAD_URL https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$DOTNET_SDK_VERSION/dotnet-sdk-$DOTNET_SDK_VERSION-win-x64.zip
ENV DOTNET_SDK_DOWNLOAD_SHA C0942299437541C6B173F71213B43D2CC1E0EBC5C75F78948C0C6279A8D1C5B293999C93863392A4219F819B1DDD73D95112930B08EC8DDAA89918931E492DB0

RUN Invoke-WebRequest $Env:DOTNET_SDK_DOWNLOAD_URL -OutFile dotnet.zip; \
    if ((Get-FileHash dotnet.zip -Algorithm sha512).Hash -ne $Env:DOTNET_SDK_DOWNLOAD_SHA) { \
        Write-Host 'CHECKSUM VERIFICATION FAILED!'; \
        exit 1; \
    }; \
    \
    Expand-Archive dotnet.zip -DestinationPath $Env:ProgramFiles\dotnet; \
    Remove-Item -Force dotnet.zip

RUN setx /M PATH $($Env:PATH + ';' + $Env:ProgramFiles + '\dotnet')

# Trigger the population of the local package cache
ENV NUGET_XMLDOC_MODE skip
RUN New-Item -Type Directory warmup; \
    cd warmup; \
    dotnet new; \
    cd ..; \
    Remove-Item -Force -Recurse warmup

# ASP.NET Core
ENV ASPNETCORE_RUNTIMESTORE_DOWNLOAD_URL https://dist.asp.net/runtimestore/2.0.0/win-x64/aspnetcore.runtimestore.zip

# set up the runtime store
RUN Write-Host \"Downloading and extracting ${env:ASPNETCORE_RUNTIMESTORE_DOWNLOAD_URL}\"; \
    Invoke-WebRequest $env:ASPNETCORE_RUNTIMESTORE_DOWNLOAD_URL -OutFile cache.zip; \
    $env:DOTNET_HOME = $(Split-Path -Parent (Get-Command dotnet.exe).Source); \
    Expand-Archive -Force cache.zip -DestinationPath $env:DOTNET_HOME; \
    Remove-Item -Force cache.zip

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
RUN mkdir C:/symbols
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
