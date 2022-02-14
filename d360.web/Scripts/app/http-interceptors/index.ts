import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { CancelOnPageChangeInterceptor } from './cancelOnPageChange.interceptor';
import { GovernRequestInterceptor } from "./govern-request.interceptor";
import { ReuseInterceptor } from "./reuse.interceptor";

export const governHttpInterceptorProviders = [
    {
        provide: HTTP_INTERCEPTORS,
        useClass: GovernRequestInterceptor,
        multi: true
    },
    {
        provide: HTTP_INTERCEPTORS,
        useClass: CancelOnPageChangeInterceptor,
        multi: true
    },
    {
        provide: HTTP_INTERCEPTORS,
        useFactory: (instance: ReuseInterceptor) => instance,
        deps: [ReuseInterceptor],
        multi: true
    },
]

export { ROUTE_INDEPENDENT_QUERY } from './routeIndependentQuery';
export { IS_QUERY, isQueryRequest } from './isQuery';
