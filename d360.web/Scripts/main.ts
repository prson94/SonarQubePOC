import { platformBrowser } from '@angular/platform-browser';
import { enableProdMode } from '@angular/core';
import { AppModule } from './app/app.module';
import { environment } from './environments/environment';

/* eslint-disable no-console */
if (environment.production) { //environment.production FOR ng build
    enableProdMode();
}
else {
    console.log("Running in developer mode...");
}

console.log("Govern Assembly Version: " + environment.version);
console.log("Govern Build Date: " + environment.timeStamp);  // remove for ng build
console.log("Browser Language: " + navigator.language);


platformBrowser().bootstrapModule(AppModule, { ngZoneEventCoalescing: true })
    .catch((err) => console.log(err));
/* eslint-enable */