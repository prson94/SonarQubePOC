import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
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
console.log("Browser Language: " + navigator.language);


platformBrowserDynamic().bootstrapModule(AppModule, { ngZoneEventCoalescing: true })
    .catch((err) => console.log(err));
/* eslint-enable */