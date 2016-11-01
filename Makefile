VVVV_BASEDIR=dotnet

BUILD=cd $(VVVV_BASEDIR) && ./build.sh

CURRENT_DIR:=$(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))

#              _   _
#  _ __   __ _| |_(_)_   _____
# | '_ \ / _` | __| \ \ / / _ \
# | | | | (_| | |_| |\ V /  __/
# |_| |_|\__,_|\__|_| \_/ \___|

run.tests:
	@nix-shell shell.nix -A irisEnv --run "cd $(VVVV_BASEDIR) && ./build.sh RunTests"

service:
	${BUILD} BuildDebugService

core:
	${BUILD} BuildDebugCore

nodes:
	${BUILD} BuildDebugNodes

serialization:
	${BUILD} GenerateSerialization

#   __                 _                 _
#  / _|_ __ ___  _ __ | |_ ___ _ __   __| |
# | |_| '__/ _ \| '_ \| __/ _ \ '_ \ / _` |
# |  _| | | (_) | | | | ||  __/ | | | (_| |
# |_| |_|  \___/|_| |_|\__\___|_| |_|\__,_|

frontend.watch:
	${BUILD} WatchFrontend

frontend.fsproj:
	${BUILD} BuildFrontendFsProj

frontend:
	${BUILD} BuildFrontendDebug

web.tests.watch:
	${BUILD} WatchWebTests

web.tests.fsproj:
	${BUILD} BuildWebTestsFsProj

run.web.tests:
	@nix-shell ${CURRENT_DIR}/shell.nix -A irisEnv --run "cd $(VVVV_BASEDIR) && ./build.sh RunWebTests"

web.tests:
	${BUILD} BuildWebTests

worker.watch:
	${BUILD} WatchWorker

worker.fsproj:
	${BUILD} BuildWorkerFsProj

worker:
	${BUILD} BuildWorkerDebug

#        _ _
#   __ _| | |
#  / _` | | |
# | (_| | | |
#  \__,_|_|_|

tests.all:
	${BUILD} AllTests

debug.all:
	${BUILD} DebugAll

clean:
	${BUILD} Clean


#           _
#  _ __ ___| | ___  __ _ ___  ___
# | '__/ _ \ |/ _ \/ _` / __|/ _ \
# | | |  __/ |  __/ (_| \__ \  __/
# |_|  \___|_|\___|\__,_|___/\___|

release:
	${BUILD} Release

#      _          _ _
#  ___| |__   ___| | |
# / __| '_ \ / _ \ | |
# \__ \ | | |  __/ | |
# |___/_| |_|\___|_|_|

shell:
	@nix-shell ${CURRENT_DIR}/shell.nix -A irisEnv

fsi:
	@nix-shell ${CURRENT_DIR}/shell.nix -A irisEnv --run "fsi --use:dotnet/src/Iris/bin/Debug/Core/interactive.fsx"

#  ____             _
# |  _ \  ___   ___| | _____ _ __
# | | | |/ _ \ / __| |/ / _ \ '__|
# | |_| | (_) | (__|   <  __/ |
# |____/ \___/ \___|_|\_\___|_|

docker:
	${BUILD} DebugDocker

image: docker
	@sudo docker build ${CURRENT_DIR}/dotnet/src/Iris/bin/Debug/Iris/

start:
	@sudo docker run -i --rm --net=host \
		-v ${PROJECT}:/project \
		-e IRIS_NODE_ID=${IRIS_NODE_ID} \
		-p ${IRIS_GIT}:${IRIS_GIT} \
		-p ${IRIS_WEB}:${IRIS_WEB} \
		-p ${IRIS_WS}:${IRIS_WS} \
		-p ${IRIS_RAFT}:${IRIS_RAFT} \
		${IMAGE}

#              _        _
#  _ __   __ _| | _____| |_
# | '_ \ / _` | |/ / _ \ __|
# | |_) | (_| |   <  __/ |_
# | .__/ \__,_|_|\_\___|\__|
# |_|

paket.restore:
	@cd $(VVVV_BASEDIR); mono .paket/paket.exe restore

paket.update:
	@cd $(VVVV_BASEDIR); mono .paket/paket.exe update

paket.install:
	@cd $(VVVV_BASEDIR); mono .paket/paket.exe install
