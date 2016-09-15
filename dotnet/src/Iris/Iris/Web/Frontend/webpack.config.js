var path = require("path");
var webpack = require("webpack");

var cfg = {
  devtool: "source-map",
  entry: [ "../../../bin/Debug/Iris/Web/Frontend/Main.js", 'flatbuffers', 'buffers' ],
  resolve: {
    alias: {
      flatbuffers: __dirname + '/../../../assets/frontend/js/flatbuffers.js',
      buffers: __dirname + '/../../../Raft_generated.js'
    }
  },
  plugins: [
    new webpack.ProvidePlugin({
      flatbuffers: "flatbuffers",
      buffers: "buffers"
    })
  ],
  output: {
    path: path.join(__dirname, "../../../bin/Debug/Iris/assets/js"),
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
