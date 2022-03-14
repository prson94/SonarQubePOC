import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, Observable } from 'rxjs';

export class AdvancedFilterUpdate {
    source: string;
    fieldName: string;
    values: any;
}
//use only for filters that are default filters(have is primary filters set to true)
@Injectable({ providedIn: 'root' })
export class AdvancedFilteringService {
    private subject = new Subject<AdvancedFilterUpdate>();

    constructor(private router: Router) { }

    updateFilter(data: AdvancedFilterUpdate) {
        this.subject.next(data);
    }

    onFilterUpdate(): Observable<AdvancedFilterUpdate> {
        return this.subject.asObservable();
    }
}
