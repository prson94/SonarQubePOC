/// <binding ProjectOpened='Watch - Development' />
var webpack = require('webpack');
var path = require('path');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');


// Webpack Config
var webpackConfig = {
    mode: 'production',
    entry: {
        'polyfills': './scripts/polyfills.ts',
        'vendor': './scripts/vendor.ts',
        'main': './scripts/main.ts',
    },

    output: {
        path: __dirname + '/scripts/dist/',
        publicPath: './scripts/dist/',
        filename: '[name].bundle.js',
        chunkFilename: '[id].[hash].chunk.js'
    },

    optimization: {
        noEmitOnErrors: true,
        runtimeChunk: false,
        splitChunks: {
            cacheGroups: {
                default: false,
                vendor: {
                    test: /node_modules/,
                    chunks: 'initial',
                    name: 'vendor',
                    enforce: true,
                    filename: '[name].bundle.js'
                },
            }
        }
    },

    plugins: [
        // Workaround for https://github.com/angular/angular/issues/11580
        new webpack.ContextReplacementPlugin(
            // The (\\|\/) piece accounts for path separators in *nix and Windows
            /@angular(\\|\/)core(\\|\/)fesm2015/,
            path.resolve(__dirname, '../src')
        ),

        new webpack.DefinePlugin({
            __BUILD_DATE: JSON.stringify(new Date().toLocaleString()),
            PRODUCTION: JSON.stringify(true),
        }),        
        new CleanWebpackPlugin()
    ],

    module: {
        rules: [
            {
                test: /\.less$/,
                exclude: /node_modules/,
                loader: 'raw-loader!less-loader'
            },
            // .ts files for TypeScript
            {
                test: /\.ts$/,
                use: [
                    {
                        loader: 'ts-loader', options: {
                            configFile: "scripts/tsconfig.json"
                        }
                    },
                    { loader: 'angular2-template-loader' },
                    { loader: 'angular2-router-loader' }
                ],
                exclude: [/\.(spec|e2e)\.ts$/],
            },
            { test: /\.css$/, loaders: ['to-string-loader', 'css-loader'] },
            { test: /\.html$/, loader: 'raw-loader' }
        ]
    }

};


// Our Webpack Defaults
var defaultConfig = {
    cache: true,
    output: {
        filename: '[name].bundle.js',
        sourceMapFilename: '[name].map',
        chunkFilename: '[id].chunk.js'
    },

    resolve: {
        extensions: ['.ts', '.js']
    },

    devServer: {
        historyApiFallback: true,
        watchOptions: { aggregateTimeout: 300, poll: 1000 }
    },

    node: {
        global: true,
        crypto: 'empty',
        module: false,
        Buffer: false,
        clearImmediate: false,
        setImmediate: false
    }
};

var webpackMerge = require('webpack-merge');
module.exports = webpackMerge(defaultConfig, webpackConfig);