import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import {
    MapItem,
    DiagramObjectType,
    LinkModel,
    NodeModel,
    Responsibility,
    TechnicalRelation,
    SourceRule,
    RelationItem,
    LineageEditorModelV2,
    LineageEditorTechnicalModel,
    LineagePreviewModel,
} from '../models/lineage.model';
import { ImpactDiagramModel } from '../models/impact.model';
import { HierarchyDiagramModel } from '../models/model.model';
import { JsonResult } from '../models/jsonresult.model';
import { Observable } from 'rxjs/Observable';

@Injectable()
export class LineageService extends BaseService {
    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    public getLineageDiagram(type: string, id: number): Promise<any> {
        return this.http.get(`services/relationships/${type}/${id}/lineage `)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public getLineageObjectTypes(): Promise<any> {
        return this.http.get('api/lineage/objectTypes')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public queryObjectTypes(type: string, id: number, query: string): Observable<any[]> {
        return this.http.get(`api/lineage/query/objects/${type}/${id}?query=${query}`)
            .map(response => <any[]>response.json());
    }

    public getLineageObjects(typeId: number): Promise<any> {
        return this.http.get(`api/lineage/objects/${typeId}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public getLineageObjectDetail(type: string, id: number): Promise<any> {
        return this.http.get(`resources/${type}/${id}/templates/tooltip/preview`)
            .toPromise()
            .catch(err => this.handleError(err));
    }

    public getLineageNodeDataForObject(type: string, id: number): Promise<any> {
        return this.http.get(`diagrams/${type}/${id}/lineagenode`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public postLineage(model: LineageEditorModelV2) {
        return this.http.post('diagrams/lineage/save', model)
            .toPromise()
            .catch(err => this.handleError(err));
    }
    
}