var path = require('path');
var webpack = require('webpack');

module.exports = {
  entry: [
    path.join(__dirname, './src/index.js'),
  ],

  output: {
    filename: 'bundle.js',
    path: path.join(__dirname, 'js'),
  },

  externals: {
    jquery: 'jQuery'
  },

  module: {
    rules: [
      {
        test: /\.jsx?$/,
        use: ['babel-loader'],
        exclude: /node_modules/,
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

  plugins: [
    new webpack.optimize.UglifyJsPlugin()
  ]
};
