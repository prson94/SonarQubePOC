
import {distinctUntilChanged, map, switchMap} from 'rxjs/operators';
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
import { Observable } from 'rxjs';

@Injectable()
export class LineageService extends BaseService {
    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    public getLineageDiagram(type: string, id: number): Promise<any> {
        return this.http.get(`services/relationships/${type}/${id}/lineage `)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public postLineageDiagram(model: LineageEditorModelV2): Promise<any> {
        return this.http.post(`services/relationships/save/lineage`, model)
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

    public getLineageIntersectTypes(): Promise<any> {
        return this.http.get('api/lineage/intersectTypes')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public queryObjectTypes(type: string, id: number, query: string): Observable<any[]> {
        return this.http.get(`api/lineage/query/objects/${type}/${id}?query=${query}`).pipe(
            map(response => <any[]>response.json()));
    }

    public getLineageObjects(event: Observable<any>) {
        let uri = `api/lineage/objects/`;
        return event.pipe(
            distinctUntilChanged(),
            switchMap(event => {
                let uri = `api/lineage/objects/${event.assetTypeId}?offset=${event.event.first}&rows=${event.event.rows}`;

                if (event.event.globalFilter != null && event.event.globalFilter.length > 0)
                    uri += `&query=${event.event.globalFilter}`;
                return this.http.get(uri).pipe(map(res => res.json()),
                    map(res => { return { assetTypeId: event.assetTypeId, results: res, event: event.event }}),);
            }),);
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
    
}