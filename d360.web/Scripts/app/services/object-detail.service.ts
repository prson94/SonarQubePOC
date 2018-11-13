import { Injectable } from '@angular/core';
import { Headers, Http, ResponseContentType, Response } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { TreeNode } from 'primeng/components/common/api';
import { FormHelper } from '../models/form.model';
import { JsonResult } from '../models/jsonresult.model';
import {
    Classification,
    AssetDetail,
    DetailField,
    DetailRow,
    DetailModel,
    IObjectDetailService,
    NymType,
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

    getAsset(assetID: number): Promise<AssetDetail> {
        return this.http.get(`api/asset/${assetID}`)
            .toPromise()
            .then(response => <AssetDetail>response.json())
            .catch(err => this.handleError(err));
    }

    getObjectSynonyms(objectID: number, objectType: string, predicateId: number): Promise<Synonym[]> {
        return this.http.get(`api/${objectType}/${objectID}/${predicateId}/synonyms`)
            .toPromise()
            .then(response => <Synonym[]>response.json())
            .catch(err => this.handleError(err));
    }

    getSynonymTypes(objectID: number, objectType: string, predicateId: number): Promise<any> {
        return this.http.get(`form/SynonymTypes?id=${objectID}&type=${objectType}&predicateId=${predicateId}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getSynonymOptions(predicateId: number, type: string, typeId: number, object: string, objectId: number, query: string = ''): Promise<SynonymEditorModel> {
        
        return this.http.get(`form/SynonymsOptions?type=${type}&typeId=${typeId}&obj=${object}&objid=${objectId}&query=${query}&predicateId=${predicateId}`)
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
        let url = `attributes/actions/${objectType}/${objectID}/${ownerType}/${ownerID}/`;
        if (attributeID != null) {
            url += `${attributeID}`;
        }
        return this.http.get(url)
            .toPromise()
            .then(response => <ToolbarItemNg[]>response.json())
            .catch(err => this.handleError(err));
    }
        
    getLookupGrid(uri: string): Promise<LookupGrid> {
        return this.http.get(uri)
            .toPromise()
            .then(result => <LookupGrid>result.json())
            .catch(err => this.handleError(err));
    }

    getLookupGridExport(type: string, id: number, fieldTypeID: number, lookupType: number) {
        let uri = `api/dynamiclookup/export/${type}/${id}/${fieldTypeID}/${lookupType}/excel.xls`;
        this.http.get(uri, { responseType: ResponseContentType.Blob }).subscribe(d => this.downloadFile(d));              
    }

    downloadFile(data: Response) {
        var filename = `Item List ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data.blob(), filename);
        }
        else {
            var url = window.URL.createObjectURL(data.blob());
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    deleteSynonym(synonym: Synonym): Promise<JsonResult> {
        if (synonym.IntersectID)
            return this.deleteDynamicWithResult(this.http, 'synonym', synonym.IntersectID);
        return this.deleteDynamicWithResult(this.http, 'customsynonym', synonym.CustomID);       
    }

    postCustomSynonym(synonymName: string, predicateID: number, object: string, objectId: number): Promise<JsonResult> {
        var synonym: any = new Object();
        synonym.Name = synonymName;
        synonym.PredicateID = predicateID;
        synonym.Object = object;
        synonym.ObjectID = objectId;
        return this.postDynamic(this.http, 'customsynonym', synonym);
    }

    getNymAllocations(objectID: number, object: string): Promise<NymType[]> {
        return this.http.get(`api/${object}/${objectID}/NymAllocations`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    saveNymAllocations(objectID: number, object: string, nyms: NymType[]): Promise<JsonResult> {
        let model = { Object: object, ObjectID: objectID, PredicateIDs: nyms.filter(x => x.Enabled).map(function (a) { return a.ID; }) };        
        return this.http.post('form/AddNymAllocation', model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }


    getClassifications(objectType:string): Promise<Classification[]> {
        return this.http.get(`api/${objectType}?$orderby=Name`)
            .toPromise()
            .then(response => <Classification[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteClassification(id: number, objectType: string): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, objectType, id);
    }

    saveClassification(classification: Classification, objectType: string): Promise<JsonResult> {
        if (classification.ID == undefined || !classification.ID) {
            return this.postDynamic(this.http, objectType, classification);
        }
        return this.putDynamic(this.http, objectType, classification);
    }    
}