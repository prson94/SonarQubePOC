import {HttpEvent, HttpHandler, HttpInterceptor, HttpRequest} from "@angular/common/http";
import {Injectable} from "@angular/core";
import {Observable} from "rxjs";

@Injectable()
export class GovernPostRequestInterceptor implements HttpInterceptor {
    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        let returnResult;

        console.log(`req ${req}`);

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

        return next.handle(returnResult);
    }
}
