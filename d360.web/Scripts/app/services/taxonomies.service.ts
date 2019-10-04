import { Injectable } from '@angular/core';
import { HierarchyType } from '../models/hierarchy.model';
import { HttpClient  } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class TaxonomiesService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getTaxonomies(): Observable<HierarchyType[]> {
        return this.http.get('/api/catalogs')
            .pipe(
                map(response => <HierarchyType[]>response),
                catchError(err => this.handleError(err))
            );
    }
}
