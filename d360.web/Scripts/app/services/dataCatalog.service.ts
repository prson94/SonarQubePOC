import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from './messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class DataCatalogService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

	public getAssets(params): Observable<any[]> {
		let qString = ``;
		if (params) {
			qString = Object.keys(params).map((key) => key + '=' + params[key]).join('&');
			if (qString) { qString = '?' + qString; }
		}
        return this
            .http
			.get(`/api/v2/assets` + qString)
            .pipe(
                map((response) => <any[]>response),
                catchError((err) => this.handleError(err, true))
            );
    }
}
