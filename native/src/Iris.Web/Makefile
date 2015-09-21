path := $(shell dirname $(abspath $(MAKEFILE_LIST)))

all:
	@echo "Path: " ${path}
	@xbuild /nologo /p:Configuration=Debug ${path}/Iris.Web.fsproj
	@mono ${path}/bin/Debug/Iris.Web.exe ${path}/bin/Debug/assets/js/iris.js
