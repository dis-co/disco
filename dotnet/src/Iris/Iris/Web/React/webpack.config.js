var path = require("path");
var webpack = require("webpack");

var entry, outDir, devtool, devServer, loaders, plugins;
if (process.env.NODE_ENV !== "production") {
    console.log("Starting Webpack Dev Server...");
    entry = [
        "webpack-dev-server/client?http://localhost:7000",
        'webpack/hot/only-dev-server',
        "./src/Main.js"
    ];
    outDir = "./js";
    devtool = "eval";
    devServer = {
        port: 7000,
        contentBase: "../../../bin/Debug/Iris/assets"
    };
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
    externals: {
        // This is the only way I had to convince RequireJS
        // to load the lib file and its dependencies properly
        lib: "js/Web/Lib",
        react: "react",
        "react-dom": "react-dom"
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
