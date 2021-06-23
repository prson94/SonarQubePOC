import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {ApiEndpoint, ApiField, ApiNamespace, ApiService, ApiUri, ApiVersion} from '../models/custom-api.model';
import {JsonResult} from '../models/jsonresult.model';

import {MessagesObservableService} from './messages-observable.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable({
    providedIn: 'root'
})
export class CustomAPIService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getServices(): Observable<ApiService[]> {
        return this
            .http
            .get(`api/custom/services`)
            .pipe(
                map(response => <ApiService[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getService(id: number): Observable<ApiService> {
        return this
            .http
            .get(`api/custom/service/${id}`)
            .pipe(
                map(response => <ApiService>response),
                catchError(err => this.handleError(err))
            );
    }

    getEndpoints(id: number): Observable<ApiEndpoint[]> {
        return this
            .http
            .get(`api/custom/service/${id}/endpoints`)
            .pipe(
                map(response => <ApiEndpoint[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getEndpoint(id: number): Observable<ApiEndpoint> {
        return this
            .http
            .get(`api/custom/endpoint/${id}`)
            .pipe(
                map(response => <ApiEndpoint>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteEndpoint(id: number): Observable<JsonResult> {
        return this
            .deleteDynamicWithResult(
                this.http,
                'endpoint',
                id
            );
    }

    getEndpointVersions(id: number): Observable<ApiVersion[]> {
        return this
            .http
            .get(`api/custom/endpoint/${id}/versions`)
            .pipe(
                map(response => <ApiVersion[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteEndpointVersion(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(
            this.http,
            'version',
            id
        );
    }

    getEndpointVersionFields(id: number): Observable<ApiField[]> {
        return this
            .http
            .get(`api/custom/version/${id}/fields`)
            .pipe(
                map(response => <ApiField[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getEndpointVersionUris(id: number): Observable<ApiUri[]> {
        return this
            .http
            .get(`api/custom/version/${id}/uritypes`)
            .pipe(
                map(response => <ApiUri[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getNamespaces(id: number): Observable<ApiNamespace[]> {
        return this
            .http
            .get(`api/custom/service/${id}/namespaces`)
            .pipe(
                map(response => <ApiNamespace[]>response),
                catchError(err => this.handleError(err))
            );
    }


    getEndpointVersionFieldEditorModel(id: number): Observable<any> {
        return this
            .http
            .get(`/api/v2/customendpoints/Version/FieldEditor/model?id=${id}`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    getEndpointVersionField_FieldTypes(versionId: number): Observable<any[]> {
        return this
            .http
            .get(`/api/v2/customendpoints/Version/FieldEditor/FieldTypes?versionId=${versionId}`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getEndpointVersionField_LookupFieldTypes(fieldTypeId: number): Observable<any[]> {
        return this
            .http
            .get(`/api/v2/customendpoints/Version/FieldEditor/LookupFields?fieldTypeId=${fieldTypeId}`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    saveEndpointVersionField(model: ApiField): Observable<any> {
        return this
            .http
            .post(`/api/v2/customendpoints/Version/FieldEditor/Field`, model)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteEndpointUri(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(
            this.http,
            'uri',
            id
        );
    }

    saveService(service: ApiService): Observable<JsonResult> {
        let methodName = "putDynamic"; /* as default value */

        if (service.ID == undefined || !service.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](
            this.http,
            'service',
            service
        );
    }

    deleteService(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(
            this.http,
            'service',
            id
        );
    }

    saveField(field: ApiField): Observable<JsonResult> {
        let methodName = "putDynamic"; /* as default value */

        if (field.ID == undefined || !field.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](
            this.http,
            'apifield',
            field
        );
    }

    deleteField(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(
            this.http,
            'apifield',
            id
        );
    }

    saveEndpoint(endpoint: ApiEndpoint): Observable<JsonResult> {
        let methodName = "putDynamic"; /* as default value */

        if (endpoint.ID == undefined || !endpoint.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](
            this.http,
            'endpoint',
            endpoint
        );
    }

    saveVersion(version: ApiVersion): Observable<JsonResult> {
        let methodName = "putDynamic"; /* as default value */

        if (version.ID == undefined || !version.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](
            this.http,
            'version',
            version
        );
    }

    saveEndpointUri(uri: ApiUri): Observable<JsonResult> {
        let methodName = "putDynamic"; /* as default value */

        if (uri.ID == undefined || !uri.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](
            this.http,
            'uri',
            uri
        );
    }

    saveNamespace(ns: ApiNamespace): Observable<JsonResult> {
        let methodName = "putDynamic"; /* as default value */

        if (ns.ID == undefined || !ns.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](
            this.http,
            'namespace',
            ns
        );
    }

    deleteNamespace(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(
            this.http,
            'namespace',
            id
        );
    }
}
