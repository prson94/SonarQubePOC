import { HttpClient, HttpContext } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { catchError, mergeMap, take } from "rxjs/operators";
import { ROUTE_INDEPENDENT_QUERY } from "../http-interceptors";
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from "./messages-observable.service";
import { LaunchDarklyConfig, LaunchDarklyService } from "@precisely/prism-ng/launch-darkly";
import { LDUser } from "launchdarkly-js-client-sdk";

@Injectable({
    providedIn: 'root'
})
export class FeatureFlagsInitService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService, private launchDarkly: LaunchDarklyService) {
        super(messagesService);
    }

    initialize(): Observable<void> {
        return this.http
            .get(
                'api/v2/environment/featureflaginfo',
                { context: new HttpContext().set(ROUTE_INDEPENDENT_QUERY, true) }
            )
            .pipe(
                mergeMap((response: { clientId: string, user: LDUser }) => {
                    response.user.anonymous = false;
                    const config: LaunchDarklyConfig = {
                        envKey: response.clientId,
                        options: { fetchGoals: false, streaming: true }
                    };
                    return this.launchDarkly.init(config, response.user).pipe(take(1));
                }),
                catchError((err) => this.handleError(err))
            );
    }
}
