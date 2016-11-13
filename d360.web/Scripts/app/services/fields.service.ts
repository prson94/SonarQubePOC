import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { FieldDefinition, IFieldsService, FieldTypeEditorModel, Lookups, LookupItem } from '../models/fields.model';
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

    getFieldTypeEditor(id: number): Promise<any> {
        return this.http.get(`form/FieldType?id=${id}`)
            .toPromise()
            .then(response => <any>response.json())
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
                let i = this.ftItemToSelectItem(r.IntersectTypes);
                l.IntersectTypes = [];
                i.forEach(j => {
                    l.IntersectTypes.push({ value: j.value, label: j.label, id: null });
                });

                l.Lookups = this.ftItemToSelectItem(r.Lookups);
                l.Patterns = this.ftItemToSelectItem(r.Patterns);      
                l.ComplexLookupRelations = r.ComplexLookupRelations;          
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

    getFusionReferenceTypes(): SelectItem[] {
        return [
            { label: 'Self Reference', value: '1' },
            { label: 'Parent Reference', value: '2' },
            { label: 'Child Reference', value: '3' },
            { label: 'Relationship Reference', value: '4' },
        ];
    }

    getReferenceTypes(): SelectItem[] {
        return [
            { label: 'Self Reference', value: '1' },
            { label: 'Child Reference', value: '2' },
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

    getRelationLookupChildIntersectTypes(id: number): Promise<SelectItem[]> {
        return this.http.get(`form/FieldType_RelationLookup_ChildIntersectTypes?id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err)); 
    }

    getChildRelations(type: string, id: number): Promise<any> {
        return this.http.get(`form/FieldType_ComplexLookup_ChildItems?type=${type}&id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getParentRelations(type: string, id: number): Promise<any> {
        return this.http.get(`form/FieldType_ComplexLookup_ParentItems?type=${type}&id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getStandardRelations(type: string, id: number): Promise<any> {
        return this.http.get(`form/FieldType_ComplexLookup_IntersectTypes?type=${type}&id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    moveUp(type: string, id: number, fieldId: number) {
        return this.http.post(`fields/${type}/${id}/${fieldId}/move/up`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    moveDown(type: string, id: number, fieldId: number) {
        return this.http.post(`fields/${type}/${id}/${fieldId}/move/dpwn`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }
}



class FtItem {
    title: string;
    value: string;
}


