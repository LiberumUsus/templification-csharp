#!/bin/bash

if [ "$1" == "help" ]
then
    echo "Usage:"
    echo "    $0"
    echo "    $0 run"
    echo ""
    echo "Build Sequence"
    echo "   cd ./src"
    echo "   dotnet build"
    echo ""
    echo "Run Sequence"
    echo "   cd ./src"
    echo "   dotnet run -- -b ../examples/standard -style-dir ../style_configs/"
    echo ""
    exit
fi



if [ "$1" == "run" ]
then
    cd ./src
    dotnet run -- -b ../examples/standard -style-dir ../style_configs/
    exit
fi

# Else build
cd ./src
dotnet build
