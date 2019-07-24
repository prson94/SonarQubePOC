import {HttpEvent, HttpHandler, HttpInterceptor, HttpRequest} from "@angular/common/http";
import {Injectable} from "@angular/core";
import {Observable, throwError} from "rxjs";
import { catchError } from "rxjs/operators";

@Injectable()
export class GovernRequestInterceptor implements HttpInterceptor {
    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        let returnResult;

        if (req.method === 'POST') {
            returnResult = req.clone(
                {
                    setHeaders: {
                        'RequestVerificationToken': (<HTMLInputElement>document.getElementById('antiForgeryToken')).value,
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                }
            );
        } else {
            returnResult = req;
        }
        return next.handle(returnResult).pipe(catchError((error: Response) => this.handleError(error)));
    }

    private handleError(error: Response): Observable<never> {
           if ((error.status === 401 || error.status === 403) && (window.location.href.match(/\?/g) || []).length < 2) {
                console.log('The authentication session expires or the user is not authorized. Forcing refresh of the current page.');
                window.location.href = '/slo';
            }
        return throwError(error);
    }

}
