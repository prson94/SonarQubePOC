import {XHRBackend, Request, XHRConnection, Response} from '@angular/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/catch';
import 'rxjs/add/observable/throw';


export class AuthenticationConnectionBackend extends XHRBackend {
    createConnection(request: Request): XHRConnection {        
        let xhrConnection = super.createConnection(request);
        xhrConnection.response = xhrConnection.response.catch((error: Response) => {
            if ((error.status === 401 || error.status === 403) && (window.location.href.match(/\?/g) || []).length < 2) {
                console.log('The authentication session expires or the user is not authorized. Forcing refresh of the current page.');                
                window.location.href = '/slo';
            }
            return Observable.throw(error);
        });
        return xhrConnection;
    }

}