#!/bin/sh
subcommand=$1
shift
rest=$*
mono Disco/disco.exe $subcommand --http=Disco/assets $rest
