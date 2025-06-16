import { ApplicationConfig, provideZoneChangeDetection, isDevMode } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routing';
import { SecurityService } from './_shared/services/security';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { CanLoadAsAdmin } from './_shared/guards/CanLoadAsAdmin';
import { authInterceptor } from './_shared/interceptors/auth';
import { statusCodeInterceptor } from './_shared/interceptors/statusCode';
import { TranslocoHttpLoader } from './transloco-loader';
import { provideTransloco } from '@jsverse/transloco';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(
      withInterceptors([authInterceptor, statusCodeInterceptor])
    ),
    { provide: SecurityService },
    { provide: CanLoadAsAdmin }, provideHttpClient(), provideTransloco({
        config: { 
          availableLangs: ['en', 'es', 'fr', 'it', 'nl', 'de'],
          defaultLang: 'en',
          // Remove this option if your application doesn't support changing language in runtime.
          reRenderOnLangChange: true,
          prodMode: !isDevMode(),
        },
        loader: TranslocoHttpLoader
      })
  ]
};
