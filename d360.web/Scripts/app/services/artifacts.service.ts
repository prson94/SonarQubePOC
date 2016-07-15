///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Artifacts, Artifact } from '../models/artifacts.model';
import { ArtifactType } from '../models/artifact-type.model';
import { SortOrder } from '../models/enums.model';

@Injectable()
export class ArtifactService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getArtifacts(artifactType: ArtifactType, pagesize: number, pagenum: number, sortfield: string, sortorder: SortOrder): Promise<Artifacts> {
        let sortOrderText = sortorder == SortOrder.None ? "" : (sortorder == SortOrder.Descending ? "desc" : "asc");

        return this.http.get(`artifacts/ArtifactsByType?id=${artifactType.ID}&pagesize=${pagesize}&pagenum=${pagenum}&sortDataField=${sortfield}&sortOrder=${sortOrderText}`)        
            .toPromise()
            .then(response => <Artifacts>response.json())
            .catch(err => this.handleError(err));        
    }   

    getArtifactsXls(artifactType: ArtifactType) {                
        window.location.assign(`artifacts/download/excel/${artifactType.ID}.xls`)
        // triggers popup blocker and corrupts file
      /*  
        let headers = new Headers();
        
        headers.append('responseType', 'arraybuffer');
        this.http.get(`artifacts/download/excel/${artifactType.ID}.xls`, headers)
            .toPromise()
            .then(res => {
                var blob = new Blob([res], { type: "application/vnd.ms-excel" });
                var objectUrl = URL.createObjectURL(blob);
                window.open(objectUrl);
            })
            .catch(err => this.handleError(err));
        */
    }

    getArtifact(id: number): Promise<Artifact> {
        return this.http.get(`api/artifact/${id}?isNg=true`)
            .toPromise()
            .then(response => <Artifact>response.json())
            .catch(err => this.handleError(err));        
    }
}