System.config({
    transpiler: 'typescript',
    typescriptOptions: { emitDecoratorMetadata: true },
    map: {
        'rxjs': './node_modules/rxjs',
        '@angular': './node_modules/@angular',
        'lodash': './node_modules/lodash',
        'primeng': './node_modules/primeng'
    },    
    packages: {
        'scripts/app': {
            format: 'register',
            defaultExtension: 'js'
        },
        'lodash' : { main: 'lodash.js' },
        'rxjs': { main: 'index.js' },

        '@angular/router': { main: '/bundles/router.umd.js', defaultExtension: 'js' },
        '@angular/forms': { main: '/bundles/forms.umd.js', defaultExtension: 'js' },
        '@angular/core': { main: '/bundles/core.umd.js', defaultExtension: 'js' },
        '@angular/http': { main: '/bundles/http.umd.js', defaultExtension: 'js' },
        '@angular/router-deprecated': { main: '/bundles/router-deprecated.umd.js', defaultExtension: 'js' },
        '@angular/upgrade': { main: '/bundles/upgrade.umd.js', defaultExtension: 'js' },
        '@angular/common': { main: '/bundles/common.umd.js', defaultExtension: 'js' },
        '@angular/compiler': { main: '/bundles/compiler.umd.js', defaultExtension: 'js' },
        '@angular/platform-browser': { main: '/bundles/platform-browser.umd.js', defaultExtension: 'js' },
        '@angular/platform-browser-dynamic': { main: '/bundles/platform-browser-dynamic.umd.js', defaultExtension: 'js' },



        'primeng': { defaultExtension: 'js' }
    },
});