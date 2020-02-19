import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { enableProdMode, LOCALE_ID } from '@angular/core';
import { AppModule }              from './app.module';

declare var __BUILD_DATE: string;
declare var VersionNumber: string;

if (window.location.href.indexOf('.local') < 0) {
    enableProdMode();
}
else {
    console.log("Running in govern developer mode...");
}

console.log("Govern Assembly Version: " + VersionNumber)
console.log("Govern Build Date: " + __BUILD_DATE);
console.log("Browser Language: " + navigator.language);


platformBrowserDynamic().bootstrapModule(AppModule, { preserveWhitespaces: false });
