import { Observable } from "rxjs";
import { HttpClient, HttpHandler, HttpHeaders } from '@angular/common/http';
import {catchError, map} from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { Headers, Http, Response, ResponseContentType } from '@angular/http';

import { MessagesService } from './messages.service';
import { BaseObservableService } from './baseObservable.service';
import { Artifacts, Artifact } from '../models/artifacts.model';
import { ArtifactType, AssetTypeExportTemplate } from '../models/artifact-type.model';
import { SortOrder } from '../models/enums.model';
import {
    GridFilterExpression,
    GridRelationshipFilterExpression,
    GridFilterFieldType,
    GridAttributeFilterExpression,
    GridOwnerFilter
} from '../models/grid-definition.model';
import { Count } from '../models/counts.model';
import { JsonResult } from '../models/jsonresult.model';
import { AssetDetail } from '../models/asset.model';

@Injectable()
export class ArtifactService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getArtifacts(
        artifactTypeId: number,
        pagesize: number,
        pagenum: number,
        sortfield: string,
        sortorder: SortOrder,
        filters?: GridFilterExpression[],
        relationships?: GridRelationshipFilterExpression[],
        attributes?: GridAttributeFilterExpression[],
        simpleFilter?: string,
        owner?: GridOwnerFilter
    ): Observable<Artifacts> {
        let sortOrderText = sortorder == SortOrder.None ? "" : (sortorder == SortOrder.Descending ? "desc" : "asc");
        let uri = `internal/artifacts/ArtifactsByType?id=${artifactTypeId}&pagesize=${pagesize}&pagenum=${pagenum}&sortDataField=${sortfield}&sortOrder=${sortOrderText}`;

        if (filters != undefined) {
            //#region regular fields

            let normalFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Normal);
            let count = 0;
            uri += '&filterscount=' + normalFilters.length;

            for (let filter of normalFilters) {
                uri += `&filterdatafield${count}=${filter.field}&filtercondition${count}=${filter.condition}&filtervalue${count}=${encodeURIComponent(filter.value)}`;
                count++;
            }

            //#endregion

            //#region related filter fields
            let rellFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Relation);

            count = 0;

            uri += '&relfilterscount=' + rellFilters.length;

            for (let filter of rellFilters) {
                uri += `&relfilterdatafield${count}=${filter.field.replace("Field", "")}&relfiltercondition${count}=${filter.condition}&relfiltervalue${count}=${filter.value}`;
                count++;
            }
            //#endregion

            //#region hidden filter fields
            let hidFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Hidden);
            count = 0;

            uri += '&hidfilterscount=' + hidFilters.length;

            for (let filter of hidFilters) {
                uri += `&hidfilterdatafield${count}=${filter.field.replace("Field", "")}&hidfiltercondition${count}=${filter.condition}&hidfiltervalue${count}=${encodeURIComponent(filter.value)}`;
                count++;
            }
            //#endregion
        }

        if (attributes != undefined) {

            uri += '&attcount=' + attributes.length;

            let count = 0;
            for (let att of attributes) {
                uri += `&att_typeid_${count}=${att.attributeType}&att_value_${count}=${att.attributeSearchValue}`;
                count++;
            }
        }

        if (relationships != undefined) {

            uri += '&relcount=' + relationships.length;

            let count = 0;

            for (let rel of relationships) {
                uri += `&rel_typeid_${count}=${rel.relationshipType.IntersectTypeID}&rel_includetype_${count}=${rel.includeType}&rel_object_${count}=${rel.relationshipType.TargetType.replace("Type", "")}&rel_objectids_${count}=${rel.objectIds.join(",")}`;
                count++;
            }
        }
        if (simpleFilter != undefined) {
            uri += `&filter=${encodeURIComponent(simpleFilter)}`;
        }

        if (owner != undefined) {
            uri += `&ownerUsers=${owner.ownerUsers.join(',')}&ownerGroups=${owner.ownerGroups.join(',')}`;
        }
        
        return this
            .http
            .get(uri)
            .pipe(
                map(response => response),
                map(item => { return <Artifacts>item }),
                catchError(err => this.handleError(err))
            )
        ;
    }   

    getArtifactByParentAndArtifactType(
        parentId: number,
        artifactTypeId: number,
        filter: string,
        pagesize: number,
        pagenum: number,
        sortfield: string,
        sortorder: SortOrder
    ): Observable<Artifacts> {
        let sortOrderText = sortorder == SortOrder.None ? "" : (sortorder == SortOrder.Descending ? "desc" : "asc");
        let uri = `internal/artifacts/artifactsbyparent?parentID=${parentId}&childArtifactTypeID=${artifactTypeId}&pagesize=${pagesize}&pagenum=${pagenum}&sortDataField=${sortfield}&sortOrder=${sortOrderText}&filter=${filter ? filter : ''}`;

        return this
            .http
            .get(uri)
            .pipe(
                map(response => <Artifacts>response),
                catchError(err => this.handleError(err))
            )
        ;
    }

    getArtifactsXls(
        listableOnly: boolean,
        artifactType: ArtifactType,
        sortfield: string,
        sortorder: SortOrder,
        filters?: GridFilterExpression[],
        relationships?: GridRelationshipFilterExpression[],
        attributes?: GridAttributeFilterExpression[],
        simpleFilter?: string,
        owner?: GridOwnerFilter
    ) {
        const sortOrderText = sortorder == SortOrder.None ? "" : (sortorder == SortOrder.Descending ? "desc" : "asc");
        let uri = `internal/artifacts/download/excel/${artifactType.ID}.xls?&sortDataField=${sortfield}&sortOrder=${sortOrderText}&listableOnly=${listableOnly}`;
        
        if (filters != undefined) {
            //regular fields
            let normalFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Normal);
            let count = 0;
            uri += '&filterscount=' + normalFilters.length;

            for (let filter of normalFilters) {
                uri += `&filterdatafield${count}=${filter.field}&filtercondition${count}=${filter.condition}&filtervalue${count}=${filter.value}`;
                count++;
            }

            //related filter fields
            let rellFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Relation);
            count = 0;

            uri += '&relfilterscount=' + rellFilters.length;

            for (let filter of rellFilters) {
                uri += `&relfilterdatafield${count}=${filter.field.replace("Field", "")}&relfiltercondition${count}=${filter.condition}&relfiltervalue${count}=${filter.value}`;
                count++;
            }

            //hiden filter fields
            let hidFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Hidden);
            count = 0;

            uri += '&hidfilterscount=' + hidFilters.length;

            for (let filter of hidFilters) {
                uri += `&hidfilterdatafield${count}=${filter.field.replace("Field", "")}&hidfiltercondition${count}=${filter.condition}&hidfiltervalue${count}=${encodeURIComponent(filter.value)}`;
                count++;
            }
        }

        if (attributes != undefined) {
            uri += '&attcount=' + attributes.length;
            let count = 0;
            for (let att of attributes) {
                uri += `&att_typeid_${count}=${att.attributeType}&att_value_${count}=${att.attributeSearchValue}`;
                count++;
            }            
        }

        if (relationships != undefined) {
            uri += '&relcount=' + relationships.length;
            let count = 0;
            for (let rel of relationships) {
                uri += `&rel_typeid_${count}=${rel.relationshipType.IntersectTypeID}&rel_includetype_${count}=${rel.includeType}&rel_object_${count}=${rel.relationshipType.TargetType.replace("Type", "")}&rel_objectids_${count}=${rel.objectIds.join(",")}`;
                count++;
            }            
        } 

        if (simpleFilter != undefined) {
            uri += `&filter=${encodeURIComponent(simpleFilter)}`;
        }

        if (owner != undefined) {
            uri += `&ownerUsers=${owner.ownerUsers.join(',')}&ownerGroups=${owner.ownerGroups.join(',')}`;
        }

        this
            .http
            .get(
                uri,
                {
                    responseType: 'blob'
                }
            )
            .subscribe(data => this.downloadFile(data, artifactType.Name))
        ;
    }

    downloadFile(
        data: Blob,
        artifactTypeName: string
    ) {        
        var filename = `Filtered ${artifactTypeName} List ${new Date().toDateString()}.xlsx`;

        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename );
        } else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    getArtifact(id: number): Observable<Artifact> {
        return this
            .http
            .get(`api/artifact/${id}`)
            .pipe(
                map(response => <Artifact>response),
                catchError(err => this.handleError(err))
            )
        ;
    }

    deleteArtifact(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'artifact', id);
    }

    saveArtifact(artifact: any): Observable<JsonResult> {
        let methodName;

        if (artifact.ID == undefined || !artifact.ID) {
            methodName = 'postDynamic';
        } else {
            methodName = 'putDynamic';
        }

        return this[methodName](this.http, 'artifact', artifact);
    }

    getActivityCount(daysToLookBack: number): Observable<Count[]> {
        return this
            .http
            .get(`api/count/activity/${daysToLookBack}`)
            .pipe(
                map(response => <Count[]>response),
                catchError(err => this.handleError(err))
            )
        ;
    }

    getActivityDetails(
        artifactTypeId: number,
        daysToLookBack
    ): Observable<AssetDetail[]> {
        return this
            .http
            .get(`api/countitems/activity/${artifactTypeId}/${daysToLookBack}`)
            .pipe(
                map(response => <AssetDetail[]>response),
                catchError(err => this.handleError(err))
            )
        ;
    }

    getSimilarArtifactNames(
        typeID: number,
        query: string
    ): Observable<any[]> {
        return this
            .http
            .get(`form/Artifact_SimilarItems?typeID=${typeID}&query=${query}`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            )
        ;
    }

    requestCertification(objectId: number): Observable<JsonResult> {
        const headers = new HttpHeaders({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });
                
        return this.http
            .post(
                'form/RequestCertification',
                `ID=${objectId}`,
                {
                    headers: headers
                }
            )
            .pipe(
                map(res => <JsonResult>res),
                catchError(err => this.handleError(err))
            )
        ;
    }

    getArtifactsCustomXls(
        templateId: number,
        listableOnly: boolean,
        artifactType: ArtifactType,
        sortfield: string,
        sortorder: SortOrder,
        filters?: GridFilterExpression[],
        relationships?: GridRelationshipFilterExpression[],
        attributes?: GridAttributeFilterExpression[],
        simpleFilter?: string,
        owner?: GridOwnerFilter
    ) {
        let sortOrderText = sortorder == SortOrder.None ? "" : (sortorder == SortOrder.Descending ? "desc" : "asc");
        let uri = `internal/artifacts/download/customexcel/${templateId}/${artifactType.ID}.xls?&sortDataField=${sortfield}&sortOrder=${sortOrderText}&listableOnly=${listableOnly}`;
        
        if (filters != undefined) {
            //regular fields
            let normalFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Normal);
            let count = 0;
            uri += '&filterscount=' + normalFilters.length;

            for (let filter of normalFilters) {
                uri += `&filterdatafield${count}=${filter.field}&filtercondition${count}=${filter.condition}&filtervalue${count}=${filter.value}`;
                count++;
            }

            //related filter fields
            let rellFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Relation);
            count = 0;

            uri += '&relfilterscount=' + rellFilters.length;

            for (let filter of rellFilters) {
                uri += `&relfilterdatafield${count}=${filter.field.replace("Field", "")}&relfiltercondition${count}=${filter.condition}&relfiltervalue${count}=${filter.value}`;
                count++;
            }

            //hiden filter fields
            let hidFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Hidden);
            count = 0;

            uri += '&hidfilterscount=' + hidFilters.length;

            for (let filter of hidFilters) {
                uri += `&hidfilterdatafield${count}=${filter.field.replace("Field", "")}&hidfiltercondition${count}=${filter.condition}&hidfiltervalue${count}=${encodeURIComponent(filter.value)}`;
                count++;
            }
        }

        if (attributes != undefined) {
            uri += '&attcount=' + attributes.length;
            let count = 0;
            for (let att of attributes) {
                uri += `&att_typeid_${count}=${att.attributeType}&att_value_${count}=${att.attributeSearchValue}`;
                count++;
            }
        }

        if (relationships != undefined) {
            uri += '&relcount=' + relationships.length;
            let count = 0;
            for (let rel of relationships) {
                uri += `&rel_typeid_${count}=${rel.relationshipType.IntersectTypeID}&rel_includetype_${count}=${rel.includeType}&rel_object_${count}=${rel.relationshipType.TargetType.replace("Type", "")}&rel_objectids_${count}=${rel.objectIds.join(",")}`;
                count++;
            }
        }

        if (simpleFilter != undefined) {
            uri += `&filter=${encodeURIComponent(simpleFilter)}`;
        }

        if (owner != undefined) {
            uri += `&ownerUsers=${owner.ownerUsers.join(',')}&ownerGroups=${owner.ownerGroups.join(',')}`;
        }

        this
            .http
            .get(
                uri,
                {
                    responseType: 'blob'
                }
            )
            .subscribe(data => this.downloadFile(data, artifactType.Name));
    }
}
