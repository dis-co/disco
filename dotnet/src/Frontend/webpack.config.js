// @ts-check

var path = require('path');
var webpack = require('webpack');

function resolve(filePath) {
  return path.join(__dirname, filePath)
}

var isDevServer = process.argv.find(v => v.includes('webpack-dev-server'));
var isProduction = process.argv.indexOf("-p") >= 0;
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

var irisHost = process.env.FRONTEND_IP;
if (irisHost == null && isDevServer) {
  throw new Error("Please specify the Iris service IP with the FRONTEND_IP env var");
}

var irisPort = process.env.FRONTEND_PORT || "3000";

var babelOptions = {
  presets: [["es2015", { "modules": false }], "stage-2", "react"],
  plugins: ["transform-runtime"]
}

var frontendConfig = {
  devtool: isProduction ? false : 'inline-source-map',
  entry: resolve('./fable/Frontend/Frontend.fsproj'),
  output: {
    filename: 'iris.js', // the output bundle
    path: resolve('js'),
    publicPath: '/js/', // For dev server
    libraryTarget: "var",
    library: "IrisLib"
  },
  externals: {
    jquery: 'jQuery'
  },
  resolve: {
    extensions: ['.ts', '.tsx', '.js', '.json'],
    modules: [
      "node_modules", resolve("../../node_modules/")
    ]
  },
  devServer: {
    contentBase: resolve("."),
    host: irisHost,
    port: 3000,
    historyApiFallback: true, // respond to 404s with index.html
    // hot: true, // enable HMR on the server
    proxy: {
      '/api/*': {
        target: 'http://' + irisHost + ':' + irisPort
      }
    },
    headers: {
      "Access-Control-Allow-Origin": "*"
    }
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
            plugins: resolve("./fable/FlatBuffersPlugin/bin/Release/netstandard1.6/FlatBuffersPlugin.dll"),
            extra: {
              useCache: false
            }
          }
        }
      },
      {
        test: /\.js$/,
        exclude: /node_modules[\\\/](?!fable-)/,
        use: {
          loader: 'babel-loader',
          options: babelOptions
        },
      },
      {
        test: /\.css$/,
        use: [ 'style-loader', 'css-loader' ]
      },
      {
        test: /\.tsx?$/,
        loader: "awesome-typescript-loader"
      },
    ],
  },
};

module.exports = [frontendConfig, require("./fable/Worker/webpack.config.js")]