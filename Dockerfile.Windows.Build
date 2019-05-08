FROM mcr.microsoft.com/windows/servercore:1809

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'silentlyContinue';"]

# Install .NET Core SDK
ENV DOTNET_SDK_VERSION 3.0.100-preview5-011568
RUN Invoke-WebRequest -OutFile dotnet.zip https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$Env:DOTNET_SDK_VERSION/dotnet-sdk-$Env:DOTNET_SDK_VERSION-win-x64.zip; \
    Expand-Archive dotnet.zip -DestinationPath 'C:\Program Files\dotnet'; \
    Remove-Item -Force dotnet.zip
RUN setx /M PATH $($Env:PATH + ';' + $Env:ProgramFiles + '\dotnet')

# Install .Net SDK
ENV DOTNET_SDK_DOWNLOAD_URL https://download.visualstudio.microsoft.com/download/pr/7afca223-55d2-470a-8edc-6a1739ae3252/c8c829444416e811be84c5765ede6148/ndp48-devpack-enu.exe
RUN Invoke-WebRequest $Env:DOTNET_SDK_DOWNLOAD_URL -OutFile DotNetSDK.exe; \
    start-Process DotNetSDK.exe -ArgumentList '/q' -Wait ; \
	Remove-Item -Force DotNetSDK.exe

# Install Windows Debugging Tools
ENV WINDBG_DOWNLOAD_URL https://download.microsoft.com/download/F/9/1/F91B5312-4385-4476-9688-055E3B1ED10F/windowssdk/winsdksetup.exe
RUN Invoke-WebRequest $Env:WINDBG_DOWNLOAD_URL -OutFile winsdksetup.exe; \
    start-Process winsdksetup.exe -ArgumentList '/features OptionId.WindowsDesktopDebuggers /q' -Wait ; \
    Remove-Item -Force winsdksetup.exe

# Install DebugDiag
ENV DEBUGDIAG_DOWNLOAD_URL https://download.microsoft.com/download/D/C/9/DC98BD0E-5A9A-4D8A-B313-22BC3604FB05/DebugDiagx64.msi
RUN Invoke-WebRequest $Env:DEBUGDIAG_DOWNLOAD_URL -OutFile DebugDiagx64.msi; \
    start-Process DebugDiagx64.msi -ArgumentList '/qn' -Wait ; \
    Remove-Item -Force DebugDiagx64.msi

# Trigger the population of the local package cache
ENV NUGET_XMLDOC_MODE skip
RUN New-Item -Type Directory warmup; \
    cd warmup; \
    dotnet new console; \
    cd ..; \
    Remove-Item -Force -Recurse warmup

# Install nodejs and bower which is used in the Prepublish Script of SuperDumpService
ENV NODEJS_DOWNLOAD_URL https://nodejs.org/dist/v10.15.3/node-v10.15.3-x64.msi
RUN Invoke-WebRequest $Env:NODEJS_DOWNLOAD_URL -OutFile nodejs.msi; \
    start-Process nodejs.msi -ArgumentList '/qn' -Wait ; \
    Remove-Item -Force nodejs.msi
RUN npm install -g bower \
	npm install -g typescript
	
# Install Visual Studio Build Tools
ENV STUDIOS_INSTALLER_DOWNLOAD_URL https://download.visualstudio.microsoft.com/download/pr/a08183e4-3087-4df5-a074-d3bdf1ad5eb8/20816d670f7909277d9793dc3e80b3c2/vs_buildtools.exe
RUN Invoke-WebRequest $Env:VSTUDIO_INSTALLER_DOWNLOAD_URL -OutFile vs_buildtools.exe; \
	start-Process vs_buildtools.exe -ArgumentList '--quiet --norestart --nocache --add Microsoft.VisualStudio.Workload.MSBuildTools --add Microsoft.VisualStudio.Workload.NetCoreBuildTools' -Wait; \
    Remove-Item -Force vs_buildtools.exe


VOLUME C:/superdump

# Build Super Dump
ENV MSBUILD "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\amd64\MSBuild.exe"
ENV TARGET "Windows"
CMD cd C:/superdump/building; \
	dotnet restore ..\src; \
	& $Env:MSBUILD msbuild.targets /l:'FileLogger,Microsoft.Build.Engine;logfile=msbuild.log' /target:$Env:TARGET #; \
	#copy '..\src\SuperDumpService\bin\Release\netcoreapp3.0\SuperDumpService.xml' '..\build\bin\SuperDumpService\SuperDumpService.xml'