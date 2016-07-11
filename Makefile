VVVV_BASEDIR=dotnet

BUILD=cd $(VVVV_BASEDIR) && ./build.sh

#              _ _      _
#  _ __   __ _| | | ___| |_
# | '_ \ / _` | | |/ _ \ __|
# | |_) | (_| | | |  __/ |_
# | .__/ \__,_|_|_|\___|\__|
# |_|

pallet:
	${BUILD} Pallet

pallet.tests:
	${BUILD} RunPalletTests

#              _   _
#  _ __   __ _| |_(_)_   _____
# | '_ \ / _` | __| \ \ / / _ \
# | | | | (_| | |_| |\ V /  __/
# |_| |_|\__,_|\__|_| \_/ \___|

runtests:
	${BUILD} RunTests

service:
	${BUILD} BuildDebugService

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
	${BUILD} BuildFrontend

web.tests.watch:
	${BUILD} WatchWebTests

web.tests.fsproj:
	${BUILD} BuildWebTestsFsProj

web.tests:
	${BUILD} BuildWebTests

worker.watch:
	${BUILD} WatchWorker

worker.fsproj:
	${BUILD} BuildWorkerFsProj

worker:
	${BUILD} BuildWorker

#        _ _
#   __ _| | |
#  / _` | | |
# | (_| | | |
#  \__,_|_|_|

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
