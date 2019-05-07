REM Builds a docker image that contains all neccessary programs to build the Windows Part of SuperDump
docker build -f ..\Dockerfile.Windows.Build -t superdump:windows-build-image ..\