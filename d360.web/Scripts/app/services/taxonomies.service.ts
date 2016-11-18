import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Taxonomy, TaxonomyLevel, TaxonomyClassification } from '../models/taxonomy.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class TaxonomiesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getTaxonomies(): Promise<Taxonomy[]> {
        return this.http.get('/api/catalogs')
            .toPromise()
            .then(response => <Taxonomy[]>response.json())
            .catch(err => this.handleError(err));
    }    

    getTaxonomy(id: number): Promise<Taxonomy> {
        return this.http.get(`/api/catalogs/${id}`)
            .toPromise()
            .then(response => <Taxonomy>response.json())
            .catch(err => this.handleError(err));
    }   

    getTaxonomyClassifications(): Promise<TaxonomyClassification[]> {
        return this.http.get('/api/TaxonomyClassifications')
            .toPromise()
            .then(response => <TaxonomyClassification[]>response.json())
            .catch(err => this.handleError(err));
    }

    saveTaxonomy(taxonomy: Taxonomy): Promise<JsonResult> {                
        if (taxonomy.ID == undefined || !taxonomy.ID) {
            return this.post(taxonomy);
        }
        return this.put(taxonomy);                    
    }    

    private updateTaxonomyWithId(taxonomy: Taxonomy, result: JsonResult): Taxonomy {
        taxonomy.ID = Number(result.id);
        return taxonomy;
    }

    private post(taxonomy: Taxonomy): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });
        return this.http
            .post("form/AddTaxonomyTypeRaw", JSON.stringify(taxonomy), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    private put(taxonomy: Taxonomy): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });
        
        return this.http
            .put('form/EditTaxonomyTypeRaw', JSON.stringify(taxonomy), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    deleteTaxonomy(taxonomyId: number): Promise<JsonResult> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/catalogs/${taxonomyId}`;

        return this.http
            .delete(url, headers)
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }
}


