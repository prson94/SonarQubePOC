import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { ApiService, ApiEndpoint, ApiVersion, ApiField, ApiUri, ApiNamespace } from '../models/custom-api.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class CustomAPIService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getServices(): Promise<ApiService[]> {
        return this.http.get(`api/custom/services`)
            .toPromise()
            .then(response => <ApiService[]>response.json())
            .catch(err => this.handleError(err));
    }

    getService(id: number): Promise<ApiService> {
        return this.http.get(`api/custom/service/${id}`)
            .toPromise()
            .then(response => <ApiService>response.json())
            .catch(err => this.handleError(err));
    }

    getEndpoints(id: number): Promise<ApiEndpoint[]> {
        return this.http.get(`api/custom/service/${id}/endpoints`)
            .toPromise()
            .then(response => <ApiEndpoint[]>response.json())
            .catch(err => this.handleError(err));
    }

    getEndpoint(id: number): Promise<ApiEndpoint> {
        return this.http.get(`api/custom/endpoint/${id}`)
            .toPromise()
            .then(response => <ApiEndpoint>response.json())
            .catch(err => this.handleError(err));
    }

    deleteEndpoint(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'endpoint', id);
    }

    getEndpointVersions(id: number): Promise<ApiVersion[]> {
        return this.http.get(`api/custom/endpoint/${id}/versions`)
            .toPromise()
            .then(response => <ApiVersion[]>response.json())
            .catch(err => this.handleError(err));
    }

    deleteEndpointVersion(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'version', id);
    }

    getEndpointVersionFields(id: number): Promise<ApiField[]> {
        return this.http.get(`api/custom/version/${id}/fields`)
            .toPromise()
            .then(response => <ApiField[]>response.json())
            .catch(err => this.handleError(err));
    }

    getEndpointVersionUris(id: number): Promise<ApiUri[]> {
        return this.http.get(`api/custom/version/${id}/uritypes`)
            .toPromise()
            .then(response => <ApiUri[]>response.json())
            .catch(err => this.handleError(err));
    }

    getNamespaces(id: number): Promise<ApiNamespace[]> {
        return this.http.get(`api/custom/service/${id}/namespaces`)
            .toPromise()
            .then(response => <ApiNamespace[]>response.json())
            .catch(err => this.handleError(err));
    }


    getEndpointVersionFieldEditorModel(id: number): Promise<any> {
        return this.http.get(`/api/v2/customendpoints/Version/FieldEditor/model?id=${id}`)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    getEndpointVersionField_FieldTypes(versionId: number) : Promise<any[]> {
        return this.http.get(`/api/v2/customendpoints/Version/FieldEditor/FieldTypes?versionId=${versionId}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    getEndpointVersionField_LookupFieldTypes(fieldTypeId: number): Promise<any[]> {
        return this.http.get(`/api/v2/customendpoints/Version/FieldEditor/LookupFields?fieldTypeId=${fieldTypeId}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }

    saveEndpointVersionField(model: ApiField): Promise<any> {
        return this.http.post(`/api/v2/customendpoints/Version/FieldEditor/Field`, model)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    deleteEndpointUri(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'uri', id);
    }

    saveService(service: ApiService): Promise<JsonResult> {
        if (service.ID == undefined || !service.ID) {
            return this.postDynamic(this.http, 'service', service);
        }
        return this.putDynamic(this.http, 'service', service);
    }

    deleteService(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'service', id);
    }

    saveField(field: ApiField): Promise<JsonResult> {
        if (field.ID == undefined || !field.ID) {
            return this.postDynamic(this.http, 'apifield', field);
        }
        return this.putDynamic(this.http, 'apifield', field);        
    }

    deleteField(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'apifield', id);
    }

    saveEndpoint(endpoint: ApiEndpoint): Promise<JsonResult> {
        if (endpoint.ID == undefined || !endpoint.ID) {
            return this.postDynamic(this.http, 'endpoint', endpoint);
        }
        return this.putDynamic(this.http, 'endpoint', endpoint);
    }

    saveVersion(version: ApiVersion): Promise<JsonResult> {
        if (version.ID == undefined || !version.ID) {
            return this.postDynamic(this.http, 'version', version);
        }
        return this.putDynamic(this.http, 'version', version);
    }

    saveEndpointUri(uri: ApiUri): Promise<JsonResult> {
        if (uri.ID == undefined || !uri.ID) {
            return this.postDynamic(this.http, 'uri', uri);
        }
        return this.putDynamic(this.http, 'uri', uri);
    }

    saveNamespace(ns: ApiNamespace): Promise<JsonResult> {
        if (ns.ID == undefined || !ns.ID) {
            return this.postDynamic(this.http, 'namespace', ns);
        }
        return this.putDynamic(this.http, 'namespace', ns);
    }

    deleteNamespace(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'namespace', id);
    }
}