import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';

@Injectable()
export class AuthenticationService {
    private _isAdmin: boolean = null;

    private isAdminSource = new Subject<boolean>();
    public isAdmin$ = this.isAdminSource.asObservable();

    constructor(private http: HttpClient) { }

    get isAdmin(): boolean {
        return this._isAdmin;
    }

    set isAdmin(val: boolean) {
        this._isAdmin = val;
        this.isAdminSource.next(val);
    }

    checkCurrentUserAdmin(): Observable<boolean> {
        return this.http.get("/api/v2/membership/users/me/roles")
            .pipe(map((res) => {
                this._isAdmin = (res as string[]).some((x) => x === "Administrator");
                return this._isAdmin;
            }));
    }

}