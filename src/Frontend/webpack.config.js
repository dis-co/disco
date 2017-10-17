// @ts-check

var path = require('path');
var webpack = require('webpack');
var fableUtils = require('fable-utils');

function resolve(filePath) {
  return path.join(__dirname, filePath)
}

var isDesignMode = false;
var isDevServer = process.argv.find(v => v.includes('webpack-dev-server'));
var isProduction = process.argv.indexOf("-p") >= 0;
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

var irisHost = process.env.FRONTEND_IP;
var irisPort = process.env.FRONTEND_PORT;
if (isDevServer) {
  if (irisHost === "localhost") {
    isDesignMode = true;
    console.log("Iris will run in DESIGN MODE (no backend)");
  }
  else {
    if (!irisHost) {
      throw new Error("Please specify the Iris service IP with the FRONTEND_IP env var");
    }
    if (!irisPort) {
      throw new Error("Please specify the Iris service HTTP port with the FRONTEND_PORT env var");
    }
    console.log("Iris will connect to " + irisHost + ":" + irisPort);
  }
}


var babelOptions = fableUtils.resolveBabelOptions({
  presets: [["es2015", { "modules": false }], "stage-2", "react"],
  plugins: ["transform-runtime"]
});

function createWebpackConfig(fsProj, outputFile, libName) {
  return {
    devtool: isProduction ? false : 'inline-source-map',
    entry: fsProj,
    output: {
      filename: outputFile, // the output bundle
      path: resolve('js'),
      publicPath: '/js/', // For dev server
      libraryTarget: "var",
      library: libName
    },
    externals: {
      jquery: 'jQuery'
    },
    resolve: {
      extensions: ['.js', '.json'],
      modules: [resolve("../../node_modules/")]
    },
    devServer: {
      contentBase: resolve("."),
      host: irisHost,
      port: 3000,
      historyApiFallback: true, // respond to 404s with index.html
      // hot: true, // enable HMR on the server
      proxy: {
        '/api/*': {
          target: 'http://' + irisHost + ':' + (irisPort || '7000')
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
              define: isProduction ? [] : ["DEBUG"].concat(isDesignMode ? "DESIGN" : null).filter(x => x),
              plugins: resolve("src/FlatBuffersPlugin/bin/Release/netstandard1.6/FlatBuffersPlugin.dll"),
              extra: { projectFile: fsProj }
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
          test: /\.css$/,
          use: [ 'style-loader', 'css-loader' ]
        },
      ],
    },
  };
}

module.exports = [
  createWebpackConfig(resolve("src/Frontend/Frontend.fsproj"), "iris.js", "IrisLib"),
  createWebpackConfig(resolve("src/Worker/Worker.fsproj"), "iris.worker.js", "IrisWorker"),
]
