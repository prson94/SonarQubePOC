import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { JsonResult } from '../models/jsonresult.model';
import {
    FusionAttributeType,
    FusionType,
} from '../models/fusion.model';
import { GridColumn } from '../models/grid-definition.model';
import { SortOrder } from '../models/enums.model';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { FormHelper } from "../models/form.model";
import { TreeNode } from 'primeng/api';
import { AssetTypeStyle } from '../models/asset-type-style.model';

@Injectable()
export class FusionService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getFusionTypes(query: string = ''): Observable<FusionType[]> {
        return this
            .http
            .get(`services/fusion?${query}`)
            .pipe(
                map(response => <FusionType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAttributeTypes(id: number, query: string = ''): Observable<FusionAttributeType[]> {
        return this
            .http
            .get(`services/fusion/${id}/attributetypes?${query}`)
            .pipe(
                map(response => <FusionAttributeType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAttributeTypeTree(id: number, query: string = ''): Observable<TreeNode[]> {
        return this.getFusionAttributeTypes(id, query).pipe(
            map((r) => {
                return FormHelper.formTree(r);
            }),
            catchError(err => this.handleError(err))
        );
    }

    deleteFusionConfiguration(id: number): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'fusionconfiguration', id);
    }

    getFusionConfigurationsByType(id: number): Observable<any[]> {
        return this
            .http
            .get(`services/fusion/${id}/configurations?useFieldName=false`)
            .pipe(
                map(response => <any[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionConfigurationGridDefinition(id: number): Observable<GridColumn[]> {
        return this
            .http
            .get(`api/fusiontype/${id}/grid/definition`)
            .pipe(
                map(response => <GridColumn[]>response["Columns"]),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAttributeTypeList(fusionID: number): Observable<FusionAttributeType[]> {
        return this
            .http
            .get(`form/getfusionattributetypes?fusionID=${fusionID}`)
            .pipe(
                map(response => <FusionAttributeType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    postFusionType(
        fusionType: FusionType,
        assetStyle: AssetTypeStyle = null
    ): Observable<any> {
        return this
            .http
            .post('form/FusionType', { fusion: fusionType, style: assetStyle })
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    putFusionType(
        fusionType: FusionType,
        assetStyle: AssetTypeStyle = null
    ): Observable<any> {
        return this
            .http
            .put('form/FusionType', { fusion: fusionType, style: assetStyle })
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    postFusionAttributeType(
        fusionAttributeType: FusionAttributeType,
        assetStyle: AssetTypeStyle = null
    ): Observable<any> {
        return this
            .http
            .post('form/FusionAttributeType', { fusion: fusionAttributeType, style: assetStyle })
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    putFusionAttributeType(
        fusionAttributeType: FusionAttributeType,
        assetStyle: AssetTypeStyle = null
    ): Observable<any> {
        return this
            .http
            .put('form/FusionAttributeType', { fusion: fusionAttributeType, style: assetStyle })
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }






































    getFindAttributeTypes() {
        return this
            .http
            .get('services/fusion/attributetypes')
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    postRunMarkitLineage(id: number) {
        const url = `form/ScheduleMarkitLineage`;

        return this
            .http
            .post(
                url,
                { id: id }
            ).pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            )
            ;
    }
}
