import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { enableProdMode, LOCALE_ID } from '@angular/core';
import { AppModule }              from './app.module';

declare var __BUILD_DATE: string;

if (window.location.href.indexOf('.local') < 0) {
    enableProdMode();
}
else {
    console.log("Running in d3s developer mode...");
}

console.log("Data3Sixty Client: " + __BUILD_DATE);

platformBrowserDynamic().bootstrapModule(AppModule, { preserveWhitespaces: false });
