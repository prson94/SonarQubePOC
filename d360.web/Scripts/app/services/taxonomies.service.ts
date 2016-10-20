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

    getTaxonomyLevels(taxonomy: Taxonomy): Promise<TaxonomyLevel[]> {
        return this.http.get(`/api/TaxonomyType/${taxonomy.ID}/levels`)
            .toPromise()
            .then(response => <TaxonomyLevel[]>response.json())
            .catch(err => this.handleError(err));
    }

    saveTaxonomyLevel(level: TaxonomyLevel): Promise<JsonResult> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/AddTaxonomyTypeLevelRaw`;

        return this.http
            .post(url, JSON.stringify(level), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    editTaxonomyLevel(level: TaxonomyLevel): Promise<JsonResult> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/EditTaxonomyTypeLevelRaw`;

        return this.http
            .put(url, JSON.stringify(level), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err));
    }

    deleteTaxonomyLevel(taxonomyTypeId: number, taxonomyLevelId: number): Promise<JsonResult> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');
                
        let url = `form/TaxonomyType/${taxonomyTypeId}/levels/${taxonomyLevelId}`;

        return this.http
            .delete(url, headers)
            .toPromise()
            .then(res => <JsonResult>res.json())
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
            .catch(this.handleError);
    }

    private put(taxonomy: Taxonomy): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/json'
        });
        let url = `form/EditTaxonomyTypeRaw/${taxonomy.ID}`;
        return this.http
            .put(url, JSON.stringify(taxonomy), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }

    deleteTaxonomy(taxonomyId: number) {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/catalogs/${taxonomyId}`;

        return this.http
            .delete(url, headers)
            .toPromise()
            .catch(err => this.handleError(err));
    }
}


