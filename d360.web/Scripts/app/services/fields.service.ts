
import {distinctUntilChanged, switchMap, map} from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { FieldDefinition, IFieldsService, FieldTypeEditorModel, Lookups, LookupItem } from '../models/fields.model';
import { EditorDropDownItem } from '../models/editor-field.model'
import { SelectItem } from 'primeng/components/common/api';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { JsonResult } from '../models/jsonresult.model';
import { Observable } from 'rxjs';

@Injectable()
export class FieldsService extends BaseService implements IFieldsService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService);  }

    getFields(objectID: number, objectType: string): Promise<FieldDefinition[]> {
        return this.http.get(`/fields/${objectType}/${objectID}/full`)
            .toPromise()
            .then(response => <FieldDefinition[]>response.json())
            .catch(err => this.handleError(err));
    }

    getAssetTypeFields(assetTypeUID: string): Promise<FieldDefinition[]> {
        return this.http.get(`/api/v2/assets/fields/${assetTypeUID}`)
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

    getRelationObjectFields(type: string, id: number, intersectTypeID: number): Promise<SelectItem[]> {
        return this.http.get(`form/FieldType_FieldFromRelationship_Fields?type=${type}&id=${id}&intersectTypeID=${intersectTypeID}`)
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

    getReferenceTypeHierarchyFields(id: number, objectType: string, objectId: number): Promise<SelectItem[]> {
        return this.http.get(`form/Reference_Hierarchy?id=${id}&objectType=${objectType}&objectId=${objectId}`)
            .toPromise()
            .then(response => <SelectItem[]>response.json())            
            .catch(err => this.handleError(err));
    }

    getListFilterOptions(objectType: string, objectId: number, type: string, id: number ): Promise<any> {
        return this.http.get(`form/FieldType_ListFilter?objectType=${objectType}&objectId=${objectId}&type=${type}&id=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getLookupDefaultValueOptions(id: number, type: string): Promise<SelectItem[]> {
        return this.http.get(`form/FieldType_Lookup_DefaultValueOptions?id=${id}&type=${type}`)
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

                l.Field_Relationships = this.ftItemToSelectItem(r.Field_Relationships);
                l.Field_CardinalRelationships = this.ftItemToSelectItem(r.Field_CardinalRelationships);
                l.Field_CardinalReferenceRelationships = this.ftItemToSelectItem(r.Field_CardinalReferenceRelationships);
                l.Field_FieldFromRelRelationships = this.ftItemToSelectItem(r.Field_FieldFromRelRelationships);
                l.Lookups = this.ftItemToSelectItem(r.Lookups);
                l.Patterns = this.ftItemToSelectItem(r.Patterns);      
                l.ComplexLookupRelations = r.ComplexLookupRelations;
                l.FilteredLookups = r.FilteredLookups;          
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
        //s.push({ label: '', value: '' }); //Empty value at beginning of list
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

    getFilteredLookupDisplayFields(type: string, id: number, listType: string, listID: number): Promise<any> {
        return this.http.get(`form/FieldType_FilteredLookup_DisplayFields?type=${type}&id=${id}&listType=${listType}&listID=${listID}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getCascadingListFieldValues(fieldTypeId: number, parentItemId?: string, parentValues?: string): Promise<EditorDropDownItem[]> {
        return this.http.get(`api/FieldType_CascadingListValues/${fieldTypeId}?parentItemId=${parentItemId != undefined ? parentItemId : ''}&parentValues=${parentValues != undefined ? encodeURIComponent(parentValues) : ''}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getRelationshipFieldIsListable(type: string, id: number, intersectTypeId): Promise<boolean> {
        return this.http.get(`form/FieldType_Relationship_IsListable?type=${type}&id=${id}&intersectTypeId=${intersectTypeId}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getRelationshipFieldItems(event: Observable<any>) {
        return event.pipe(
            distinctUntilChanged(),
            switchMap(event => {
                let uri = `api/relationships/field/${event.fieldTypeID}?offset=${event.event.first}&rows=${event.event.rows}`;
                if (event.event.globalFilter != null && event.event.globalFilter.length > 0)
                    uri += `&query=${event.event.globalFilter}`
                if (event.objectID != null)
                    uri += `&object=${event.object}&objectID=${event.objectID}`
                return this.http.get(uri).pipe(map(res => res.json()),
                    map(res => { return { fieldTypeID: event.fieldTypeID, results: res, event: event.event } }),);
            }),);
    }

    getTypeaheadItems(e: Observable<any>): Observable<EditorDropDownItem[]> {
        return e.pipe(
            distinctUntilChanged(),
            switchMap(e => {
                let uri = `form/FieldType_TypeAheadLookup?fieldTypeId=${e.fieldTypeID}&query=${e.event.query}`;
                if (e.value != null)
                    uri += `&value=${e.value}`;
                return this.http.get(uri).pipe(map(res => <EditorDropDownItem[]>res.json()));
            }),);
    }

    getLookupFilteredByPredicate(fieldTypeID: number, objectType: string, id:number): Promise<any> {
        return this.http.get(`form/FieldType_Lookup_FilteredByPredicate?fieldTypeId=${fieldTypeID}&objectType=${objectType}&ObjectID=${id}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getTypeaheadFilteredByPredicateItems(e: Observable<any>, objectType: string, id: number): Observable<EditorDropDownItem[]> {
        return e.pipe(
            distinctUntilChanged(),
            switchMap(e => {
                let uri = `form/FieldType_Lookup_FilteredByPredicate?fieldTypeId=${e.fieldTypeID}&objectType=${objectType}&ObjectID=${id}&query=${e.event.query}`
                if (e.value != null)
                    uri += `&value=${e.value}`;
                return this.http.get(uri).pipe(map(res => <any[]>res.json().items));
            }));
    }

}

class FtItem {
    title: string;
    value: string;
}