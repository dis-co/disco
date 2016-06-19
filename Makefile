WWW_BASEDIR=www
VVVV_BASEDIR=dotnet
DOC_BASEDIR=wiki/docs
OPTS=/nologo /verbosity:minimal /p:Configuration=Release
RELEASE_DIR=tmp/Release
VERSTR := $(shell head -n1 $(VVVV_BASEDIR)/CHANGELOG.md | sed -e 's/[^0-9\.]//g')
MANIFEST="Iris Version: $(VERSTR)"

DEVBUILD=xbuild /nologo /p:Configuration=Debug
JSBUILD=cd $(VVVV_BASEDIR) && npm run

#              _ _      _
#  _ __   __ _| | | ___| |_
# | '_ \ / _` | | |/ _ \ __|
# | |_) | (_| | | |  __/ |_
# | .__/ \__,_|_|_|\___|\__|
# |_|

pallet:
	$(DEVBUILD) $(VVVV_BASEDIR)/src/Pallet/Pallet.fsproj

pallet.tests:
	$(DEVBUILD) $(VVVV_BASEDIR)/src/Pallet.Tests/Pallet.Tests.fsproj
	@sh -c 'fsi $(VVVV_BASEDIR)/src/Pallet.Tests/run.fsx'

#              _   _
#  _ __   __ _| |_(_)_   _____
# | '_ \ / _` | __| \ \ / / _ \
# | | | | (_| | |_| |\ V /  __/
# |_| |_|\__,_|\__|_| \_/ \___|

tests: prepare.serialization
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris/Tests.fsproj

service: prepare.serialization
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris/Service.fsproj

nodes: prepare.serialization
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris/Nodes.fsproj

#   __                 _                 _
#  / _|_ __ ___  _ __ | |_ ___ _ __   __| |
# | |_| '__/ _ \| '_ \| __/ _ \ '_ \ / _` |
# |  _| | | (_) | | | | ||  __/ | | | (_| |
# |_| |_|  \___/|_| |_|\__\___|_| |_|\__,_|

frontend: prepare.serialization
	${JSBUILD} build-frontend

frontend.watch: prepare.serialization
	${JSBUILD} watch-frontend

web.tests: prepare.serialization
	${JSBUILD} build-tests

web.tests.watch: prepare.serialization
	${JSBUILD} watch-tests

worker: prepare.serialization
	${JSBUILD} build-worker

worker.watch: prepare.serialization
	${JSBUILD} watch-worker

#        _ _
#   __ _| | |
#  / _` | | |
# | (_| | | |
#  \__,_|_|_|

debug.all: prepare.serialization tests nodes frontend web.tests worker service

sln: prepare.serialization
	${DEVBUILD} ${VVVV_BASEDIR}/Iris.sln

clean:
	@make -f ${VVVV_BASEDIR}/src/Iris/Serialization/Serialization.mk clean
	@rm -f Iris-*.zip
	@rm -rf $(VVVV_BASEDIR)/build/*
	@rm -rf tmp/*
	@find ${VVVV_BASEDIR} -type d -name bin -prune -exec rm -rf '{}' \;
	@find ${VVVV_BASEDIR} -type d -name obj -prune -exec rm -rf '{}' \;

#           _
#  _ __ ___| | ___  __ _ ___  ___
# | '__/ _ \ |/ _ \/ _` / __|/ _ \
# | | |  __/ |  __/ (_| \__ \  __/
# |_|  \___|_|\___|\__,_|___/\___|

release.all: release.prepare clean release.build release.copy

release.copy: zip
	@cp $(RELEASE_DIR)/$(VERSTR)/x86/Iris-$(VERSTR)_x86.zip .
	@cp $(RELEASE_DIR)/$(VERSTR)/x64/Iris-$(VERSTR)_x64.zip .

release.release: build
	@echo "FIXME"
	@exit 1
	@echo "building release packages"
	@mkdir -p $(RELEASE_DIR)/$(VERSTR)/x86/Iris/nodes $(RELEASE_DIR)/$(VERSTR)/x64/Iris
	@echo "copying nodes"
	@cp -r $(VVVV_BASEDIR)/Nodes/bin/x86/Release $(RELEASE_DIR)/$(VERSTR)/x86/Iris/Nodes
	@cp -r $(VVVV_BASEDIR)/Nodes/bin/x64/Release $(RELEASE_DIR)/$(VERSTR)/x64/Iris/Nodes
	@echo "copying front-end server"
	@cp -r $(VVVV_BASEDIR)/FrontEnd/bin/Release/x86 $(RELEASE_DIR)/$(VERSTR)/x86/Iris/FrontEnd
	@cp -r $(VVVV_BASEDIR)/FrontEnd/bin/Release/x64 $(RELEASE_DIR)/$(VERSTR)/x64/Iris/FrontEnd
	@echo "copying www assets"
	@cp -r $(WWW_BASEDIR)/dist $(RELEASE_DIR)/$(VERSTR)/x86/Iris/www
	@cp -r $(WWW_BASEDIR)/dist $(RELEASE_DIR)/$(VERSTR)/x64/Iris/www

release.manifest: release
	@echo "creating manifest file"
	@echo  $(MANIFEST) > $(RELEASE_DIR)/$(VERSTR)/x86/Iris/manifest-$(VERSTR).txt
	@echo  $(MANIFEST) > $(RELEASE_DIR)/$(VERSTR)/x64/Iris/manifest-$(VERSTR).txt

release.documentation: release
	@make -C $(DOC_BASEDIR)
	@cp $(DOC_BASEDIR)/documentation.pdf $(RELEASE_DIR)/$(VERSTR)/x86/Iris
	@cp $(DOC_BASEDIR)/documentation.pdf $(RELEASE_DIR)/$(VERSTR)/x64/Iris

release.zip: documentation manifest
	@echo "zipping packages"
	@cd $(RELEASE_DIR)/$(VERSTR)/x86/; zip -qr Iris-$(VERSTR)_x86.zip Iris
	@cd $(RELEASE_DIR)/$(VERSTR)/x64/; zip -qr Iris-$(VERSTR)_x64.zip Iris

release.prepare:
	@mkdir -p $(RELEASE_DIR)
	@echo "Building Iris" $(VERSTR)

release.build:
	@echo "building x86"
	@cd $(VVVV_BASEDIR); xbuild Iris.sln $(OPTS) /p:Platform=x86
	@echo "building x64"
	@cd $(VVVV_BASEDIR); xbuild Iris.sln $(OPTS) /p:Platform=x64

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

#   __
#  / _|___ _____ __ ___   __ _
# | |_/ __|_  / '_ ` _ \ / _` |
# |  _\__ \/ /| | | | | | (_| |
# |_| |___/___|_| |_| |_|\__, |
#                           |_|

fszmq.debug:
	$(DEVBUILD) $(VVVV_BASEDIR)/src/fszmq/fszmq.fsproj

prepare.serialization:
	@make -f ${VVVV_BASEDIR}/src/Iris/Serialization.mk all
