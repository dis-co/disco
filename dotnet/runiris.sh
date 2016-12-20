#!/bin/sh
subcommand=$1
shift
rest=$*
mono src/Iris/bin/Debug/Iris/iris.exe $subcommand --http=src/Iris/assets/frontend $rest
