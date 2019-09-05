/// <binding />
var webpack = require('webpack');
var path = require('path');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');

// Webpack Config
var webpackConfig = {
  entry: {
    'polyfills': './scripts/app/polyfills.ts',
    'vendor':    './scripts/app/vendor.ts',
    'main':       './scripts/app/main.ts',
  },  
  performance: {
      hints: false
  },
  output: {
      path: __dirname + '/scripts/dist/',
      publicPath: './scripts/dist/'
  },

  plugins: [
    //new webpack.optimize.OccurrenceOrderPlugin(true),
    //new webpack.optimize.UglifyJsPlugin({compress: { warnings: false }}),
    // Workaround for https://github.com/angular/angular/issues/11580
    new webpack.ContextReplacementPlugin(
      // The (\\|\/) piece accounts for path separators in *nix and Windows
          /@angular(\\|\/)core(\\|\/)fesm5/,
      path.resolve(__dirname, '../src')
    ),
      /*new webpack.optimize.UglifyJsPlugin({
          compress: { warnings: false },comments:false
      }),*/
    new webpack.optimize.CommonsChunkPlugin({ name: ['main', 'vendor', 'polyfills'], minChunks: Infinity }),
    new webpack.DefinePlugin({
        __BUILD_DATE: JSON.stringify(new Date().toLocaleString()),        
    }),
    new webpack.SourceMapDevToolPlugin({
        filename: '[file].map',
        noSources: true,
        moduleFilenameTemplate: '[absolute-resource-path]',
        fallbackModuleFilenameTemplate: '[absolute-resource-path]'
    }),
    new CleanWebpackPlugin()
  ],

  module: {
    loaders: [
      // .ts files for TypeScript
        { test: /\.ts$/, loaders: ['awesome-typescript-loader?configFileName=scripts/app/tsconfig.json', 'angular2-template-loader','angular2-router-loader'],  exclude: [/\.(spec|e2e)\.ts$/]},
      { test: /\.css$/, loaders: ['to-string-loader', 'css-loader'] },
      { test: /\.html$/, loader: 'raw-loader' }        
    ]
  }

};

// Our Webpack Defaults
var defaultConfig = {
  // devtool: 'cheap-module-source-map',
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