/// <binding />
var webpack = require('webpack');
var path = require('path');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const BundleAnalyzerPlugin = require('webpack-bundle-analyzer').BundleAnalyzerPlugin;
const { AngularWebpackPlugin } = require('@ngtools/webpack');

// Webpack Config
var webpackConfig = {
    mode: 'development',
    entry: {
        'polyfills': './scripts/polyfills.ts',
        'main': './scripts/main.ts',
    },
    performance: {
        hints: false
    },
    output: {
        path: __dirname + '/scripts/dist/',
        publicPath: './scripts/dist/',        
        filename: '[name].bundle.js',
        chunkFilename: '[id].chunk.js'
    },

    optimization: {
        emitOnErrors: true,
        runtimeChunk: false,
        splitChunks: {
            cacheGroups: {
                default: false,
                defaultVendors: {
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
            path.resolve(__dirname, '../scripts/app')
        ),        
        new webpack.DefinePlugin({
            __BUILD_DATE: JSON.stringify(new Date().toLocaleString()),
            PRODUCTION: JSON.stringify(false),
        }),
        new webpack.SourceMapDevToolPlugin({
            filename: '[file].map',
            noSources: true,
            moduleFilenameTemplate: '[absolute-resource-path]',
            fallbackModuleFilenameTemplate: '[absolute-resource-path]'
        }),
        new AngularWebpackPlugin ({
            tsconfig: 'scripts/tsconfig.json',
        }),
        // Uncomment the following line to see the webpack bundle sizes for the SPA
        //new BundleAnalyzerPlugin(),
        new CleanWebpackPlugin()
    ],

    module: {
        rules: [
            {
                test: /\.[cm]?js$/,
                resolve: {
                    fullySpecified: false,
                },
                use: {
                    loader: 'babel-loader',
                    options: {
                        cacheDirectory: true,
                        compact: false,
                        plugins: ['@angular/compiler-cli/linker/babel'],
                    },
                },
            },
            {
                test: /\.less$/,        
                exclude: /node_modules/,
                use: ['raw-loader','less-loader'] 
            },
            {
                test: /\.[jt]sx?$/,
                use: '@ngtools/webpack'
            },
            { test: /\.css$/, use: ['to-string-loader', 'css-loader'] },            
            { test: /\.html$/, use: 'raw-loader' },
        ],
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
        global: true
    }
};

var webpackMerge = require('webpack-merge');
module.exports = webpackMerge(defaultConfig, webpackConfig);