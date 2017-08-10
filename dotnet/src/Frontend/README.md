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
yarn build
```

## WATCHING

Make sure you have installed dependencies and built the plugin (see Building above) and then run:

```shell
yarn start
```
