@echo off

IF "%~1" == "help" (
  echo "Usage:"
  echo "    %~0"
  echo "    %~0 run"
  echo ""
  echo "Build Sequence"
  echo "   cd ..\src"
  echo "   dotnet build"
  echo ""
  echo "Run Sequence"
  echo "   cd ..\src"
  echo "   dotnet run -- -b ..\examples\standard -style-dir ..\style_configs\"
  echo ""
  exit
)

IF "%~1" == "run" (
  cd .\src
  dotnet run -- -b ..\examples\standard -style-dir ..\style_configs\
  exit
)

cd .\src
dotnet build
