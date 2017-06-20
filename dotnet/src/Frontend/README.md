# INSTRUCTIONS

## BUILDING

All of these will be executed if you run a full build. All commands must be run from `dotnet` directory.

- Install dependencies:

```shell
yarn install                  # Installs npm dependencies
dotnet restore Fable.proj     # Installs Fable CLI tool
dotnet restore src/Frontend/fable/Iris.Frontend.sln
```

> There's a bug in dotnet SDK by which solution cannot be restored in non-Windows system, so you may need to restore all the folders in `src/Frontend/fable` one by one. This is already done in the `BuildFrontend` FAKE target.

- Build Fable plugin:

```shell
dotnet build -c Release src/Frontend/fable/FlatBuffersPlugin
```

- Build Worker & Frontend (not necessary in development, see Watching below):

```shell
dotnet fable yarn-run build
```

> `dotnet fable yarn-run` is used to start a Fable daemon (to compile F# files into JS) and a package.json script (usually just to call Webpack with specific arguments) in parallel.

## WATCHING

Make sure you have installed dependencies and built the plugin (see Building above) and then run:

```shell
dotnet fable yarn-run start
```
