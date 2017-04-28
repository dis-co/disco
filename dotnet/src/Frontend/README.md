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

At the moment, you need to use two terminals to watch Fable and JS files (this will be fixed later).
Make sure you have installed dependencies and built the plugin (see Building above).

- Watch Fable files:

```shell
dotnet fable npm-run watch-fable
```

- Watch JS files and start a Webpack Dev Server (this is only for static files, you must also start
  an Iris server in localhost:7000 for the HTTP API to work):

```shell
npm run start
```

