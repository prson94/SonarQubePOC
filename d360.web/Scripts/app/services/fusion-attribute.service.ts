import { Injectable } from '@angular/core';
import { Headers, Http, Response, ResponseContentType } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { FusionAttributePagedResults, FusionAttributeValueDetails, FusionAttributeFilter } from '../models/fusion-attribute.model';
import { SortOrder } from '../models/enums.model';

@Injectable()
export class FusionAttributeService extends BaseService {
    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getFusionAttributes(fusionId: number, fusionAttributeTypeId: number, pageNumber?: number, pageSize?: number, sortField?: string, sortOrder?: SortOrder, filters?: FusionAttributeFilter[]): Promise<FusionAttributePagedResults> {
        let sortOrderText = '';

        if (sortOrder == SortOrder.Ascending) sortOrderText = 'asc';
        if (sortOrder == SortOrder.Descending) sortOrderText = 'desc';

        var url = `internal/fusion/ItemsByAttributeType?fusionID=${fusionId}&fusionAttributeTypeID=${fusionAttributeTypeId}&pagenum=${pageNumber ? pageNumber : 0}&pagesize=${pageSize ? pageSize : 20}&sortDataField=${sortField ? sortField : ''}&sortOrder=${sortOrderText}`;

        if (filters && filters.length > 0) {
            url += `&filterscount=${filters.length}`;

            let index = 0;
            for (let filter of filters) {
                url += `&filterdatafield${index}=${filter.dataField}&filtercondition${index}=${filter.condition}&filtervalue${index}=${filter.value}`;
                index++;
            }
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <FusionAttributePagedResults>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionQueryAttributes(fusionId: number, fusionQueryAttributeTypeId: number, pageNumber?: number, pageSize?: number, sortField?: string, sortOrder?: SortOrder, filters?: FusionAttributeFilter[]): Promise<FusionAttributePagedResults> {
        let sortOrderText = '';

        if (sortOrder == SortOrder.Ascending) sortOrderText = 'asc';
        if (sortOrder == SortOrder.Descending) sortOrderText = 'desc';

        var url = `internal/fusion/QueryItemsByAttributeType?fusionID=${fusionId}&fusionQueryAttributeTypeID=${fusionQueryAttributeTypeId}&pagenum=${pageNumber ? pageNumber : 0}&pagesize=${pageSize ? pageSize : 20}&sortDataField=${sortField ? sortField : ''}&sortOrder=${sortOrderText}`;

        if (filters && filters.length > 0) {
            url += `&filterscount=${filters.length}`;

            let index = 0;
            for (let filter of filters) {
                url += `&filterdatafield${index}=${filter.dataField}&filtercondition${index}=${filter.condition}&filtervalue${index}=${filter.value}`;
                index++;
            }
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <FusionAttributePagedResults>response.json())
            .catch(err => this.handleError(err));
    }
        
    getFusionAttributeExcel(type: string, fusionId: number, fusionQueryAttributeTypeId: number, sortField?: string, sortOrder?: SortOrder, filters?: FusionAttributeFilter[]) {
        let route = 'ExportItemsByAttributeType';
        if (type == 'FusionQueryAttributeType') {
            route = 'ExportQueryItemsByAttributeType';
        }

        let sortOrderText = '';

        if (sortOrder == SortOrder.Ascending) sortOrderText = 'asc';
        if (sortOrder == SortOrder.Descending) sortOrderText = 'desc';

        let url = `internal/fusion/${route}?fusionID=${fusionId}&${type}ID=${fusionQueryAttributeTypeId}&sortDataField=${sortField ? sortField : ''}&sortOrder=${sortOrderText}`;

        if (filters && filters.length > 0) {
            url += `&filterscount=${filters.length}`;

            let index = 0;
            for (let filter of filters) {
                url += `&filterdatafield${index}=${filter.dataField}&filtercondition${index}=${filter.condition}&filtervalue${index}=${filter.value}`;
                index++;
            }
        }

        this.http.get(url, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data));
    }

    downloadFile(data: Response) {
        var filename = `Fusion Data ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data.blob(), filename);
        }
        else {
            var url = window.URL.createObjectURL(data.blob());
            var anchor = document.createElement("a");
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }
    
    getFusionAttributeDetails(fusionAttributeId: number): Promise<FusionAttributeValueDetails> {
        return this.http.get(`internal/fusion/details/FusionAttribute/${fusionAttributeId}`)
            .toPromise()
            .then(response => <FusionAttributeValueDetails>response.json())
            .catch(err => this.handleError(err));
    }
}