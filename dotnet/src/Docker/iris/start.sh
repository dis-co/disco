#!/bin/sh

BASEDIR=$(dirname "$0")

export PATH=/usr/local/bin:/usr/bin/:/bin:/sbin

cd $BASEDIR

case $COMMAND in
    "tests")
	mono Iris.Tests.exe
	;;
    "interactive")
	if [ -z "$IRIS_NOWEB" ]; then
	    mono iris.exe start -i --dir=/project
	else
	    mono iris.exe start -i --no-http --dir=/project
	fi
	;;
    "start")
	if [ -z "$IRIS_NOWEB" ]; then
	    mono iris.exe start --dir=/project
	else
	    mono iris.exe start --no-http --dir=/project
	fi
	;;
    "create")
	mono iris.exe create \
	     --bind=$IRIS_BIND \
	     --dir=/project \
	     --git=$IRIS_GIT_PORT \
	     --web=$IRIS_WEB_PORT \
	     --ws=$IRIS_WS_PORT \
	     --raft=$IRIS_RAFT_PORT \
	     --name=$IRIS_NAME
	;;
    "shell")
	/bin/sh
	;;
    *)
	env
	;;
esac
