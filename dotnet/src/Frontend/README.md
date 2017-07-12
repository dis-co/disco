# INSTRUCTIONS

## BUILDING

All of these will be executed if you run a full build. All commands must be run from `dotnet` directory.

- Install dependencies:

```shell
yarn install
dotnet restore src/Frontend/fable/Iris.Frontend.sln
```

- Build Fable plugin:

```shell
dotnet build -c Release src/Frontend/fable/FlatBuffersPlugin
```

- Build Worker & Frontend (not necessary in development, see Watching below):

```shell
cd src/Frontend/fable/Worker
dotnet fable yarn-worker --port free -- -p

# In a different shell
cd src/Frontend/fable/Frontend
dotnet fable yarn-build
```

> `dotnet fable yarn-run [SCRIPT]` is used to start a Fable daemon (to compile F# files into JS) and a package.json script (usually just to call Webpack with specific arguments) in parallel.

## WATCHING

Make sure you have installed dependencies and built the plugin (see Building above) and then run:

```shell
cd src/Frontend/fable/Worker
dotnet fable yarn-worker --port free -- -w

# In a different shell
cd src/Frontend/fable/Frontend
dotnet fable yarn-start
```
