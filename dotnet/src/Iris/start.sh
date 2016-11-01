#!/bin/sh

BASEDIR=$(dirname "$0")

export PATH=/usr/local/bin:/usr/bin/:/bin:/sbin

cd $BASEDIR

mono iris.exe start --dir=/project
