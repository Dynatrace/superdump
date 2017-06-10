FROM microsoft/dotnet-framework:4.6.2

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# Install .NET Core
ENV DOTNET_VERSION 1.1.2
ENV DOTNET_DOWNLOAD_URL https://dotnetcli.blob.core.windows.net/dotnet/release/1.1.0/Binaries/$DOTNET_VERSION/dotnet-win-x64.$DOTNET_VERSION.zip

RUN Invoke-WebRequest $Env:DOTNET_DOWNLOAD_URL -OutFile dotnet.zip; \
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


# setup superdump
RUN mkdir C:/superdump
COPY . C:/superdump
WORKDIR C:/superdump

CMD C:/superdump/runsuperdump.cmd

# TODO: volumes