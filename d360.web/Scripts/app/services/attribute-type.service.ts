import {Injectable} from '@angular/core';
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {Headers, Http} from '@angular/http';
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {BaseObservableService} from "./baseObservable.service";
import {MessagesService} from './messages.service';
import {AttributeType, AttributeTypeAllocation} from '../models/attribute-type.model';
import {JsonResult} from '../models/jsonresult.model';
import {DropdownOption} from '../models/dropdown.model';

@Injectable()
export class AttributeTypeService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getAttributes(): Observable<AttributeType[]> {
        return this
            .http
            .get('attributes/fulltypes')
            .pipe(
                map(response => <AttributeType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getAttributeCategoryTypes(
        parentID?: number
    ): Observable<DropdownOption[]> {
        let url = `attributes/categories`;

        if (parentID != undefined) {
            url = `attributes/categories?parentID={parentID}`;
        }

        return this
            .http
            .get(url)
            .pipe(
                map(response => <DropdownOption[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getAttributeTypeAllocations(
        attributeTypeId: number
    ): Observable<AttributeTypeAllocation[]> {
        return this
            .http
            .get(`/api/AttributeType/${attributeTypeId}/allocations`)
            .pipe(
                map(response => <AttributeTypeAllocation[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteAttributeTypeAllocations(
        attributeTypeId: number,
        objectTypeId: number,
        objectType: string
    ): Observable<JsonResult> {
        const deleteAttributeTypeAllocationsUrl = `form/DeleteAttributeTypeRelationWithUri?AttributeTypeID=${attributeTypeId}&ObjectType=${encodeURIComponent(objectType)}&ObjectID=${objectTypeId}`;

        return this
            .http
            .delete(deleteAttributeTypeAllocationsUrl)
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    addAttributeTypeAllocations(
        objectTypeInfo: string,
        allowMultiple: boolean,
        attributeTypeId: number
    ): Observable<JsonResult> {
        const addAttributeTypeAllocationsHeaders = new HttpHeaders({
            /* pass as text since its a dynamic object and mvc has issue with dynamic models */
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        });
        const addAttributeTypeAllocationsUrl = 'form/AddAttributeTypeRelation';
        const addAttributeTypeAllocationsBody = `AllowMultipleEntries=${allowMultiple}&ObjectTypeInfo=${objectTypeInfo}&AttributeTypeID=${attributeTypeId}`;

        return this
            .http
            .post(
                addAttributeTypeAllocationsUrl,
                addAttributeTypeAllocationsBody,
                {headers: addAttributeTypeAllocationsHeaders}
            )
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    editAttributeTypeAllocations(
        objectTypeInfo: string,
        allowMultiple: boolean,
        attributeTypeId: number
    ): Observable<JsonResult> {
        const headers = new HttpHeaders({
            /* pass as text since its a dynamic object and mvc has issue with dynamic models */
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        });

        return this
            .http
            .put(
                'form/EditAttributeTypeRelation',
                `AllowMultipleEntries=${allowMultiple}&ObjectTypeInfo=${objectTypeInfo}&AttributeTypeID=${attributeTypeId}`,
                {headers: headers}
            )
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            );
    }

    getAttributeTypesForObject(
        objectType: string,
        objectId: number
    ): Observable<AttributeType[]> {
        return this
            .http
            .get(`/api/${objectType}/${objectId}/attributetypefilters`)
            .pipe(
                map(response => <AttributeType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getAttributeFilterValues(
        objectType: string,
        objectId: number,
        attributeId: number
    ): Observable<string[]> {
        return this
            .http
            .get(`/api/${objectType}/${objectId}/${attributeId}/attributefiltervalues`)
            .pipe(
                map(response => response),
                map((item) => item['Name']),
                catchError(err => this.handleError(err))
            );
    }

    deleteAttributeType(
        id: number
    ): Observable<JsonResult> {
        return this
            .deleteDynamicWithResult(this.http, 'attributetype', id);
    }

    saveAttributeType(
        attributeType: AttributeType
    ): Observable<JsonResult> {
        let methodName;

        if (attributeType.ID == undefined || !attributeType.ID) {
            methodName = "postDynamic";
        } else {
            methodName = "putDynamic";
        }

        return this[methodName](this.http, 'attributetype', attributeType);
    }
}
