import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class HrefClickService {
    private subject = new Subject<any>();

    sendEvent(event: any, data: any) {
        this.subject.next({ data: data, event: event });
    }

    getEvents(): Observable<any> {
        return this.subject.asObservable();
    }
}
