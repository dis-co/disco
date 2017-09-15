# IRIS Frontend Quickstart

## Requirements

- [Mono](http://www.mono-project.com/download/)
- [node.js](https://nodejs.org/) 6.9.2 or higher with [npm](https://www.npmjs.com/) 5.x
- [dotnet SDK](https://www.microsoft.com/net/download/core)

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

Make sure you have installed dependencies and built the plugin (see Building above). Also you must set the `FRONTEND_IP` and `FRONTEND_PORT` environment variables with the IP and HTTP port exposed by Iris server. Then run:

```shell
npm start
```

When the webpack-dev-server has finished compiling the code, you can open `[FRONTEND_IP]:3000` in your browser.

> To run Iris Frontend in **Design Mode** (using mock data instead of connecting with Iris server), set the environmental variable `FRONTEND_IP` to `localhost` (e.g. `export FRONTEND_IP=localhost`), and open `localhost:3000` in your browser.
