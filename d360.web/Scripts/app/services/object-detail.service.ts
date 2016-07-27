///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { DetailField, DetailRow, DetailModel, IObjectDetailService, Synonym, SynonymItem, SynonymEditorModel, SynonymEditModel, AttributeHeirarchyItem } from '../models/object-detail.model';
import { MessagesService } from './index';
import { BaseService } from './base.service';

@Injectable()
export class ObjectDetailService extends BaseService implements IObjectDetailService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getObjectDetail(objectID: number, objectType: string): Promise<DetailModel> {
        return this.http.get(`api/${objectType}/${objectID}/detail`)
            .toPromise()
            .then(response => <DetailModel>response.json())
            .catch(err=>this.handleError(err));
    }

    getObjectSynonyms(objectID: number, objectType: string): Promise<Synonym[]> {
        return this.http.get(`api/${objectType}/${objectID}/synonyms`)
            .toPromise()
            .then(response => <Synonym[]>response.json()).
            catch(err => this.handleError(err));
    }

    getSynonymOptions(objectID: number, objectType: string): Promise<SynonymEditorModel> {
        return this.http.get(`form/GetSynonyms?id=${objectID}&type=${objectType}`)
            .toPromise()
            .then(response => <SynonymEditorModel>response.json())
            .then(r => {
                r.items.forEach(i => {
                    i.ID = i[0].Value;
                    i.Name = i[1].Value;
                    i.TargetingSubject = i[2].Value;
                });
                return r;
            })
            .catch(err => this.handleError(err));
    }

    postSynonym(model: SynonymEditModel): Promise<any> {
        return this.http.post('form/AddSynonym', model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getAttributeHierarchyItems(objectID: number, objectType: string): Promise<AttributeHeirarchyItem[]> {
        return this.http.get(`attributes/hierarchy/${objectType}/${objectID}`)
            .toPromise()
            .then(response => <AttributeHeirarchyItem[]>response.json())
            .catch(err => this.handleError(err));
    }
}