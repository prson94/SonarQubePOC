import { Observable } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { SelectItem } from 'primeng/api';
import { FieldDefinition, Lookups, IFieldsService, FieldTypeEditorModel } from '../models/fields.model';
import { EditorDropDownItem } from '../models/editor-field.model'
import { JsonResult } from '../models/jsonresult.model';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { ApiResult, ErrorResponse } from '../models/apiresult.model';
import { FieldType, FieldTypeAPIModel, FieldTypeAPIModelField } from '../models/fieldtype-api.model';

@Injectable({
    providedIn: 'root'
})
export class FieldsObservableService extends BaseObservableService implements IFieldsService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
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

    getFieldsV2(assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string, fieldName: string = ""): Observable<FieldTypeAPIModelField[]> {
        let url = "";
        if (assetTypeUid)
            url = `AssetTypeUid=${assetTypeUid}`;
        if (actionTypeUid)
            url = `ActionTypeUid=${actionTypeUid}`;
        if (relationshipTypeUid)
            url = `RelationshipTypeUid=${relationshipTypeUid}`;

        if (fieldName) {
            url += `&Name=${fieldName}`;
        }

        return this
            .http
            .get<any>(`api/v2/fields?${url}`)
            .pipe(
                map(response => <FieldTypeAPIModelField[]>response.items),
                catchError(err => this.handleError(err))
            );
    }

    putFieldsV2(
        model: FieldTypeAPIModel
    ): any {
        return this
            .http
            .put<any>(`api/v2/fields`, model)
            .pipe(
                map(response => <any>response),
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

    getComplexField

    getFieldTypeEditor(name: string, assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string): Observable<FieldTypeAPIModelField> {
        let url = "";
        if (assetTypeUid)
            url = `AssetTypeUid=${assetTypeUid}`;
        if (actionTypeUid)
            url = `ActionTypeUid=${actionTypeUid}`;
        if (relationshipTypeUid)
            url = `RelationshipTypeUid=${relationshipTypeUid}`;

        return this
            .http
            .get<any>(`api/v2/fields?${url}&Name=${name}`)
            .pipe(
                map(response => <FieldTypeAPIModelField>response.items[0]),
                catchError(err => this.handleError(err))
            );
    }

    getRelationObjectFields(assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string, intersectTypeUid: string): Observable<SelectItem[]> {
        let url = "";
        if (assetTypeUid)
            url = `intersectTypeUid=${intersectTypeUid}&AssetTypeUid=${assetTypeUid}`;
        if (actionTypeUid)
            url = `intersectTypeUid=${intersectTypeUid}&ActionTypeUid=${actionTypeUid}`;
        if (relationshipTypeUid)
            url = `intersectTypeUid=${intersectTypeUid}&RelationshipTypeUid=${relationshipTypeUid}`;

        return this
            .http
            .get<SelectItem[]>(`api/v2/fields/GetFieldFromRelationshipFields?${url}`)
            .pipe(
                map((response) => <FtItem[]>response),
                map((r) => this.ftItemToSelectItem(r)),
                catchError((err) => this.handleError(err))
            );
    }

    getRelationLookupDisplayFields(assetTypeUid: string, intersectTypeUid: string): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`api/v2/fields/GetRelationLookupDisplayFields?assetTypeUid=${assetTypeUid}&intersectTypeUid=${intersectTypeUid}`)
            .pipe(
                map(response => <FtItem[]>response),
                map(r => this.ftItemToSelectItem(r)),
                catchError(err => this.handleError(err))
            );
    }

    getReferenceTypeHierarchyFields(uid: string, assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string): Observable<SelectItem[]> {
        let url = "";
        if (assetTypeUid)
            url = `uid=${uid}&AssetTypeUid=${assetTypeUid}`;
        if (actionTypeUid)
            url = `uid=${uid}&ActionTypeUid=${actionTypeUid}`;
        if (relationshipTypeUid)
            url = `uid=${uid}&RelationshipTypeUid=${relationshipTypeUid}`;

        return this
            .http
            .get<SelectItem[]>(`api/v2/fields/GetReferenceHierarchy?${url}`)
            .pipe(
                map(response => <SelectItem[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getListFilterOptions(uid: string, assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string): Observable<any> {
        let url = "";
        if (assetTypeUid)
            url = `uid=${uid}&AssetTypeUid=${assetTypeUid}`;
        if (actionTypeUid)
            url = `uid=${uid}&ActionTypeUid=${actionTypeUid}`;
        if (relationshipTypeUid)
            url = `uid=${uid}&RelationshipTypeUid=${relationshipTypeUid}`;

        return this
            .http
            .get<any>(`api/v2/fields/GetLookupListFilter?${url}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getLookupDefaultValueOptions(
        Uid: string
    ): Observable<SelectItem[]> {
        return this
            .http
            .get<SelectItem[]>(`api/v2/fields/GetLookupDefaultValues?Uid=${Uid}`)
            .pipe(
                map(r => this.ftItemToSelectItem(<FtItem[]>r)),
                catchError(err => this.handleError(err))
            );
    }


    getLookupTokens(uid: string): Observable<Array<SelectItem>> {
        return this
            .http
            .get<Array<SelectItem>>(`api/v2/fields/GetFieldTypeLookupTokens?identifier=${uid}`)
            .pipe(
                map(r => this.ftItemToSelectItem(<FtItem[]>r)),
                catchError(err => this.handleError(err))
            );
    }

    getLookups(assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string): Observable<Lookups> {
        let url = "";
        if (assetTypeUid)
            url = `AssetTypeUid=${assetTypeUid}`;
        if (actionTypeUid)
            url = `ActionTypeUid=${actionTypeUid}`;
        if (relationshipTypeUid)
            url = `RelationshipTypeUid=${relationshipTypeUid}`;

        return this
            .http
            .get<Lookups>(`api/v2/fields/GetLookups?${url}`)
            .pipe(
                map(response => <any>response),
                map(
                    (r) => {
                        let l = new Lookups();
                        let i = this.ftItemToSelectItem(r.IntersectTypes);

                        l.DataTypes = this.ftItemToSelectItem(r.DataTypes);
                        l.FusionAttributeTypes = this.ftItemToSelectItem(r.FusionAttributeTypes);
                        l.IntersectTypes = [];

                        i.forEach((j) => {
                            l.IntersectTypes.push({ value: j.value, label: j.label, id: null });
                        });

                        l.Field_Relationships = this.ftItemToSelectItem(r.Field_Relationships);
                        l.Field_CardinalRelationships = this.ftItemToSelectItem(r.Field_CardinalRelationships);
                        l.Field_CardinalReferenceRelationships = this.ftItemToSelectItem(r.Field_CardinalReferenceRelationships);
                        l.Field_FieldFromRelRelationships = this.ftItemToSelectItem(r.Field_FieldFromRelRelationships);
                        l.Field_JsonDataTypes = this.ftItemToSelectItem(r.Field_JsonDataTypes);
                        l.Field_JsonFields = this.ftItemToSelectItem(r.Field_JsonFields);
                        l.FieldResponsibilityTypes = this.ftItemToSelectItem(r.Field_ResponsibilityTypes == null ? [] : r.Field_ResponsibilityTypes);
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

    getFormData(name: string, assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string): Observable<FieldTypeEditorModel> {
        let url = "";
        if (assetTypeUid)
            url = `AssetTypeUid=${assetTypeUid}`;
        if (actionTypeUid)
            url = `ActionTypeUid=${actionTypeUid}`;
        if (relationshipTypeUid)
            url = `RelationshipTypeUid=${relationshipTypeUid}`;
        return this
            .http
            .get<FieldTypeEditorModel>(`api/v2/fields/GetFieldTypeFormData?name=${name}&${url}`)
            .pipe(
                map(response => <FieldTypeEditorModel>response),
                catchError(err => this.handleError(err))
            );
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

    deleteFieldType(name: string, assetTypeUid?: string, actionTypeUid?: string, relationshipTypeUid?: string): Observable<ApiResult & ErrorResponse> {
        const options = {
            headers: new HttpHeaders({
                'Content-Type': 'application/json',
            }),
            body: {
                Fields: [{ Name: name }],
                ActionTypeUid: actionTypeUid,
                AssetTypeUid: assetTypeUid,
                RelationshipTypeUid: relationshipTypeUid
            },
        };

        return this
            .http
            .delete('api/v2/fields', options)
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }


    private ftItemToSelectItem(items: FtItem[]): SelectItem[] {
        let s = new Array<SelectItem>();

        /* Empty value at beginning of list */
        items.forEach((i) => {
            if (typeof i.value === "undefined") {
                i.value = null;
            }
            s.push({ label: i.title, value: i.value });
        });

        return s;
    }

    getChildRelations(assetTypeUid: string): Observable<any> {
        let url = `AssetTypeUid=${assetTypeUid}`;
        return this
            .http
            .get(`api/v2/fields/GetChildRelations?${url}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getParentRelations(assetTypeUid: string): Observable<any> {
        let url = `AssetTypeUid=${assetTypeUid}`;
        return this
            .http
            .get(`api/v2/fields/GetParentRelations?${url}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getStandardRelations(assetTypeUid: string): Observable<any> {
        let url = `AssetTypeUid=${assetTypeUid}`;
        return this
            .http
            .get(`api/v2/fields/GetStandardRelations?${url}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getTechnicalRelations(intersectTypeUid: string): Observable<any> {
        let url = `intersectTypeUid=${intersectTypeUid}`;
        return this
            .http
            .get(`api/v2/fields/technicalrelationships?${url}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    moveUp(typeUid: string, fieldTypeName: string) {
        let model = {
            TypeUid: typeUid,
            FieldTypename: fieldTypeName,
            Direction: "up"
        }
        return this
            .http
            .post(`api/v2/fields/move`, model)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    moveDown(typeUid: string, fieldTypeName: string) {
        let model = {
            TypeUid: typeUid,
            FieldTypename: fieldTypeName,
            Direction: "down"
        }
        return this
            .http
            .post(`api/v2/fields/move`, model)
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

    getRelationshipFieldIsListable(intersectTypeUid: string, assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string): Observable<any> {
        let url = "";
        if (assetTypeUid)
            url = `intersectTypeUid=${intersectTypeUid}&assetTypeUid=${assetTypeUid}`;
        if (actionTypeUid)
            url = `intersectTypeUid=${intersectTypeUid}&actionTypeUid=${actionTypeUid}`;
        if (relationshipTypeUid)
            url = `intersectTypeUid=${intersectTypeUid}&relationshipTypeUid=${relationshipTypeUid}`;
        return this
            .http
            .get<any>(`api/v2/fields/IsListableRelationship?${url}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getRelationshipFieldItems(event: Observable<any>) {
        return event.pipe(
            distinctUntilChanged(),
            switchMap(
                (event) => {
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
                            (res) => {
                                return { fieldTypeID: event.fieldTypeID, results: res, event: event.event }
                            }
                        ),
                        catchError(err => this.handleError(err))
                    );
                }
            )
        );
    }

    getTypeaheadItems(e: Observable<any>, useColor: boolean = false): Observable<EditorDropDownItem[]> {
        return e.pipe(
            distinctUntilChanged(),
            switchMap(
                (e) => {
                    let uri = `form/FieldType_TypeAheadLookup?fieldTypeId=${e.fieldTypeID}&query=${e.event.query}&useColor=${useColor}`;

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
            switchMap((e) => {
                let uri = `form/FieldType_Lookup_FilteredByPredicate?fieldTypeId=${e.fieldTypeID}&objectType=${objectType}&ObjectID=${id}&query=${e.event.query}`
                if (e.value != null)
                    uri += `&value=${e.value}`;
                return this.http.get(uri).pipe(map(res => <any[]>res["items"]));
            }));
    }

    getTypeaheadJsonPropertyOptionsForJsonField(fieldName: string, phrase: string, assetTypeUid: string, actionTypeUid: string, relationshipTypeUid: string): Observable<string[]> {
        let url = "";
        if (assetTypeUid)
            url = `assetTypeUid=${assetTypeUid}`;
        if (actionTypeUid)
            url = `actionTypeUid=${actionTypeUid}`;
        if (relationshipTypeUid)
            url = `relationshipTypeUid=${relationshipTypeUid}`;
        return this.http.get(`form/FieldType_TypeaheadJsonPropertyOptionsForJsonField?fieldName=${fieldName}&phrase=${encodeURIComponent(phrase)}&${url}`)
            .pipe(
                map(response => <string[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getAvailableScoreTypes(assetTypeUid: string) {
        return this
            .http
            .get(`api/v2/fields/GetAvailableScoreTypes?assetTypeUid=${assetTypeUid}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );

    }

    getLookupValues(assetTypeUid: string, fieldName: string, params: any): Observable<any> {
        var qString = '';
        if (params) {
            qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
            if (qString)
                qString = '?' + qString;
        }

        let url = `api/v2/fields/${assetTypeUid}/lookupvalues/${fieldName}` + qString;

        return this
            .http
            .get(url)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getLookupValuesForComplexField(assetUid: string, fieldName: string, filterName: string, params: any): Observable<any> {
        var qString = '';
        if (params) {
            qString = Object.keys(params).map(key => key + '=' + params[key]).join('&');
            if (qString)
                qString = '?' + qString;
        }

        let url = `api/v2/fields/${assetUid}/complexLookupvalues/${fieldName}/filter/${filterName}` + qString;

        return this
            .http
            .get(url)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );

    }

    getComplexFieldFieldTypes(assetUid: string, fieldName: string): Observable<FieldTypeAPIModelField[]> {
        let url = `api/v2/fields/${assetUid}/complexlookupfields/${fieldName}`;

        return this
            .http
            .get(url)
            .pipe(
                map((response) => <FieldTypeAPIModelField[]>response["items"]),
                catchError((err) => this.handleError(err))
            );

    }
}

class FtItem {
    title: string;
    value: string;
}
