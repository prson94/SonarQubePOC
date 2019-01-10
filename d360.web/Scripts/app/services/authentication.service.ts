import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable()
export class AuthenticationService {    
    private _isAdmin: boolean = false;    

    private isAdminSource = new Subject<boolean>();
    public isAdmin$ = this.isAdminSource.asObservable();

    get isAdmin(): boolean {
        return this._isAdmin;
    }

    set isAdmin(val: boolean) {
        this._isAdmin = val;
        this.isAdminSource.next(val);
    }


}