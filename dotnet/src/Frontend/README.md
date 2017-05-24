# INSTRUCTIONS

## BUILDING

All of these will be executed if you run a full build. All commands must be run from `dotnet` directory.

- Install dependencies:

```shell
yarn install                  # Installs npm dependencies
dotnet restore Fable.proj     # Installs Fable CLI tool
dotnet restore src/Frontend/fable/Iris.Frontend
```

- Build Fable plugin:

```shell
dotnet build -c Release src/Frontend/fable/plugins
```

- Build Worker & Frontend (not necessary in development, see Watching below):

```shell
dotnet fable npm-run build-worker
dotnet fable npm-run build
```

> `dotnet fable npm-run` is used to start a Fable server (to compile F# files into JS) and an npm script
  (in package.json, usually just to call Webpack with specific arguments) in parallel.

## WATCHING

Make sure you have installed dependencies and built the plugin (see Building above) and then run:

```shell
dotnet fable npm-run start
```

To watch also the worker files you must run in a different terminal:

```shell
dotnet fable npm-run watch-worker --port free
```

> The `--port free` argument is necessary to prevent conflicts with the other Fable server