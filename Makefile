WWW_BASEDIR=www
VVVV_BASEDIR=vvvv/plugins
DOC_BASEDIR=wiki/docs
OPTS=/nologo /verbosity:minimal /p:Configuration=Release
RELEASE_DIR=tmp/Release
VERSTR := $(shell head -n1 $(VVVV_BASEDIR)/CHANGELOG.md | sed -e 's/[^0-9\.]//g')
MANIFEST="Iris Version: $(VERSTR)"

DEVBUILD=xbuild /nologo /p:Configuration=Debug

debug.nodes:
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris.Nodes/Iris.Nodes.fsproj

debug.core:
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris.Core/Iris.Core.fsproj

debug.web:
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris.Web/Iris.Web.fsproj

debug.web.core:
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris.Web.Core/Iris.Web.Core.fsproj

debug.web.tests:
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris.Web.Tests/Iris.Web.Tests.fsproj

debug.web.worker:
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris.Web.Worker/Iris.Web.Worker.fsproj

debug.vsync:
	${DEVBUILD} ${VVVV_BASEDIR}/src/Vsync/Vsync.csproj

debug.tests:
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris.Tests/Iris.Tests.fsproj

debug.service:
	${DEVBUILD} ${VVVV_BASEDIR}/src/Iris.Service/Iris.Service.fsproj

debug.all:
	${DEVBUILD} ${VVVV_BASEDIR}/Iris.sln

clean:
	@rm -f Iris-*.zip
	@rm -rf $(VVVV_BASEDIR)/build/*
	@rm -rf tmp/*
	@find ${VVVV_BASEDIR} -type d -name bin -prune -exec rm -rf '{}' \;
	@find ${VVVV_BASEDIR} -type d -name obj -prune -exec rm -rf '{}' \;


release.all: release.prepare clean release.build release.copy

release.copy: zip
	@cp $(RELEASE_DIR)/$(VERSTR)/x86/Iris-$(VERSTR)_x86.zip .
	@cp $(RELEASE_DIR)/$(VERSTR)/x64/Iris-$(VERSTR)_x64.zip .

release.release: build
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
