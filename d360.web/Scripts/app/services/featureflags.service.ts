import { HttpClient, HttpContext } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { LDClient, LDFlagSet, initialize, LDOptions } from "ldclient-js";
import { Observable, Subject } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { ROUTE_INDEPENDENT_QUERY } from "../http-interceptors";
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from "./messages-observable.service";

export enum FeatureFlags {
    DistributedCacheFlag = "GovernDistributedCachePerm"
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
            "GovernDistributedCachePerm": false
        };
    }

    initialize(): Observable<string> {
        return this.http
            .get(
                'api/v2/environment/featureflaginfo',
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                map((response: any) => {
                    this.clientId = response.clientId;
                    response.user.anonymous = false;
                    this.currentUser = response.user;
                }),
                catchError((err) => this.handleError(err))
            );
    }

    createClientConnection() {
        this.ldClient = initialize(this.clientId, this.currentUser, {
            fetchGoals: false
        });

        this.ldClient.on('change', (flags) => {
            if (flags['GovernDistributedCachePerm'] !== undefined) {
                this.flags['GovernDistributedCachePerm'] = flags['GovernDistributedCachePerm'];
            }
            this.flagChange.next(this.flags);
        });

        this.ldClient.on('ready', () => {
            this.setFlags();
        });
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
