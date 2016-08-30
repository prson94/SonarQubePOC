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
    ToolbarItemNg,
    ObjectDetail
} from '../models/object-detail.model';
import { HierarchyModel, PredicateType } from '../models/relations.model';
import { LookupGrid } from '../models/grid-definition.model';

@Injectable()
export class ObjectDetailService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getObjectDetail(objectID: number, objectType: string): Promise<any> {
        return this.http.get(`api/${objectType}/${objectID}/detail`)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    getObject(objectID: number, objectType: string): Promise<ObjectDetail> {
        return this.http.get(`api/${objectType}/${objectID}`)
            .toPromise()
            .then(response => <ObjectDetail>response.json())
            .catch(err => this.handleError(err));
    }

    getObjectSynonyms(objectID: number, objectType: string): Promise<Synonym[]> {
        return this.http.get(`api/${objectType}/${objectID}/synonyms`)
            .toPromise()
            .then(response => <Synonym[]>response.json())
            .catch(err => this.handleError(err));
    }

    getSynonymOptions(objectID: number, objectType: string): Promise<SynonymEditorModel> {
        return this.http.get(`form/SynonymsOptions?id=${objectID}&type=${objectType}`)
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
            let data = FormHelper.flattenTree(result, 'Items', 'ID', 'ParentUID');
            return FormHelper.formTree(data, 'ID', 'ParentUID');
        });

    }

    getAttributeActions(objectID: number, objectType: string, ownerID: number, ownerType: string, attributeID: number = null): Promise<ToolbarItemNg[]> {
        return this.http.get(`attributes/AttributeActionsNg?id=${objectID}&type=${objectType}&ownerID=${ownerID}&owner=${ownerID}&attributeID=${attributeID}`)
            .toPromise()
            .then(response => <ToolbarItemNg[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationsHierarchy(predicateType: PredicateType, type: string, id: number): Promise<HierarchyModel[]> {
        return this.http.get(`relations/hierarchy/${predicateType}/${type}/${id}`)
            .toPromise()
            .then(response => <HierarchyModel[]>response.json())
            .catch(err => this.handleError(err));
    }

    getRelationsHierarchyTree(predicateType: PredicateType, type: string, id: number): Promise<TreeNode[]> {
        return this.getRelationsHierarchy(predicateType, type, id).then(result => {
            return FormHelper.formTree(result, 'UID', 'ParentID');
        });
    }

    testDynamicParams(): Promise<any> {
        var params = [];
        params.push(1);
        params.push('bob');
        params.push(3);
        params.push(4);
        return this.http.post('form/dynamiceditor/new/attribute', params)
            .toPromise()
            .then(result => <any>result.json());
    }

    //TODO: make explicit call here instead of passing uri
    getLookupGrid(uri: string): Promise<LookupGrid> {
        return this.http.get(uri)
            .toPromise()
            .then(result => <LookupGrid>result.json())
            .catch(err => this.handleError(err));
    }

}