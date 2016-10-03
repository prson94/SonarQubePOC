import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/Rx';
import {take} from "rxjs/operator/take";

@Injectable()
export class AuthenticationService {
    admin$ = <Subject<boolean>>new Subject();  

    admin() {        
        return this.admin$;
    }
}