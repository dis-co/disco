# Iris documentation Generator

Fable Node.js app to generate Iris documentation as static web pages.

## Installing and building

- Install JS dependencies: `npm install`
- **Move to src folder**: `cd src`
- Install F# dependencies: `dotnet restore`

To generate the web pages, _still in src folder_ (where the .fsproj is), run `dotnet fable npm-start`. This will start Fable in watch mode, so any time you edit one of the F# files, the page(s) will be generated again. If you just want to run Fable once, use `dotnet fable npm-build` instead.

The web pages will be output to `public` directory. To start a static server to display them in a browser, _in a new terminal and from the repo root_, run `npm run server`.
