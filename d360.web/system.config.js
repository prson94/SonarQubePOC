System.config({
    transpiler: 'typescript',
    typescriptOptions: { emitDecoratorMetadata: true },
    map: {
        'rxjs': './node_modules/rxjs',
        '@angular': './node_modules/@angular',
        'ng2-table': './node_modules/ng2-table',
    },
    packages: {
        'scripts/app': {
            format: 'register',
            defaultExtension: 'js'
        },
        'rxjs': { main: 'index.js' },
        'ng2-table': { main: 'ng2-table.js' },
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