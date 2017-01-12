import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/Subject';

@Injectable()
export class AuthenticationService {
    admin$ = <Subject<boolean>>new Subject();  

    admin() {        
        return this.admin$;
    }
}