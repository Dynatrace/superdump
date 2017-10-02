REM builds for windows and creates a docker images named "superdump:dev" from it
REM currently with docker, only windows analysis are supported
build Windows
docker build -t superdump:dev ../build