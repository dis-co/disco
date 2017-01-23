var path = require("path");
var webpack = require("webpack");

var outDir, devtool, devServer = null, plugins = [];
if (process.env.NODE_ENV !== "production") {
    console.log("Starting Webpack Dev Server...");
    outDir = "./js";
    devtool = "eval";
    devServer = {
        port: 8080,
        contentBase: "../../../assets/frontend",
        proxy: {
            '/api/*': {
                target: 'http://localhost:7000'
            }
        }
    };
}
else {
    console.log("Bundling for production...");
    outDir ="../../../assets/frontend/js";
    devtool = "source-map";
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
    entry: "./src/Main.js",
    output: {
        path: path.join(__dirname, outDir),
        publicPath: "/js/",
        filename: "ReactApp.js",
        libraryTarget: "amd",
    },
    externals: {
        // This is the only way I had to convince RequireJS
        // to load the lib file and its dependencies properly
        iris: "js/Web/Lib",
        react: "react",
        "react-dom": "react-dom"
    },
    devtool: devtool,
    devServer: devServer,
    module: {
        loaders: [
            { enforce: "pre", test: /\.js$/, loader: "source-map-loader" },
            { test: /\.less$/, loader: "style-loader!css-loader!less-loader" },
            { test: /\.css$/, loader: "style-loader!css-loader" },
            { test: /\.tsx?$/, loader: "awesome-typescript-loader" },
            { test: /\.js$/, loader: 'babel-loader', exclude: /node_modules/ }
        ]
    },
    plugins: plugins
}
