CPATH := $(shell pwd)

gen:
	@flatc -I $(CPATH)/Schema --csharp $(CPATH)/Schema/*.fbs
	@cat $(CPATH)/Properties/top.tmpl > $(CPATH)/Iris.Types.csproj
	@find $(CPATH)/Iris -type f -exec echo '<Compile Include="'\{\}'" />' \; >> $(CPATH)/Iris.Types.csproj
	@cat $(CPATH)/Properties/bottom.tmpl >> $(CPATH)/Iris.Types.csproj

clean:
	@rm -rf $(CPATH)/Iris.Types.csproj
	@rm -rf $(CPATH)/Iris

build: clean gen
	@xbuild $(CPATH)/Iris.Types.csproj
