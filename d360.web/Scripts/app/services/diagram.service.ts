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
    LineageEditorModel,
    LineageEditorTechnicalModel,
    LineagePreviewModel,
} from '../models/lineage.model';
import { ImpactDiagramModel } from '../models/impact.model';
import { HierarchyDiagramModel } from '../models/model.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class DiagramService extends BaseService {
    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    public getLineageDiagram(type: string, id: number, viewID: number, usageOnly: boolean): Promise<any> {
        return this.http.get(`diagrams/${type}/${id}/lineage/${viewID}/${usageOnly}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public getLineageSourceRules(source: string, sourceId: number, target: string, targetId: number): Promise<SourceRule[]> {
        return this.http.get(`api/${source}/${sourceId}/sources/${target}/${targetId}/rules`)
            .toPromise()
            .then(response => <SourceRule[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getLineageSourceRulesFocal(focal: string, focalId: number, source: string, sourceId: number, target: string, targetId: number): Promise<SourceRule[]> {
        return this.http.get(`api/${focal}/${focalId}/${source}/${sourceId}/${target}/${targetId}/rules`)
            .toPromise()
            .then(response => <SourceRule[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getLineageObjectDetail(type: string, id: number): Promise<any> {
        return this.http.get(`resources/${type}/${id}/templates/tooltip/preview?isNg=true`)
            .toPromise()
            .catch(err => this.handleError(err));
    }

    public getLineageTechnicalRelationships(source: string, sourceId: number, target: string, targetId: number): Promise<TechnicalRelation[]> {
        return this.http.get(`relations/ChildRelationshipsBySourceAndTarget?s=${source}&sid=${sourceId}&t=${target}&tid=${targetId}`)
            .toPromise()
            .then(response => <TechnicalRelation[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getLineageResponsibilities(type: string, id: number, showHidden = false): Promise<Responsibility[]> {
        return this.http.get(`api/${type}/${id}/ownership?showHidden=${showHidden}`)
            .toPromise()
            .then(response => <Responsibility[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getLineageMapItems(source: string, sourceId: number, target: string, targetId: number): Promise<MapItem[]> {
        return this.http.get(`api/maps/${source}/${sourceId}/${target}/${targetId}/mapItems`)
            .toPromise()
            .then(response => <MapItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getLineageMapItemsExport(source: string, sourceId: number, target: string, targetId: number) {
        window.location.assign(`api/export/maps/${source}/${sourceId}/${target}/${targetId}/mapitems/excel.xls`);
    }

    public getLineageMapSequence(object: string, objectId: number): Promise<any> {
        return this.http.get(`form/mapsequence/${object}/${objectId}/mapitems`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));

    }

    public postLineageMapSequence(object: string, objectId: number, model: any): Promise<any> {
        return this.http.post(`form/mapsequence/${object}/${objectId}/mapitems`, model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public getImpactDiagram(object: string, objectId: number): Promise<ImpactDiagramModel> {
        return this.http.get(`diagrams/${object}/${objectId}/ImpactAnalysis`)
            .toPromise()
            .then(response => <ImpactDiagramModel>response.json())
            .catch(err => this.handleError(err));
    }

    public getImpactDiagramFusion(object: string, objectId: number): Promise<ImpactDiagramModel> {
        return this.http.get(`diagrams/${object}/${objectId}/ImpactAnalysisFusion`)
            .toPromise()
            .then(response => <ImpactDiagramModel>response.json())
            .catch(err => this.handleError(err));
    }

    public getCatalogDiagram(id: number): Promise<HierarchyDiagramModel[]> {
        return this.http.get(`diagrams/${id}/InformationCatalogDiagramData`)
            .toPromise()
            .then(response => <HierarchyDiagramModel[]>response.json())
            .catch(err => this.handleError(err));
    }

    public getRelations(object: string, objectId: number): Promise<RelationItem[]> {
        return this.http.get(`api/${object}/${objectId}/relations`)
            .toPromise()
            .then(response => <RelationItem[]>response.json())
            .catch(err => this.handleError(err));
    }

    public queryRelationshipTypes(query: string): Promise<any> {
        return this.http.get(`api/lineage/query/relationshiptypes?query=${query}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public queryObjects(type: string, id: number, query: string): Promise<any> {
        return this.http.get(`api/lineage/query/objects/${type}/${id}?query=${query}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public queryFusionAttributes(query: string): Promise<any> {
        return this.http.get(`api/lineage/query/fusionattributes?query=${query}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public deleteIntersect(intersectID: number) {
        return this.http.delete(`form/DeleteIntersect?id=${intersectID}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public updateLineage(model: LineageEditorModel): Promise<LineageEditorModel> {
        return this.http.post('form/UpdateLineage', model)
            .toPromise()
            .then(response => <LineageEditorModel>response.json())
            .catch(err => this.handleError(err));
    }

    public updateTechnicalLineage(model: LineageEditorTechnicalModel): Promise<LineageEditorTechnicalModel> {
        return this.http.post('form/UpdateTechnicalLineage', model)
            .toPromise()
            .then(response => <LineageEditorTechnicalModel>response.json())
            .catch(err => this.handleError(err));
    }

    public previewLineage(type: string, id: number, view: number, businessModel: LineageEditorModel = null, technicalModel: LineageEditorTechnicalModel = null): Promise<any> {
        let model: LineagePreviewModel = new LineagePreviewModel();
        model.BusinessModel = businessModel;
        model.TechnicalModel = technicalModel;
        return this.http.post(`diagrams/${type}/${id}/lineagepreview/${view}`, model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public getLineageMappings() : Promise<any[]> {
        return this.http.get('api/lineage/mappings')
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    public deleteLineageMapping(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'lineagemapping', id);
    }

    public saveLineageMapping(map: any): Promise<JsonResult> {
        if (map.ID == undefined || !map.ID) {
            return this.postDynamic(this.http, 'map', map);
        }
        return this.putDynamic(this.http, 'map', map);
    }
}