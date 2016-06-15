gen:
	@flatc -I ./Schema --csharp ./Schema/*.fbs
	@cat Properties/top.tmpl > Iris.Types.csproj
	@find Iris -type f -exec echo '<Compile Include="'\{\}'" />' \; >> Iris.Types.csproj
	@cat Properties/bottom.tmpl >> Iris.Types.csproj

clean:
	@rm -rf Iris

build:
	@xbuild Iris.Types.csproj

all: clean gen build
