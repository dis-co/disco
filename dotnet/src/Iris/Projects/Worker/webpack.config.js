var path = require("path");
var webpack = require("webpack");

var src = path.join(__dirname, "../..");

var cfg = {
  devtool: "source-map",
  entry: path.join(src, "bin/Debug/Worker/Web/Worker.js"),
  // context: path.join(src, "bin", "Debug", "Iris", "Web", "Worker"),
  resolve: {
    alias: {
      flatbuffers: path.join(src, "assets", "frontend", "js", "flatbuffers.js"),
      buffers: path.join(src, "bin", "Raft_generated.js")
    }
  },
  plugins: [
    new webpack.ProvidePlugin({
      flatbuffers: "flatbuffers",
      buffers: "buffers"
    })
  ],
  output: {
    path: path.join(src,"bin","Debug","Iris","assets","js"),
    filename: "worker.js"
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
