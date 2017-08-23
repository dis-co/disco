# INSTRUCTIONS

## BUILDING

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

## WATCHING

Make sure you have installed dependencies and built the plugin (see Building above) and then run:

```shell
npm start
```
