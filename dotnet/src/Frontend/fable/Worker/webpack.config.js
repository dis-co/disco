// @ts-check

var path = require('path');
var webpack = require('webpack');

function resolve(filePath) {
  return path.join(__dirname, filePath)
}

var isProduction = process.argv.indexOf("-p") >= 0;
console.log("Bundling Worker for " + (isProduction ? "production" : "development") + "...");

var babelOptions = {
  presets: [["es2015", { "modules": false }]],
  plugins: ["transform-runtime"]
}

module.exports = {
  devtool: isProduction ? false : 'inline-source-map',
  entry: resolve('./Worker.fsproj'),
  output: {
    filename: 'iris.worker.js', // the output bundle
    path: resolve('../../js'),
    libraryTarget: "var",
    library: "IrisWorker"
  },
  resolve: {
    extensions: ['.ts', '.tsx', '.js', '.json'],
    modules: [resolve("../../../../node_modules/")]
  },
  module: {
    rules: [
      {
        test: /\.fs(x|proj)?$/,
        use: {
          loader: 'fable-loader',
          options: {
            babel: babelOptions,
            define: isProduction ? [] : ["DEBUG"],
            plugins: resolve("../FlatBuffersPlugin/bin/Release/netstandard1.6/FlatBuffersPlugin.dll"),
            extra: { useCache: "readonly" }
          }
        }
      },
      {
        test: /\.js$/,
        exclude: /node_modules/,
        use: {
          loader: 'babel-loader',
          options: babelOptions
        },
      },
      {
        test: /\.tsx?$/,
        loader: "awesome-typescript-loader"
      },
    ],
  },
};
