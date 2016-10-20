import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { Artifacts, Artifact } from '../models/artifacts.model';
import { ArtifactType } from '../models/artifact-type.model';
import { SortOrder } from '../models/enums.model';
import { GridFilterExpression, GridRelationshipFilterExpression, GridFilterFieldType, GridAttributeFilterExpression } from '../models/grid-definition.model';
import { Count } from '../models/counts.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class ArtifactService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }
    
    getArtifacts(artifactTypeId: number, pagesize: number, pagenum: number, sortfield: string, sortorder: SortOrder, filters?: GridFilterExpression[], relationships?: GridRelationshipFilterExpression, attributes?: GridAttributeFilterExpression, simpleFilter?:string ): Promise<Artifacts> {
        let sortOrderText = sortorder == SortOrder.None ? "" : (sortorder == SortOrder.Descending ? "desc" : "asc");
        let uri = `internal/artifacts/ArtifactsByType?id=${artifactTypeId}&pagesize=${pagesize}&pagenum=${pagenum}&sortDataField=${sortfield}&sortOrder=${sortOrderText}`;

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
                uri += `&relfilterdatafield${count}=${filter.field.replace("Field","")}&relfiltercondition${count}=${filter.condition}&relfiltervalue${count}=${filter.value}`;
                count++;
            }

            //hiden filter fields
            let hidFilters = filters.filter(f => f.fieldtype == GridFilterFieldType.Hidden);
            count = 0;

            uri += '&hidfilterscount=' + hidFilters.length;

            for (let filter of hidFilters) {
                uri += `&hidfilterdatafield${count}=${filter.field.replace("Field","")}&hidfiltercondition${count}=${filter.condition}&hidfiltervalue${count}=${filter.value}`;
                count++;
            }
        }

        if (attributes != undefined) {
            uri += `&AttributeSearchValue=${attributes.attributeSearchValue}&AttributeType=${attributes.attributeType}`;
        }

        if (relationships != undefined) {
            uri += `&RelationshipIncludeType=${relationships.includeType}&RelationshipObjectType=${relationships.relationshipType.TargetType.replace("Type", "")}&RelationshipObjectIDs=${relationships.objectIds.join(",")}`;
        }

        if (simpleFilter != undefined) {
            uri += `&filter=${simpleFilter}`;
        }

        return this.http.get(uri)        
            .toPromise()
            .then(response => <Artifacts>response.json())
            .catch(err => this.handleError(err));        
    }   

    getArtifactsXls(artifactType: ArtifactType) {                
        window.location.assign(`internal/artifacts/download/excel/${artifactType.ID}.xls`);        
    }

    getArtifact(id: number): Promise<Artifact> {
        return this.http.get(`api/artifact/${id}?isNg=true`)
            .toPromise()
            .then(response => <Artifact>response.json())
            .catch(err => this.handleError(err));        
    }

    getActivityCount(daysToLookBack: number): Promise<Count[]> {
        return this.http.get(`api/count/activity/${daysToLookBack}`)
            .toPromise()
            .then(response => <Count[]>response.json())
            .catch(err => this.handleError(err));
    }

    getActivityDetails(artifactTypeId: number, daysToLookBack): Promise<Artifact[]> {
        return this.http.get(`api/countitems/activity/${artifactTypeId}/${daysToLookBack}`)
            .toPromise()
            .then(response => <Artifact[]>response.json())
            .catch(err => this.handleError(err));
    }

    requestCertification(objectId: number): Promise<JsonResult> {
        let headers = new Headers({
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', //pass as text since its a dynamic object and mvc has issue with dynamic models                        
        });

        this.addRequestVerificationHeaders(headers);

        return this.http
            .post('form/RequestCertification', `ID=${objectId}`, { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }

    getSimilarArtifactNames(typeID: number, query: string): Promise<any[]> {
        return this.http.get(`form/Aritfact_SimilarItems?typeID=${typeID}&query=${query}`)
            .toPromise()
            .then(response => <any[]>response.json())
            .catch(err => this.handleError(err));
    }
}