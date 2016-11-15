var path = require("path");
var webpack = require("webpack");

module.exports = {
    entry: "./src/Main.js",
    output: {
        path: path.join(__dirname, "../../../bin/Debug/Iris/assets/js"),
        filename: "ReactApp.js",
        libraryTarget: "amd",
    },
    module: {
        loaders: [{
            test: /\.js$/,
            loaders: ['babel'],
            exclude: /node_modules/,
        }]
    },
    devtool: "eval",
    // devtool: "source-map",
    plugins: [
        // new webpack.DefinePlugin({
        //     'process.env': {
        //         'NODE_ENV': JSON.stringify('production')
        // }}),
        // new webpack.optimize.UglifyJsPlugin({
        //     compress: {
        //         warnings: false
        // }})
    ],
    // externals: ["react", "react-dom", "core-js", "babel-runtime"]
}