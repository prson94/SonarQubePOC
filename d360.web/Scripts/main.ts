import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { enableProdMode } from '@angular/core';
import { AppModule } from './app/app.module';
import { environment } from './environments/environment';


declare var __BUILD_DATE: string;
declare var VersionNumber: string;
declare var PRODUCTION: boolean;


if (PRODUCTION) { //environment.production FOR ng build
    enableProdMode();
}
else {
    console.log("Running in developer mode...");
}

console.log("Govern Assembly Version: " + VersionNumber);
console.log("Govern Build Date: " + __BUILD_DATE);  // remove for ng build
console.log("Browser Language: " + navigator.language);


platformBrowserDynamic().bootstrapModule(AppModule, { ngZoneEventCoalescing: true })
    .catch((err) => console.log(err));