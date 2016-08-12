import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { enableProdMode } from '@angular/core';
import { AppModule }              from './app.module';

if (window.location.href.indexOf('.local') < 0) {
    enableProdMode();
}
else {
    console.log("Running in d3s developer mode...");
}

platformBrowserDynamic().bootstrapModule(AppModule);
