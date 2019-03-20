import {Injectable} from '@angular/core';
import {MessagesService} from './messages.service';
import {
    LineageEditorModel,
    LineageEditorTechnicalModel,
    LineagePreviewModel,
    MapItem,
    RelationItem,
    Responsibility,
    SourceRule,
    TechnicalRelation,
} from '../models/lineage.model';
import {ImpactDiagramModel} from '../models/impact.model';
import {HierarchyDiagramModel} from '../models/model.model';
import {JsonResult} from '../models/jsonresult.model';
import {BaseObservableService} from "./baseObservable.service";
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

@Injectable()
export class DiagramService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    public getLineageDiagram(
        type: string,
        id: number,
        viewID: number,
        usageOnly: boolean
    ): Observable<any> {
        return this
            .http
            .get(`diagrams/${type}/${id}/lineage/${viewID}/${usageOnly}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    public getLineageSourceRules(
        source: string,
        sourceId: number,
        target: string,
        targetId: number
    ): Observable<SourceRule[]> {
        return this
            .http
            .get(`api/${source}/${sourceId}/sources/${target}/${targetId}/rules`)
            .pipe(
                map(response => <SourceRule[]>response),
                catchError(err => this.handleError(err))
            );
    }

    public getLineageSourceRulesFocal(
        focal: string,
        focalId: number,
        source: string,
        sourceId: number,
        target: string,
        targetId: number
    ): Observable<SourceRule[]> {
        return this
            .http
            .get(`api/${focal}/${focalId}/${source}/${sourceId}/${target}/${targetId}/rules`)
            .pipe(
                map(response => <SourceRule[]>response),
                catchError(err => this.handleError(err))
            );
    }

    public getLineageObjectDetail(
        type: string,
        id: number
    ): Observable<any> {
        /* FIXME: non using method */

        return this
            .http
            .get(`resources/${type}/${id}/templates/tooltip/preview`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    public getLineageTechnicalRelationships(
        source: string,
        sourceId: number,
        target: string,
        targetId: number
    ): Observable<TechnicalRelation[]> {
        return this
            .http
            .get(`relations/ChildRelationshipsBySourceAndTarget?s=${source}&sid=${sourceId}&t=${target}&tid=${targetId}`)
            .pipe(
                map(response => <TechnicalRelation[]>response),
                catchError(err => this.handleError(err))
            );
    }

    public getLineageResponsibilities(assetId: number): Observable<Responsibility[]> {
        return this
            .http
            .get(`api/${assetId}/ownership`)
            .pipe(
                map(response => <Responsibility[]>response),
                catchError(err => this.handleError(err))
            );
    }

    public getLineageMapItems(
        source: string,
        sourceId: number,
        target: string,
        targetId: number
    ): Observable<MapItem[]> {
        return this
            .http
            .get(`api/maps/${source}/${sourceId}/${target}/${targetId}/mapItems`)
            .pipe(
                map(response => <MapItem[]>response),
                catchError(err => this.handleError(err))
            );
    }

    public static getLineageMapItemsExport(
        source: string,
        sourceId: number,
        target: string,
        targetId: number
    ) {
        window.location.assign(`api/export/maps/${source}/${sourceId}/${target}/${targetId}/mapitems/excel.xls`);
    }

    public getLineageMapSequence(
        object: string,
        objectId: number
    ): Observable<any> {
        return this
            .http
            .get(`form/mapsequence/${object}/${objectId}/mapitems`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );

    }

    public postLineageMapSequence(
        object: string,
        objectId: number,
        model: any
    ): Observable<any> {
        return this
            .http
            .post(`form/mapsequence/${object}/${objectId}/mapitems`, model)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    public getImpactDiagram(
        object: string,
        objectId: number
    ): Observable<ImpactDiagramModel> {
        return this
            .http
            .get(`diagrams/${object}/${objectId}/ImpactAnalysis`)
            .pipe(
                map(response => <ImpactDiagramModel>response),
                catchError(err => this.handleError(err))
            );
    }

    public getImpactDiagramFusion(
        object: string,
        objectId: number
    ): Observable<ImpactDiagramModel> {
        /* FIXME: non using method */

        return this
            .http
            .get(`diagrams/${object}/${objectId}/ImpactAnalysisFusion`)
            .pipe(
                map(response => <ImpactDiagramModel>response),
                catchError(err => this.handleError(err))
            );
    }

    public getCatalogDiagram(id: number): Observable<HierarchyDiagramModel[]> {
        return this
            .http
            .get(`diagrams/${id}/InformationCatalogDiagramData`)
            .pipe(
                map(response => <HierarchyDiagramModel[]>response),
                catchError(err => this.handleError(err))
            );
    }

    public getRelations(
        object: string,
        objectId: number
    ): Observable<RelationItem[]> {
        return this
            .http
            .get(`api/${object}/${objectId}/relations`)
            .pipe(
                map(response => <RelationItem[]>response),
                catchError(err => this.handleError(err))
            );
    }

    public queryRelationshipTypes(query: string): Observable<any> {
        return this
            .http
            .get(`api/lineage/query/relationshiptypes?query=${query}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    public queryObjects(
        type: string,
        id: number,
        query: string
    ): Observable<any> {
        return this
            .http
            .get(`api/lineage/query/objects/${type}/${id}?query=${query}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    public queryFusionAttributes(query: string): Observable<any> {
        return this
            .http
            .get(`api/lineage/query/fusionattributes?query=${query}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    public deleteIntersect(intersectID: number) {
        /* FIXME: non using method */

        return this
            .http
            .delete(`form/DeleteIntersect?id=${intersectID}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    public updateLineage(model: LineageEditorModel): Observable<LineageEditorModel> {
        return this
            .http
            .post('form/UpdateLineage', model)
            .pipe(
                map(response => <LineageEditorModel>response),
                catchError(err => this.handleError(err))
            );
    }

    public updateTechnicalLineage(model: LineageEditorTechnicalModel): Observable<LineageEditorTechnicalModel> {
        return this
            .http
            .post('form/UpdateTechnicalLineage', model)
            .pipe(
                map(response => <LineageEditorTechnicalModel>response),
                catchError(err => this.handleError(err))
            );
    }

    public previewLineage(
        type: string,
        id: number,
        view: number,
        businessModel: LineageEditorModel = null,
        technicalModel: LineageEditorTechnicalModel = null
    ): Observable<any> {
        let model: LineagePreviewModel = new LineagePreviewModel();

        model.BusinessModel = businessModel;
        model.TechnicalModel = technicalModel;

        return this
            .http
            .post(`diagrams/${type}/${id}/lineagepreview/${view}`, model)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    public getLineageMappings(): Observable<any[]> {
        return this
            .http
            .get('api/lineage/mappings')
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    public deleteLineageMapping(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(
            this.http,
            'lineagemapping',
            id
        );
    }

    public saveLineageMapping(map: any): Observable<JsonResult> {
        let methodName = "putDynamic";

        if (map.ID == undefined || !map.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](
            this.http,
            'map',
            map
        );
    }
}
