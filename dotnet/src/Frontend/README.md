# IRIS FRONTEND DEVELOPMENT

## Requirements

- [Mono](http://www.mono-project.com/download/)
- [node.js](https://nodejs.org/) 6.9.2 or higher with [npm](https://www.npmjs.com/) 5.x
- [dotnet SDK](https://www.microsoft.com/net/download/core)

To run Fable 1.1 you'll also need **netcore runtime 1.0.4**, which can be downloaded [from here](https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.0.4-download.md).

## Building

All of these will be executed if you run a full build. All commands must be run from `dotnet` directory.

- Install dependencies:

```shell
npm install
dotnet restore src/Frontend/src/Iris.Frontend.sln
```

- Build Fable plugin:

```shell
dotnet build -c Release src/Frontend/src/FlatBuffersPlugin
```

- Build Worker & Frontend (not necessary in development, see Watching below):

```shell
npm run build
```

## Watching

Make sure you have installed dependencies and built the plugin (see Building above) and then run:

```shell
npm start
```

> To run Iris Frontend in **Design Mode** (using mocking data instead of connecting with the backend), set the environmental variable `FRONTEND_IP` to `localhost` (e.g. `export FRONTEND_IP=localhost`).
