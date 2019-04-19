import {Observable} from 'rxjs';
import {catchError, distinctUntilChanged, map, switchMap} from 'rxjs/operators';
import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {SelectItem} from 'primeng/components/common/api';

import {FieldDefinition, FieldTypeEditorModel, IFieldsService, Lookups} from '../models/fields.model';
import {EditorDropDownItem} from '../models/editor-field.model'
import {JsonResult} from '../models/jsonresult.model';

import {MessagesService} from './messages.service';
import {BaseObservableService} from "./baseObservable.service";


@Injectable()
export class FieldsService extends BaseObservableService implements IFieldsService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getFields(
        objectID: number,
        objectType: string
    ): Observable<FieldDefinition[]> {
        return this
            .http
            .get<FieldDefinition[]>(`/fields/${objectType}/${objectID}/full`)
            .pipe(
                map(response => <FieldDefinition[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getAssetTypeFields(assetTypeUID: string): Observable<FieldDefinition[]> {
        return this
            .http
            .get<FieldDefinition[]>(`/api/v2/assets/fields/${assetTypeUID}`)
            .pipe(
                map(response => <FieldDefinition[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFieldTypeEditor(id: number): Observable<any> {
        return this
            .http
            .get<any>(`form/FieldType?id=${id}`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionLookupDisplayFields(id: number): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`form/FieldType_FusionLookup_DisplayFields?id=${id}`)
            .pipe(
                map(response => <FtItem[]>response),
                map(r => this.ftItemToSelectItem(r)),
                catchError(err => this.handleError(err))
            );
    }

    getFusionLookupTargetAttributeTypes(
        sourceID: number,
        referenceTypeID: number
    ): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`form/FieldType_FusionLookup_TargetAttributeTypes?s=${sourceID}&r=${referenceTypeID}`)
            .pipe(
                map(response => <FtItem[]>response),
                map(r => this.ftItemToSelectItem(r)),
                catchError(err => this.handleError(err))
            );
    }

    getRelationObjectFields(
        type: string,
        id: number,
        intersectTypeID: number
    ): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`form/FieldType_FieldFromRelationship_Fields?type=${type}&id=${id}&intersectTypeID=${intersectTypeID}`)
            .pipe(
                map(response => <FtItem[]>response),
                map(r => this.ftItemToSelectItem(r)),
                catchError(err => this.handleError(err))
            );
    }

    getRelationLookupDisplayFields(
        id: number,
        type: string,
        intersectTypeID: number
    ): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`form/FieldType_RelationLookup_DisplayFields?id=${id}&type=${type}&intersectTypeID=${intersectTypeID}`)
            .pipe(
                map(response => <FtItem[]>response),
                map(r => this.ftItemToSelectItem(r)),
                catchError(err => this.handleError(err))
            );
    }

    getReferenceTypeHierarchyFields(
        id: number,
        objectType: string,
        objectId: number
    ): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`form/Reference_Hierarchy?id=${id}&objectType=${objectType}&objectId=${objectId}`)
            .pipe(
                map(response => <SelectItem[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getListFilterOptions(
        objectType: string,
        objectId: number,
        type: string,
        id: number
    ): Observable<any> {
        return this
            .http
            .get<any>(`form/FieldType_ListFilter?objectType=${objectType}&objectId=${objectId}&type=${type}&id=${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getLookupDefaultValueOptions(
        id: number,
        type: string
    ): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`form/FieldType_Lookup_DefaultValueOptions?id=${id}&type=${type}`)
            .pipe(
                map(r => this.ftItemToSelectItem(<FtItem[]>r)),
                catchError(err => this.handleError(err))
            );
    }


    getLookupTokens(
        id: number,
        type: string
    ): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`form/FieldType_Lookup_Tokens?id=${id}&type=${type}`)
            .pipe(
                map(r => this.ftItemToSelectItem(<FtItem[]>r)),
                catchError(err => this.handleError(err))
            );
    }

    getLookups(
        id: number,
        type: string
    ): Observable<Lookups> {
        return this
            .http
            .get<Lookups>(`form/FieldType_Lookups?id=${id}&type=${type}&isNg=true`)
            .pipe(
                map(response => <any>response),
                map(
                    r => {
                        let l = new Lookups();
                        let i = this.ftItemToSelectItem(r.IntersectTypes);

                        l.DataTypes = this.ftItemToSelectItem(r.DataTypes);
                        l.FusionAttributeTypes = this.ftItemToSelectItem(r.FusionAttributeTypes);
                        l.IntersectTypes = [];

                        i.forEach(j => {
                            l.IntersectTypes.push({value: j.value, label: j.label, id: null});
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
                    }
                ),
                catchError(err => this.handleError(err))
            );
    }

    getFormData(id: number): Observable<FieldTypeEditorModel> {
        return this
            .http
            .get<FieldTypeEditorModel>(`form/FieldType_FormData?id=${id}`)
            .pipe(
                map(response => <FieldTypeEditorModel>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionDisplayFields(id: number): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`form/FieldType_FusionLookup_DisplayFields?id=${id}`)
            .pipe(
                map(response => <FtItem[]>response),
                map(r => this.ftItemToSelectItem(r)),
                catchError(err => this.handleError(err))
            );
    }

    getFusionReferenceTypes(): SelectItem[] {
        return [
            {label: 'Self Reference', value: '1'},
            {label: 'Parent Reference', value: '2'},
            {label: 'Child Reference', value: '3'},
            {label: 'Relationship Reference', value: '4'},
        ];
    }

    getReferenceTypes(): SelectItem[] {
        return [
            {label: 'Self Reference', value: '1'},
            {label: 'Child Reference', value: '2'},
        ];
    }

    putFieldType(model: FieldTypeEditorModel): Observable<JsonResult> {
        return this
            .http
            .put('form/EditFieldType', model)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    postFieldType(model: FieldTypeEditorModel): Observable<JsonResult> {
        return this
            .http
            .post('form/AddFieldType', model)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteFieldType(id: number): Observable<JsonResult> {
        return this
            .http
            .delete(`form/DeleteFieldTypeByID?id=${id}`)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }

    private ftItemToSelectItem(items: FtItem[]): SelectItem[] {
        let s = new Array<SelectItem>();

        /* Empty value at beginning of list */
        items.forEach(i => {
            s.push({label: i.title, value: i.value});
        });

        return s;
    }

    getRelationLookupChildIntersectTypes(id: number): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`form/FieldType_RelationLookup_ChildIntersectTypes?id=${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getChildRelations(
        type: string,
        id: number
    ): Observable<any> {
        return this
            .http
            .get(`form/FieldType_ComplexLookup_ChildItems?type=${type}&id=${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getParentRelations(
        type: string,
        id: number
    ): Observable<any> {
        return this
            .http
            .get(`form/FieldType_ComplexLookup_ParentItems?type=${type}&id=${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getStandardRelations(
        type: string,
        id: number
    ): Observable<any> {
        return this
            .http
            .get(`form/FieldType_ComplexLookup_IntersectTypes?type=${type}&id=${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    moveUp(
        type: string,
        id: number,
        fieldId: number
    ) {
        return this
            .http
            .post(`fields/${type}/${id}/${fieldId}/move/up`, null)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    moveDown(
        type: string,
        id: number,
        fieldId: number
    ) {
        return this
            .http
            .post(`fields/${type}/${id}/${fieldId}/move/dpwn`, null)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getFilteredLookupDisplayFields(
        type: string,
        id: number,
        listType: string,
        listID: number
    ): Observable<any> {
        return this
            .http
            .get(`form/FieldType_FilteredLookup_DisplayFields?type=${type}&id=${id}&listType=${listType}&listID=${listID}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getCascadingListFieldValues(
        fieldTypeId: number,
        parentItemId?: string,
        parentValues?: string
    ): Observable<EditorDropDownItem[]> {
        parentItemId = (parentItemId != undefined) ? parentItemId : '';
        parentValues = (parentValues != undefined) ? encodeURIComponent(parentValues) : '';

        return this
            .http
            .get<EditorDropDownItem[]>(`api/FieldType_CascadingListValues/${fieldTypeId}?parentItemId=${parentItemId}&parentValues=${parentValues}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getRelationshipFieldIsListable(
        type: string,
        id: number,
        intersectTypeId
    ): Observable<boolean> {
        return this
            .http
            .get<boolean>(`form/FieldType_Relationship_IsListable?type=${type}&id=${id}&intersectTypeId=${intersectTypeId}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getRelationshipFieldItems(event: Observable<any>) {
        return event.pipe(
            distinctUntilChanged(),
            switchMap(
                event => {
                    let uri = `api/relationships/field/${event.fieldTypeID}?offset=${event.event.first}&rows=${event.event.rows}`;

                    if (event.event.globalFilter != null && event.event.globalFilter.length > 0) {
                        uri += `&query=${event.event.globalFilter}`
                    }

                    if (event.objectID != null) {
                        uri += `&object=${event.object}&objectID=${event.objectID}`
                    }

                    return this.http.get(uri).pipe(
                        map(res => res),
                        map(
                            res => {
                                return {fieldTypeID: event.fieldTypeID, results: res, event: event.event}
                            }
                        ),
                        catchError(err => this.handleError(err))
                    );
                }
            )
        );
    }

    getTypeaheadItems(e: Observable<any>): Observable<EditorDropDownItem[]> {
        return e.pipe(
            distinctUntilChanged(),
            switchMap(
                e => {
                    let uri = `form/FieldType_TypeAheadLookup?fieldTypeId=${e.fieldTypeID}&query=${e.event.query}`;

                    if (e.value != null) {
                        uri += `&value=${e.value}`;
                    }

                    return this.http.get(uri).pipe(
                        map(res => <EditorDropDownItem[]>res)
                    );
                }
            )
        );
    }

    getLookupFilteredByPredicate(
        fieldTypeID: number,
        objectType: string,
        id: number
    ): Observable<any> {
        return this
            .http
            .get<any>(`form/FieldType_Lookup_FilteredByPredicate?fieldTypeId=${fieldTypeID}&objectType=${objectType}&ObjectID=${id}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getTypeaheadFilteredByPredicateItems(e: Observable<any>, objectType: string, id: number): Observable<EditorDropDownItem[]> {
        return e.pipe(
            distinctUntilChanged(),
            switchMap(e => {
                let uri = `form/FieldType_Lookup_FilteredByPredicate?fieldTypeId=${e.fieldTypeID}&objectType=${objectType}&ObjectID=${id}&query=${e.event.query}`
                if (e.value != null)
                    uri += `&value=${e.value}`;
                return this.http.get(uri).pipe(map(res => <any[]>res["items"]));
            }));
    }

}

class FtItem {
    title: string;
    value: string;
}
