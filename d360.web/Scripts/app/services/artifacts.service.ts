///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Artifacts } from '../models/artifacts.model';
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
}