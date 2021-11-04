var fs = require('fs');

/**
 * Workaround for GOV-16239
 * The precompiled UMD bundle in PrineNG 11 does not import Quill correctly
 * The ngcc compiler in Angular 12 called by Wbepack will translate the bundles
 * to Ivy entry points with that error causing an error on HTML fields (p-editor/quill).
 * 
 * This workaround is only until PrimeNG is upgraded, or 'ng build' is being used.
 * Also remove the postinstall entry from package.json when removing this file.
 * */

function goc16239fix() {
    var files = [
        '.\\node_modules\\primeng\\bundles\\primeng-editor.umd.js',
        '.\\node_modules\\primeng\\__ivy_ngcc__\\bundles\\primeng-editor.umd.js'
    ];

    files.forEach(function (editorfile) {
        /* eslint-disable security/detect-non-literal-fs-filename -- Safe as no value holds user input */
        fs.exists(editorfile, function (exists) {
            if (exists) {
                fs.readFile(editorfile, 'utf-8', function (err, data) {
                    if (err) {
                        throw err;
                    }
                    var pattern = /\/\*\#__PURE__\*\/_interopNamespace\(Quill\)/gim;
                    if (data.match(pattern)) {
                        var newValue = data.replace(pattern, 'Quill');

                        fs.writeFile(editorfile, newValue, 'utf-8', function (err) {
                            if (err) {
                                throw err;
                            }
                        });
                    }
                });
            }
        });
        /* eslint-enable */
    });
}

goc16239fix();