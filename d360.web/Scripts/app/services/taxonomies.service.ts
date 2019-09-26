import { Injectable } from '@angular/core';
import { Taxonomy } from '../models/taxonomy.model';
import { HttpClient  } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable()
export class TaxonomiesService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    getTaxonomies(): Observable<Taxonomy[]> {
        return this.http.get('/api/catalogs')
            .pipe(
                map(response => <Taxonomy[]>response),
                catchError(err => this.handleError(err))
            );
    }
}
