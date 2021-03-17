import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { enableProdMode } from '@angular/core';
import { AppModule } from './app/app.module';
import { environment } from './environments/environment';

if (environment.production) {
    enableProdMode();
}

declare var __BUILD_DATE: string;
declare var VersionNumber: string;


if (environment.production) {
    enableProdMode();
}
else {
    console.log("Running in developer mode...");
}

console.log("Govern Assembly Version: " + VersionNumber)
//console.log("Govern Build Date: " + __BUILD_DATE);
console.log("Browser Language: " + navigator.language);


platformBrowserDynamic().bootstrapModule(AppModule)
    .catch(err => console.log(err));
