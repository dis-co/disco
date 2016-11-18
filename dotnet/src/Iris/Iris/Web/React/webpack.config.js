var path = require("path");
var webpack = require("webpack");

var entry, outDir, devtool, devServer, loaders, plugins;
if (process.env.NODE_ENV !== "production") {
    console.log("Starting Webpack Dev Server...");
    entry = [
        "webpack-dev-server/client?http://localhost:8080",
        'webpack/hot/only-dev-server',
        "./src/Main.js"
    ];
    outDir = "./temp";
    devtool = "eval";
    devServer = null;
    loaders = [{
        test: /\.js$/,
        loader: "react-hot-loader/webpack",
        exclude: /node_modules/
    }];
    plugins = [
        new webpack.HotModuleReplacementPlugin()
    ];
}
else {
    console.log("Bundling for production...");
    entry = "./src/Main.js";
    outDir = "../../../bin/Debug/Iris/assets/js";
    devtool = "source-map";
    devServer = null;
    loaders = [];
    plugins = [
        new webpack.DefinePlugin({
            'process.env': {
                'NODE_ENV': JSON.stringify('production')
        }}),
        new webpack.optimize.UglifyJsPlugin({
            compress: {
                warnings: false
        }})
    ];
}

module.exports = {
    entry: entry,
    output: {
        path: path.join(__dirname, outDir),
        filename: "ReactApp.js",
        libraryTarget: "amd",
    },
    devtool: devtool,
    devServer: devServer,
    module: {
        loaders: [
            { test: /\.less$/, loader: "style-loader!css-loader!less-loader" },
            { test: /\.css$/, loader: "style-loader!css-loader" },
            { test: /\.ts$/, loader: 'ts-loader' },
            { test: /\.js$/, loader: 'babel', exclude: /node_modules/ }
        ].concat(loaders)
    },
    plugins: plugins
}
