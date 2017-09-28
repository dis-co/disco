#!/bin/sh
subcommand=$1
shift
rest=$*
mono Iris/iris.exe $subcommand --http=Iris/assets $rest
