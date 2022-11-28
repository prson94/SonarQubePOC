import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { empty, Observable } from 'rxjs';
import { catchError, map, publishReplay, refCount } from 'rxjs/operators';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable({
	providedIn: 'root'
})
export class UserSettingsService extends BaseObservableService {

	constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

	getUserGlobalSettings(): Observable<Map<string, string>> {

		return this.http.get(`api/v2/environment/usersettings`)
			.pipe(
				map((response) => response),
				catchError((err) => this.handleError(err))
			);
	}

	updateUserGlobalSetting(Setting: string, Value: string) {
		return this.http.put(`api/v2/environment/usersetting/${Setting}`, Value)
			.pipe(
				map((response) => response),
				catchError((err) => this.handleError(err))
			);
	}

	getUserSettings(AseetTypeUID: string): Observable<Map<string, string>> {

		return this.http.get(`api/v2/environment/usersettings/${AseetTypeUID}`)
			.pipe(
				map((response) => response),
				catchError((err) => this.handleError(err))
			);
	}

	updateUserSetting(AseetTypeUID: string, Setting: string, Value: string) {
		return this.http.put(`api/v2/environment/usersetting/${AseetTypeUID}/${Setting}`, Value)
			.pipe(
				map((response) => response),
				catchError((err) => this.handleError(err))
			);
	}
}