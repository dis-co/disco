var path = require("path");
var webpack = require("webpack");

var src = path.join(__dirname, "..", "..", "..");

var cfg = {
  devtool: "source-map",
  entry: [ "./Main.js", "flatbuffers"],
  context: path.join(src, "bin", "Debug", "Iris", "Tests", "Web"),
  resolve: {
    alias: {
      flatbuffers: path.join(src, "assets", "frontend", "js", "flatbuffers.js"),
      buffers: path.join(src, "bin", "Raft_generated.js")
    }
  },
  plugins: [
    new webpack.ProvidePlugin({
      flatbuffers: "flatbuffers"
    })
  ],
  output: {
    path: path.join(src,"bin","Debug","Iris","assets","js"),
    filename: "iris.tests.js"
  },
  module: {
    preLoaders: [{
      test: /\.js$/,
      exclude: /node_modules/,
      loader: "source-map-loader"
    }],
    loaders: [
      { test: /buffers/, loader: 'imports?buffers' }
    ]
  }
};

module.exports = cfg;
