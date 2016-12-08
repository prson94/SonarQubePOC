/// <binding />
var webpack = require('webpack');
var path = require('path');


// Webpack Config
var webpackConfig = {
  entry: {
    'polyfills': './scripts/app/polyfills.ts',
    'vendor':    './scripts/app/vendor.ts',
    'main':       './scripts/app/main.ts',
  },

  output: {
      path: './scripts/app/',
      publicPath: './scripts/app/'
  },

  plugins: [
    //new webpack.optimize.OccurrenceOrderPlugin(true),
    //new webpack.optimize.UglifyJsPlugin({compress: { warnings: false }}),
    // Workaround needed for angular 2 angular/angular#11580
      new webpack.ContextReplacementPlugin(
        // The (\\|\/) piece accounts for path separators in *nix and Windows
        /angular(\\|\/)core(\\|\/)(esm(\\|\/)src|src)(\\|\/)linker/,
         path.join(__dirname, '/scripts/app/') // location of your src
      ),
      /*new webpack.optimize.UglifyJsPlugin({
          compress: { warnings: false },comments:false
      }),*/
    new webpack.optimize.CommonsChunkPlugin({ name: ['main', 'vendor', 'polyfills'], minChunks: Infinity }),
  ],

  module: {
    loaders: [
      // .ts files for TypeScript
      { test: /\.ts$/, loaders: ['awesome-typescript-loader?tsconfig=./scripts/app/tsconfig.json', 'angular2-template-loader','angular2-router-loader']},
      { test: /\.css$/, loaders: ['to-string-loader', 'css-loader'] },
      { test: /\.html$/, loader: 'raw-loader' }        
    ]
  }

};


// Our Webpack Defaults
var defaultConfig = {
   devtool: 'cheap-module-source-map',
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