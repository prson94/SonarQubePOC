"use strict";
var platform_browser_dynamic_1 = require('@angular/platform-browser-dynamic');
var core_1 = require('@angular/core');
var app_module_1 = require('./app.module');
if (window.location.href.indexOf('.local') < 0) {
    core_1.enableProdMode();
}
else {
    console.log("Running in d3s developer mode...");
}
platform_browser_dynamic_1.platformBrowserDynamic().bootstrapModule(app_module_1.AppModule);
//# sourceMappingURL=main.js.map