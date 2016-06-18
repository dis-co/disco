CPATH := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))

clean:
	@rm -rf $(CPATH)/Serialization.csproj ||:
	@rm -rf $(CPATH)/Iris/Serialization/* ||:

gen: clean
	@cd $(CPATH); flatc -I $(CPATH)/Schema --csharp $(CPATH)/Schema/*.fbs
	@cat $(CPATH)/assets/csproj/Serialization.top.xml > $(CPATH)/Serialization.csproj
	@find $(CPATH)/Iris/Serialization -type f -iname '*.cs' -exec echo '<Compile Include="'\{\}'" />' \; >> $(CPATH)/Serialization.csproj
	@cat $(CPATH)/assets/csproj/Serialization.bottom.xml >> $(CPATH)/Serialization.csproj

build: clean gen
	@xbuild $(CPATH)/Serialization.csproj

all: clean gen build
