var path = require("path");
var webpack = require("webpack");

var src = path.join(__dirname, "../..");

var cfg = {
  devtool: "source-map",
  entry: path.join(src, "bin/Debug/Web.Tests/Main.js"),
  resolve: {
    alias: {
      flatbuffers: path.join(src, "assets", "frontend", "js", "flatbuffers.js"),
      buffers: path.join(src, "bin", "Raft_generated.js")
    },
    moduleDirectories: [ 'node_modules' ]
  },
  plugins: [
    new webpack.ProvidePlugin({
      flatbuffers: "flatbuffers",
      buffers: "buffers"
    })
  ],
  output: {
    path: path.join(src, "bin", "Debug", "Iris", "assets", "js"),
    filename: "iris.js"
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
