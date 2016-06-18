var path = require("path");
var webpack = require("webpack");

var cfg = {
  devtool: "source-map",
  entry: "../../../bin/Debug/src/Iris/Web/Frontend/Main.js",
  output: {
      path: path.join(__dirname, "../../../bin/Debug"),
      filename: "frontend.js"
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
