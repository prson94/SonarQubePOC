/// <binding Clean='clean' ProjectOpened='watch' />

var ts = require('gulp-typescript');
var gulp = require('gulp');
var del = require('del');

var tsproj = ts.createProject('scripts/app/tsconfig.json', { typescript: require('typescript') });
var app = 'scripts/app';


gulp.task('clean', function (done) {
    return del([`${app}/**/*.js`, `${app}/**/*.js.map`]);
});

gulp.task('compile', ['clean'], function (done) {
    var tsres = tsproj.src().pipe(ts(tsproj));
    return tsres.pipe(gulp.dest(app));
});

gulp.task('bundle', ['compile'], function (done) {

    let bundleFilename = `${app}/app.js`;
    const Builder = require('systemjs-builder');
    let builder = new Builder();

    builder.loadConfig('system.config.js')
    .then(function () { 
        return builder 
            .buildStatic(`${app}/main.js`, bundleFilename, {
                normalize: true,
                minify: false,
                mangle: false,
                runtime: false
            });
    })
    .then(function () {
        done();
    })
    .catch(function (err) {
        console.log('error', err);
        console.log('app.js bundle failed.');
        process.exit(1);
    });
});

gulp.task('bundle-release', ['compile'], function (done) {

    let bundleFilename = `${app}/app.js`;
    const Builder = require('systemjs-builder');
    let builder = new Builder();

    builder.loadConfig('system.config.js')
    .then(function () { 
        return builder 
            .buildStatic(`${app}/main.js`, bundleFilename, {
                normalize: true,
                minify: true,
                mangle: true,
                runtime: false
            });
    })
    .then(function () {
        done();
    })
    .catch(function (err) {
        console.log('error', err);
        console.log('app.js bundle failed.');
        process.exit(1);
    });
});

gulp.task('default', ['bundle'], function (done) {
    done();
});

gulp.task('watch', function () {
    gulp.watch(`${app}/**/*.ts`, ['default']);
});