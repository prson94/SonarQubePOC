var replace = require('replace-in-file');
const os = require('os');
const v8 = require('node:v8');
const options = {
    files: './Scripts/environments/environment.prod.ts',
    from: /{BUILD_TIMESTAMP}/g,
    to: new Date().toLocaleString(),
    allowEmptyPaths: false,
};

function formatMem(size) {
	const gbNow = size / 1024 / 1024 / 1024;
	const gbRounded = Math.round(gbNow * 100) / 100;
	return gbRounded;
}

/* eslint-disable no-console */
function logMemoryInfo() {
	console.log('Total memory: ' + formatMem(os.totalmem()) + ' GB');
	console.log('Free memory: ' + formatMem(os.freemem()) + ' GB');
	console.log('Heap Statistics: ' + v8.getHeapStatistics().heap_size_limit / (1024 * 1024));
}

try {
	logMemoryInfo();
    let changedFiles = replace.sync(options);
    console.log('Build version set: ' + new Date().toLocaleString());
}
catch (error) {
    console.error('Error occurred:', error);
}
/* eslint-enable */