import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { GovernRequestInterceptor } from "./govern-request.interceptor";

export const governHttpInterceptorProviders = [
    {
        provide: HTTP_INTERCEPTORS,
        useClass: GovernRequestInterceptor,
        multi: true
    }
]