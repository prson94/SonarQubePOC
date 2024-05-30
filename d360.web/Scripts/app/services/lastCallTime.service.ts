import { Injectable } from '@angular/core';
import { Subject, throttleTime } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LastCallTimeService {
	// Observable sources
	private lastCallTime = new Subject<Date>();

	// Observable streams
	lastAPICall$ = this.lastCallTime.asObservable().pipe(throttleTime(1000));

	// Service message commands
	updateLastCall() {
		this.lastCallTime.next(new Date());
	}
}