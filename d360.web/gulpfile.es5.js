'use strict';

var gulp = require('gulp');

gulp.task('default', function (done) {

    var bundleFilename = 'scripts/app/app.js';

    var Builder = require('systemjs-builder');

    var builder = new Builder();

    builder.loadConfig('system.config.js').then(function () {
        return builder.buildStatic('scripts/app/main.js', bundleFilename, {
            normalize: true,
            minify: true,
            mangle: true,
            runtime: false
        });
    }).then(function () {
        console.log('app.js bundle complete. `stat ./srcipts/app/' + bundleFilename + '`');
        done();
    })['catch'](function (err) {
        console.log('error', err);
        console.log('app.js bundle failed.');
        process.exit(1);
    });
});

