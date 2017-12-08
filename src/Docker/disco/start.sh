#!/bin/sh

BASEDIR=$(dirname "$0")

export PATH=/usr/local/bin:/usr/bin/:/bin:/sbin

cd $BASEDIR

case $COMMAND in
    "tests")
        mono Disco.Tests.exe
        ;;
    "interactive")
        if [ -z "$DISCO_NOWEB" ]; then
            mono disco.exe start -i --dir=/project
        else
            mono disco.exe start -i --no-http --dir=/project
        fi
        ;;
    "start")
        if [ -z "$DISCO_NOWEB" ]; then
            mono disco.exe start --dir=/project
        else
            mono disco.exe start --no-http --dir=/project
        fi
        ;;
    "create")
        mono disco.exe create \
             --bind=$DISCO_BIND \
             --dir=/project \
             --git=$DISCO_GIT_PORT \
             --web=$DISCO_WEB_PORT \
             --ws=$DISCO_WS_PORT \
             --raft=$DISCO_RAFT_PORT \
             --name=$DISCO_NAME
        ;;
    "shell")
        /bin/sh
        ;;
    *)
        env
        ;;
esac
