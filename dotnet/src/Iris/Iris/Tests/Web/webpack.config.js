var path = require("path");
var webpack = require("webpack");

var cfg = {
  devtool: "source-map",
  entry: "../../../bin/Debug/Iris/Tests/Web/Main.js",
  output: {
    path: path.join(__dirname, "../../../bin/"),
    filename: "iris.tests.js"
  },
  module: {
    preLoaders: [
      {
        test: /\.js$/,
        exclude: /node_modules/,
        loader: "source-map-loader"
      }
    ]
  }
};

module.exports = cfg;
