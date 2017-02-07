import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/Subject';

@Injectable()
export class AuthenticationService {    
    isAdmin: boolean = false;    
}