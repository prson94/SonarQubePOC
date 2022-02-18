var replace = require('replace-in-file');
const options = {
    files: './Scripts/environments/environment.prod.ts',
    from: /{BUILD_TIMESTAMP}/g,
    to: new Date().toLocaleString(),
    allowEmptyPaths: false,
};

/* eslint-disable no-console */
try {
    let changedFiles = replace.sync(options);
    console.log('Build version set: ' + new Date().toLocaleString());
}
catch (error) {
    console.error('Error occurred:', error);
}
/* eslint-enable */