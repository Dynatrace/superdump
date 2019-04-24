REM Run the docker-windows-build-image and builds the Windows Services of Superdump
docker run -v %CD%\..:C:/superdump superdump:windows-build-image