path := $(shell dirname $(abspath $(MAKEFILE_LIST)))

all:
	@echo "Path: " ${path}
	@xbuild /nologo /p:Configuration=Debug ${path}/Iris.Web.Worker.fsproj
