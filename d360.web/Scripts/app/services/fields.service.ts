import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { FieldDefinition, IFieldsService, FieldTypeEditorModel, Lookups } from '../models/fields.model';
import { SelectItem } from 'primeng/primeng';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class FieldsService extends BaseService implements IFieldsService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService);  }

    getFields(objectID: number, objectType: string): Promise<FieldDefinition[]> {
        return this.http.get(`/fields/${objectType}/${objectID}/full`)
            .toPromise()
            .then(response => <FieldDefinition[]>response.json())
            .catch(err => this.handleError(err)); 
    }

    getFieldTypeEditor(id: number): Promise<FieldTypeEditorModel> {
        return this.http.get(`form/FieldType?id=${id}`)
            .toPromise()
            .then(response => <FieldTypeEditorModel>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionLookupDisplayFields(id: number): Promise<SelectItem[]> {
        return this.http.get(`form/FieldType_FusionLookup_DisplayFields?id=${id}`)
            .toPromise()
            .then(response => <FtItem[]>response.json())
            .then(r => this.ftItemToSelectItem(r))
            .catch(err => this.handleError(err));
    }

    getFusionLookupTargetAttributeTypes(sourceID: number, referenceTypeID: number): Promise<SelectItem[]> {
        return this.http.get(`form/FieldType_FusionLookup_TargetAttributeTypes?s=${sourceID}&r=${referenceTypeID}`)
            .toPromise()
            .then(response => <FtItem[]>response.json())
            .then(r => this.ftItemToSelectItem(r))
            .catch(err => this.handleError(err));
    }

    getRelationLookupChildIntersectTypes(id: number): Promise<SelectItem[]> {
        return this.http.get(`form/FieldType_RelationLookup_ChildIntersectTypes?id=${id}`)
            .toPromise()
            .then(response => <FtItem[]>response.json())
            .then(r => this.ftItemToSelectItem(r))
            .catch(err => this.handleError(err));
    }

    getRelationLookupDisplayFields(id: number, type: string, intersectTypeID: number): Promise<SelectItem[]> {
        return this.http.get(`form/FieldType_RelationLookup_DisplayFields?id=${id}&type=${type}&intersectTypeID=${intersectTypeID}`)
            .toPromise()
            .then(response => <FtItem[]>response.json())
            .then(r => this.ftItemToSelectItem(r))
            .catch(err => this.handleError(err));
    }

    getLookupTokens(id: number, type: string): Promise<SelectItem[]> {        
        return this.http.get(`form/FieldType_Lookup_Tokens?id=${id}&type=${type}`)
            .toPromise()
            .then(response => <FtItem[]>response.json())
            .then( r => this.ftItemToSelectItem(r))
            .catch(err => this.handleError(err));
    }

    getLookups(id: number, type: string): Promise<Lookups> {
        return this.http.get(`form/FieldType_Lookups?id=${id}&type=${type}&isNg=true`)
            .toPromise()
            .then(response => <any>response.json())
            .then(r => {                
                let l = new Lookups();
                l.DataTypes = this.ftItemToSelectItem(r.DataTypes);
                l.FusionAttributeTypes = this.ftItemToSelectItem(r.FusionAttributeTypes);
                l.IntersectTypes = this.ftItemToSelectItem(r.IntersectTypes);
                l.Lookups = this.ftItemToSelectItem(r.Lookups);
                l.Patterns = this.ftItemToSelectItem(r.Patterns);                
                return l;
            })
            .catch(err => this.handleError(err));
    } 

    getFormData(id: number): Promise<FieldTypeEditorModel> {
        return this.http.get(`form/FieldType_FormData?id=${id}`)
            .toPromise()
            .then(response => <FieldTypeEditorModel>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionDisplayFields(id: number): Promise<SelectItem[]> {
        return this.http.get(`form/FieldType_FusionLookup_DisplayFields?id=${id}`)
            .toPromise()
            .then(response => <FtItem[]>response.json())
            .then(r => this.ftItemToSelectItem(r))
            .catch(err => this.handleError(err));
    }

    getReferenceTypes(): SelectItem[] {
        return [
            { label: 'Self Reference', value: '1' },
            { label: 'Parent Reference', value: '2' },
            { label: 'Child Reference', value: '3' },
            { label: 'Relationship Reference', value: '4' },
        ];
    }

    putFieldType(model: FieldTypeEditorModel): Promise<JsonResult> {
        return this.http.put('form/EditFieldType', model)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    postFieldType(model: FieldTypeEditorModel): Promise<JsonResult> {
        return this.http.post('form/AddFieldType', model)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    deleteFieldType(id: number): Promise<JsonResult> {
        return this.http.delete(`form/DeleteFieldTypeByID?id=${id}`)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    private ftItemToSelectItem(items: FtItem[]): SelectItem[] {
        let s = new Array<SelectItem>();
        items.forEach(i => {
            s.push({label: i.title, value: i.value });
        });
        return s;
    }
}



class FtItem {
    title: string;
    value: string;
}


