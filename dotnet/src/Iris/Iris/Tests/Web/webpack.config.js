var path = require("path");
var webpack = require("webpack");

var cfg = {
  devtool: "source-map",
  entry: "./Main.js",
  context: path.join(__dirname, "..", "..", "..", "bin", "Debug", "Iris", "Tests", "Web"),
  output: {
    path: path.join(__dirname, "..", "..", "..","bin","Debug","Iris","assets","js"),
    filename: "iris.tests.js"
  },
  module: {
    preLoaders: [{
      test: /\.js$/,
      exclude: /node_modules/,
      loader: "source-map-loader"
    }]
  }
};

module.exports = cfg;
