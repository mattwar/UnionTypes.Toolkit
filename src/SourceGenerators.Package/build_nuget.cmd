@echo off
if "%1"=="" goto usage
dotnet build -c:release -p:version=%1 -p:packageversion=%1
goto done

:usage
echo usage: build_nuget build_version

:done
