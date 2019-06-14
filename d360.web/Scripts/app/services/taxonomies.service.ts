import { Injectable } from '@angular/core';
import { Taxonomy, TaxonomyLevel, TaxonomyClassification } from '../models/taxonomy.model';
import { JsonResult } from '../models/jsonresult.model';
import { HttpClient, HttpHeaderResponse, HttpHeaders } from '@angular/common/http';
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

    getTaxonomy(id: number): Observable<Taxonomy> {
        return this.http.get(`/api/catalogs/${id}`)
            .pipe(
                map(response => <Taxonomy>response),
                catchError(err => this.handleError(err))
            );
    }

    getTaxonomyClassifications(): Observable<TaxonomyClassification[]> {
        return this.http.get('/api/TaxonomyClassifications')
            .pipe(
                map(response => <TaxonomyClassification[]>response),
                catchError(err => this.handleError(err))
            );
    }

    saveTaxonomy(taxonomy: Taxonomy): Observable<JsonResult> {
        if (taxonomy.ID == undefined || !taxonomy.ID) {
            return this.post(taxonomy);
        }
        return this.put(taxonomy);
    }

    private updateTaxonomyWithId(taxonomy: Taxonomy, result: JsonResult): Taxonomy {
        taxonomy.ID = Number(result.id);
        return taxonomy;
    }

    private post(taxonomy: Taxonomy): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        return this.http
            .post("form/AddTaxonomyTypeRaw", JSON.stringify(taxonomy), { headers: headers })
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    private put(taxonomy: Taxonomy): Observable<JsonResult> {
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        return this.http
            .put('form/EditTaxonomyTypeRaw', JSON.stringify(taxonomy), { headers: headers })
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    deleteTaxonomy(taxonomyId: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, "TAXONOMYTYPE", taxonomyId);
    }
}


