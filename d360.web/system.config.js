System.config({
    transpiler: 'typescript',
    typescriptOptions: { emitDecoratorMetadata: true },
    map: {
        'rxjs': './node_modules/rxjs',
        '@angular': './node_modules/@angular',
        'angular2-datatable/datatable': './node_modules/angular2-datatable',
        'lodash': './node_modules/lodash'
    },
    packages: {
        'scripts/app': {
            format: 'register',
            defaultExtension: 'js'
        },
        'lodash' : { main: 'lodash.js' },
        'rxjs': { main: 'index.js' },
        'angular2-datatable/datatable': { main: 'datatable.js'},
        '@angular/core': { main: 'index.js' },
        '@angular/http': { main: 'index.js' },
        '@angular/router-deprecated': { main: 'index.js' },
        '@angular/upgrade': { main: 'index.js' },
        '@angular/common': { main: 'index.js' },
        '@angular/compiler': { main: 'index.js' },
        '@angular/router': { main: 'index.js' },
        '@angular/platform-browser': { main: 'index.js' },
        '@angular/platform-browser-dynamic': { main: 'index.js' },
    }
});