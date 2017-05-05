# INSTRUCTIONS

## BUILDING

All of these will be executed if you run a full build. All commands must be run from this directory.

- Install dependencies:

```shell
npm install        # Installs npm dependencies
dotnet restore     # Installs Fable CLI tool
dotnet restore fable/Core.Frontend
dotnet restore fable/Frontend
```

- Build Fable plugin:

```shell
dotnet restore fable/plugins
dotnet build -c Release fable/plugins
```

- Build Frontend (not necessary in development, see Watching below):

```shell
dotnet fable npm-run build
```

> `dotnet fable npm-run` is used to start a Fable server (to compile F# files into JS) and an npm script
  (in package.json, usually just to call Webpack with specific arguments) in parallel.

## WATCHING

Make sure you have installed dependencies and built the plugin (see Building above) and then run:

```shell
dotnet fable npm-run start
```
