var path = require('path');
var webpack = require('webpack');

function resolve(filePath) {
  return path.join(__dirname, filePath)
}

var isProduction = process.argv.indexOf("-p") >= 0;
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

var babelOptions = {
  presets: [["es2015", { "modules": false }]],
  plugins: ["transform-runtime"]
}

var babelReactOptions = {
  presets: [["es2015", { "modules": false }], "react"],
  plugins: ["transform-runtime", "react-hot-loader/babel"]
}

var irisConfig = {
  devtool: "source-map",
  entry: resolve('./fable/Frontend/Frontend.fsproj'),
  output: {
    filename: 'iris.js', // the output bundle
    path: resolve('js'),
    libraryTarget: "var",
    library: "Iris"
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
            plugins: resolve("./fable/plugins/bin/Release/netstandard1.6/FlatBuffersPlugin.dll"),            
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
    ],
  },
  resolve: {
    modules: [
      "node_modules", resolve("./node_modules/")
    ]
  },
};
  
var bundleConfig = {
  entry: isProduction
    ? resolve('./src/index.js')
    : [
      'react-hot-loader/patch',
      // activate HMR for React

      'webpack-dev-server/client?http://localhost:3000',
      // bundle the client for webpack-dev-server
      // and connect to the provided endpoint

      'webpack/hot/only-dev-server',
      // bundle the client for hot reloading
      // only- means to only hot reload for successful updates
      resolve('./src/index.js')
      // the entry point of our app
    ],

  output: {
    filename: 'bundle.js', // the output bundle
    path: resolve('js'),
    publicPath: '/js/' // necessary for HMR to know where to load the hot update chunks    
  },

  externals: {
    jquery: 'jQuery'
  },

  resolve: {
    extensions: ['.ts', '.tsx', '.js', '.json']
  },

  devtool: isProduction ? false : 'inline-source-map',

  module: {
    rules: [
      {
        test: /\.js$/,
        exclude: /node_modules[\\\/](?!fable-)/,
        use: {
          loader: 'babel-loader',
          options: babelReactOptions
        },
      },
      {
        test: /\.css$/,
        use: [ 'style-loader', 'css-loader' ]
      },
      {
        test: /\.less$/,
        use: [
          'style-loader',
          { loader: 'css-loader', options: { importLoaders: 1 } },
          'less-loader'
        ]
      },
      {
        test: /\.tsx?$/,
        loader: "awesome-typescript-loader"
      },
    ],
  },

  plugins: isProduction ? [] : [
    new webpack.HotModuleReplacementPlugin(),
    // enable HMR globally

    new webpack.NamedModulesPlugin(),
    // prints more readable module names in the browser console on HMR updates

    new webpack.NoEmitOnErrorsPlugin(),
    // do not emit compiled assets that include errors
  ],

  devServer: {
    host: 'localhost',
    port: 3000,
    historyApiFallback: true, // respond to 404s with index.html
    hot: true, // enable HMR on the server
    proxy: {
      '/api/*': {
        target: 'http://localhost:7000'
      }
    }    
  },  
};

module.exports = [ irisConfig, bundleConfig ];
