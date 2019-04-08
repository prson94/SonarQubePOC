import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {
    FusionAttributeFilter,
    FusionAttributePagedResults,
    FusionAttributeValueDetails
} from '../models/fusion-attribute.model';
import {SortOrder} from '../models/enums.model';
import {JsonResult} from '../models/jsonresult.model';

import {MessagesService} from './messages.service';
import {BaseObservableService} from "./baseObservable.service";
import { AssetDataProfile } from '../models/fusion.model';

@Injectable()
export class FusionAttributeService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getFusionAttributes(
        fusionId: number,
        fusionAttributeTypeId: number,
        target?:string,
        pageNumber?: number,
        pageSize?: number,
        sortField?: string,
        sortOrder?: SortOrder,
        filters?: FusionAttributeFilter[]
    ): Observable<FusionAttributePagedResults> {
        let url;
        let sortOrderText = '';

        if (sortOrder == SortOrder.Ascending) {
            sortOrderText = 'asc';
        }
        if (sortOrder == SortOrder.Descending) {
            sortOrderText = 'desc';
        }

        url = `internal/fusion/ItemsByAttributeType?fusionID=${fusionId}&fusionAttributeTypeID=${fusionAttributeTypeId}&target=${target}&pagenum=${pageNumber ? pageNumber : 0}&pagesize=${pageSize ? pageSize : 20}&sortDataField=${sortField ? sortField : ''}&sortOrder=${sortOrderText}`;

        if (filters && filters.length > 0) {
            url += `&filterscount=${filters.length}`;

            let index = 0;
            for (let filter of filters) {
                url += `&filterdatafield${index}=${filter.dataField}&filtercondition${index}=${filter.condition}&filtervalue${index}=${encodeURIComponent(filter.value)}`;
                index++;
            }
        }

        return this
            .http
            .get(url)
            .pipe(
                map(response => <FusionAttributePagedResults>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionQueryAttributes(
        fusionId: number,
        fusionQueryAttributeTypeId: number,
        pageNumber?: number,
        pageSize?: number,
        sortField?: string,
        sortOrder?: SortOrder,
        filters?: FusionAttributeFilter[]
    ): Observable<FusionAttributePagedResults> {
        let url;
        let sortOrderText = '';

        if (sortOrder == SortOrder.Ascending) sortOrderText = 'asc';
        if (sortOrder == SortOrder.Descending) sortOrderText = 'desc';

        url = `internal/fusion/QueryItemsByAttributeType?fusionID=${fusionId}&fusionQueryAttributeTypeID=${fusionQueryAttributeTypeId}&pagenum=${pageNumber ? pageNumber : 0}&pagesize=${pageSize ? pageSize : 20}&sortDataField=${sortField ? sortField : ''}&sortOrder=${sortOrderText}`;

        if (filters && filters.length > 0) {
            url += `&filterscount=${filters.length}`;

            let index = 0;
            for (let filter of filters) {
                url += `&filterdatafield${index}=${filter.dataField}&filtercondition${index}=${filter.condition}&filtervalue${index}=${encodeURIComponent(filter.value)}`;
                index++;
            }
        }

        return this
            .http
            .get(url)
            .pipe(
                map(response => <FusionAttributePagedResults>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAttributeExcel(
        type: string,
        fusionId: number,
        fusionQueryAttributeTypeId: number,
        isDataProfile: boolean,
        sortField?: string,
        sortOrder?: SortOrder,
        filters?: FusionAttributeFilter[]
    ): Observable<any> {
        let url;
        let route = "ExportItemsByAttributeType";
        let sortOrderText = "";
        let dataProfile: string = "";

        if (type == 'FusionQueryAttributeType') {
            route = 'ExportQueryItemsByAttributeType';
        }

        if (sortOrder == SortOrder.Ascending) {
            sortOrderText = 'asc';
        }
        if (sortOrder == SortOrder.Descending) {
            sortOrderText = 'desc';
        }

        if (isDataProfile) {
            dataProfile = "&target=DataProfile";
        }

        url = `internal/fusion/${route}?fusionID=${fusionId}&${type}ID=${fusionQueryAttributeTypeId}&sortDataField=${sortField ? sortField : ''}&sortOrder=${sortOrderText}${dataProfile}`;

        if (filters && filters.length > 0) {
            url += `&filterscount=${filters.length}`;

            let index = 0;
            for (let filter of filters) {
                url += `&filterdatafield${index}=${filter.dataField}&filtercondition${index}=${filter.condition}&filtervalue${index}=${encodeURIComponent(filter.value)}`;
                index++;
            }
        }

        return this
            .http
            .get(url, {responseType: 'blob'})
            .pipe(
                map(data => this.downloadFile(data)),
                catchError(err => this.handleError(err))
            );
    }

    downloadFile(data: Blob) {
        var filename = `Fusion Data ${new Date().toDateString()}.xlsx`;

        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        } else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");

            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }

        return data;
    }

    getFusionAttributeDetails(
        fusionAttributeType: string,
        fusionAttributeId: number
    ): Observable<FusionAttributeValueDetails> {
        return this
            .http
            .get(`internal/fusion/details/${fusionAttributeType}/${fusionAttributeId}`)
            .pipe(
                map(response => <FusionAttributeValueDetails>response),
                catchError(err => this.handleError(err))
            );
    }

    getAssetDataProfile(
        profileId: number
    ): Observable<AssetDataProfile> {
        return this
            .http
            .get(`internal/fusion/dataprofile/${profileId}`)
            .pipe(
                map(response => <AssetDataProfile>response),
                catchError(err => this.handleError(err))
            );
    }

    getFusionAttributeProfile(
        type: string,
        id: number
    ): Observable<any> {
        return this
            .http
            .get(`internal/fusion/profile/${type}/${id}`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    saveAttribute(attribute: any): Observable<JsonResult> {
        let methodName = "putDynamic";

        if (attribute.ID == undefined || !attribute.ID) {
            methodName = "postDynamic";
        }

        return this[methodName](this.http, 'fusionattribute', attribute);
    }
}
