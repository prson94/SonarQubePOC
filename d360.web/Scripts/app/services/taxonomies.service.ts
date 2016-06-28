///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Taxonomy, TaxonomyLevel } from '../models/taxonomy.model';

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
}


