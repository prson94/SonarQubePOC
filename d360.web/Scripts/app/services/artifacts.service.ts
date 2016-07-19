///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Artifacts, Artifact } from '../models/artifacts.model';
import { ArtifactType } from '../models/artifact-type.model';
import { SortOrder } from '../models/enums.model';
import { GridFilterExpression } from '../models/grid-definition.model';

@Injectable()
export class ArtifactService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getArtifacts(artifactType: ArtifactType, pagesize: number, pagenum: number, sortfield: string, sortorder: SortOrder, filters?: GridFilterExpression[]): Promise<Artifacts> {
        let sortOrderText = sortorder == SortOrder.None ? "" : (sortorder == SortOrder.Descending ? "desc" : "asc");
        let uri = `artifacts/ArtifactsByType?id=${artifactType.ID}&pagesize=${pagesize}&pagenum=${pagenum}&sortDataField=${sortfield}&sortOrder=${sortOrderText}`;

        if (filters != undefined) {            
            let count = 0;
            uri += '&filterscount=' +filters.length;

            for (let filter of filters) {
                uri += `&filterdatafield${count}=${filter.field}&filtercondition${count}=${filter.condition}&filtervalue${count}=${filter.value}`;
                count++;
            }
        }


        return this.http.get(uri)        
            .toPromise()
            .then(response => <Artifacts>response.json())
            .catch(err => this.handleError(err));        
    }   

    getArtifactsXls(artifactType: ArtifactType) {                
        window.location.assign(`artifacts/download/excel/${artifactType.ID}.xls`)        
    }

    getArtifact(id: number): Promise<Artifact> {
        return this.http.get(`api/artifact/${id}?isNg=true`)
            .toPromise()
            .then(response => <Artifact>response.json())
            .catch(err => this.handleError(err));        
    }
}