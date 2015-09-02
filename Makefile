WWW_BASEDIR=www
VVVV_BASEDIR=vvvv/plugins
DOC_BASEDIR=wiki/docs
OPTS=/nologo /verbosity:minimal /p:Configuration=Release
X86=/p:Platform=x86
X64=/p:Platform=x64
RELEASE_DIR=tmp/Release
VERSTR := $(shell head -n1 ./CHANGELOG.md | sed -e 's/[^0-9\.]//g')
MANIFEST="Iris Version: $(VERSTR)"

all: prepare clean build copy done

copy: zip
	@cp $(RELEASE_DIR)/$(VERSTR)/x86/Iris-$(VERSTR)_x86.zip .
	@cp $(RELEASE_DIR)/$(VERSTR)/x64/Iris-$(VERSTR)_x64.zip .

release: build
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

manifest: release
	@echo "creating manifest file"
	@echo  $(MANIFEST) > $(RELEASE_DIR)/$(VERSTR)/x86/Iris/manifest-$(VERSTR).txt
	@echo  $(MANIFEST) > $(RELEASE_DIR)/$(VERSTR)/x64/Iris/manifest-$(VERSTR).txt

documentation: release
	@make -C $(DOC_BASEDIR)
	@cp $(DOC_BASEDIR)/documentation.pdf $(RELEASE_DIR)/$(VERSTR)/x86/Iris
	@cp $(DOC_BASEDIR)/documentation.pdf $(RELEASE_DIR)/$(VERSTR)/x64/Iris

zip: documentation manifest
	@echo "zipping packages"
	@cd $(RELEASE_DIR)/$(VERSTR)/x86/; zip -qr Iris-$(VERSTR)_x86.zip Iris
	@cd $(RELEASE_DIR)/$(VERSTR)/x64/; zip -qr Iris-$(VERSTR)_x64.zip Iris

prepare:
	@mkdir -p $(RELEASE_DIR)
	@echo "Building Iris" $(VERSTR)

build:
	@echo "building x86"
	@cd $(VVVV_BASEDIR); xbuild Iris.sln $(OPTS) $(X86)
	@echo "building x64"
	@cd $(VVVV_BASEDIR); xbuild Iris.sln $(OPTS) $(X64)

clean:
	@echo 'removing builds'
	@rm -f Iris-*.zip
	@echo 'cleaning build dir'
	@rm -rf $(VVVV_BASEDIR)/build/*
	@echo 'cleaning tmp dir'
	@rm -rf tmp/*

done:
	@echo "done!"
