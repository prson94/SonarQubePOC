import { Injectable } from '@angular/core';
import { Headers, Http, Response, ResponseContentType } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { FusionAttributePagedResults, FusionAttributeValueDetails, FusionAttributeFilter } from '../models/fusion-attribute.model';
import { SortOrder } from '../models/enums.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class FusionAttributeService extends BaseService {
    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getFusionAttributes(fusionId: number, fusionAttributeTypeId: number,target?:string, pageNumber?: number, pageSize?: number, sortField?: string, sortOrder?: SortOrder, filters?: FusionAttributeFilter[]): Promise<FusionAttributePagedResults> {
        let sortOrderText = '';

        if (sortOrder == SortOrder.Ascending) sortOrderText = 'asc';
        if (sortOrder == SortOrder.Descending) sortOrderText = 'desc';

        var url = `internal/fusion/ItemsByAttributeType?fusionID=${fusionId}&fusionAttributeTypeID=${fusionAttributeTypeId}&target=${target}&pagenum=${pageNumber ? pageNumber : 0}&pagesize=${pageSize ? pageSize : 20}&sortDataField=${sortField ? sortField : ''}&sortOrder=${sortOrderText}`;

        if (filters && filters.length > 0) {
            url += `&filterscount=${filters.length}`;

            let index = 0;
            for (let filter of filters) {
                url += `&filterdatafield${index}=${filter.dataField}&filtercondition${index}=${filter.condition}&filtervalue${index}=${encodeURIComponent(filter.value)}`;
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
                url += `&filterdatafield${index}=${filter.dataField}&filtercondition${index}=${filter.condition}&filtervalue${index}=${encodeURIComponent(filter.value)}`;
                index++;
            }
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <FusionAttributePagedResults>response.json())
            .catch(err => this.handleError(err));
    }
        
    getFusionAttributeExcel(type: string, fusionId: number, fusionQueryAttributeTypeId: number, sortField?: string, sortOrder?: SortOrder, filters?: FusionAttributeFilter[]): Promise<any> {
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
                url += `&filterdatafield${index}=${filter.dataField}&filtercondition${index}=${filter.condition}&filtervalue${index}=${encodeURIComponent(filter.value)}`;
                index++;
            }
        }
        
        return this.http.get(url, { responseType: ResponseContentType.Blob })
            .toPromise()
            .then(data => this.downloadFile(data))
            .catch(err => this.handleError(err));
    }

    downloadFile(data: Response) {
        var filename = `Fusion Data ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data.blob(), filename);
        }
        else {
            var url = window.URL.createObjectURL(data.blob());
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
        return data;
    }
    
    getFusionAttributeDetails(fusionAttributeType: string, fusionAttributeId: number): Promise<FusionAttributeValueDetails> {
        return this.http.get(`internal/fusion/details/${fusionAttributeType}/${fusionAttributeId}`)
            .toPromise()
            .then(response => <FusionAttributeValueDetails>response.json())
            .catch(err => this.handleError(err));
    }

    getFusionAttributeProfile(type: string, id: number): Promise<any> {
        return this.http.get(`internal/fusion/profile/${type}/${id}`)
            .toPromise()
            .then(response => <any>response.json())
            .catch(err => this.handleError(err));
    }

    saveAttribute(attribute: any): Promise<JsonResult> {
        if (attribute.ID == undefined || !attribute.ID) {
            return this.postDynamic(this.http, 'fusionattribute', attribute);
        }
        return this.putDynamic(this.http, 'fusionattribute', attribute);
    }
}