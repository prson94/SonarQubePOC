import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { LDClient, LDFlagSet, initialize, LDOptions } from "ldclient-js";
import { Observable, Subject } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from "./messages-observable.service";

export enum FeatureFlags {
    DistributedCacheFlag = "govern-distributed-cache-perm"
}

@Injectable({
    providedIn: 'root'
})
export class FeatureFlagsService extends BaseObservableService {
    ldClient: LDClient;
    flags: LDFlagSet;
    flagChange: Subject<Object> = new Subject<Object>();
    clientId: string;
    currentUser: any;

    constructor(private http: HttpClient, messagesService: MessagesObservableService) {
        super(messagesService);
        this.flags = {
            "govern-distributed-cache-perm": false
        };
    }

    initialize(): Observable<string> {
        return this.http.get('api/v2/environment/featureflaginfo')
            .pipe(
                map((response: any) => {
                    this.clientId = response.clientId;//;"6165a52a7fcabc0c120d82c2";
                    response.user.anonymous = false;
                    this.currentUser = response.user;
                }),
                catchError((err) => this.handleError(err))
        );
    }

    createClientConnection() {
        console.log(this.currentUser);
        this.ldClient = initialize(this.clientId, this.currentUser);

        this.ldClient.on('change', (flags) => {
            if (flags['govern-distributed-cache-perm'] !== undefined) {
                this.flags['govern-distributed-cache-perm'] = flags['govern-distributed-cache-perm'];
            }
            this.flagChange.next(this.flags);
        });

        this.ldClient.on('ready', () => {
            this.setFlags();
        })
    }

    setFlags() {
        this.flags = this.ldClient.allFlags();
    }

    changeUser(user) {
        if (user !== "Anonymous") {
            this.ldClient.identify({ key: user, name: user, anonymous: false });
        }
        else {
            this.ldClient.identify({ key: 'anon', anonymous: true });
        }
    }
 }