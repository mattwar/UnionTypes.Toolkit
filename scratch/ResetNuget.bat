rd %USERPROFILE%\.nuget\packages\uniontypes.toolkit.generator\0.0.0 /S /Q
dir %USERPROFILE%\.nuget\packages\uniontypes.toolkit.generator
dotnet restore --force-evaluate Scratch.csproj
dir %USERPROFILE%\.nuget\packages\uniontypes.toolkit.generator