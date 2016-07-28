///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { TreeNode } from 'primeng/primeng';
import { FormHelper } from '../models/form.model';
import {
    DetailField,
    DetailRow,
    DetailModel,
    IObjectDetailService,
    Synonym,
    SynonymItem,
    SynonymEditorModel,
    SynonymEditModel,
    AttributeHeirarchyItem,
    ToolbarItem
} from '../models/object-detail.model';

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

    getAttributeHierarchyTree(objectID: number, objectType: string): Promise<TreeNode[]> {
        return this.getAttributeHierarchyItems(objectID, objectType).then(result => {
            let data = FormHelper.flattenTree(result, 'Items','ID','ParentUID');
            return FormHelper.formTree(data, 'ID', 'ParentUID');
        });

    }

    getAttributeActions(objectID: number, objectType: string, ownerID: number, ownerType: string, attributeID: number = null): Promise<ToolbarItem[]> {
        return this.http.get(`attributes/AttributeActions?id=${objectID}&type=${objectType}&ownerID=${ownerID}&owner=${ownerID}&attributeID=${attributeID}`)
            .toPromise()
            .then(response => <ToolbarItem[]>response.json())
            .catch(err => this.handleError(err));
    }
}