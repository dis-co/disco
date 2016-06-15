CPATH := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))

clean:
	@rm -rf $(CPATH)/Iris.Types.csproj ||:
	@rm -rf $(CPATH)/Iris ||:

gen: clean
	@cd $(CPATH); flatc -I $(CPATH)/Schema --csharp $(CPATH)/Schema/*.fbs
	@cat $(CPATH)/Properties/top.tmpl > $(CPATH)/Iris.Types.csproj
	@find $(CPATH)/Iris -type f -exec echo '<Compile Include="'\{\}'" />' \; >> $(CPATH)/Iris.Types.csproj
	@cat $(CPATH)/Properties/bottom.tmpl >> $(CPATH)/Iris.Types.csproj

build: clean gen
	@xbuild $(CPATH)/Iris.Types.csproj


all: clean gen build
