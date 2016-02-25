BUILD=xbuild /nologo /p:Configuration=Debug

nodes:
	${BUILD} src/Iris.Nodes/Iris.Nodes.fsproj

core:
	${BUILD} src/Iris.Core/Iris.Core.fsproj

web:
	${BUILD} src/Iris.Web/Iris.Web.fsproj

web.core:
	${BUILD} src/Iris.Web.Core/Iris.Web.Core.fsproj

web.tests:
	${BUILD} src/Iris.Web.Tests/Iris.Web.Tests.fsproj

web.worker:
	${BUILD} src/Iris.Web.Worker/Iris.Web.Worker.fsproj

vsync:
	${BUILD} src/Vsync/Vsync.csproj

tests:
	${BUILD} src/Iris.Tests/Iris.Tests.fsproj

service:
	${BUILD} src/Iris.Service/Iris.Service.fsproj

all:
	${BUILD} Iris.sln

clean:
	@find . -type d -name bin -prune -exec rm -rf '{}' \;
	@find . -type d -name obj -prune -exec rm -rf '{}' \;
