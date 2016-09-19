import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './index';
import { FusionAttributePagedResults, FusionAttributeValueDetails } from '../models/fusion-attribute.model';
import { SortOrder } from '../models/enums.model';

@Injectable()
export class FusionAttributeService extends BaseService {
    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }


    getFusionAttributes(fusionId: number, fusionAttributeTypeId: number, pageNumber?: number, pageSize?: number, sortField?: string, sortOrder?: SortOrder): Promise<FusionAttributePagedResults> {
        let sortOrderText = '';

        if (sortOrder == SortOrder.Ascending) sortOrderText = 'asc';
        if (sortOrder == SortOrder.Descending) sortOrderText = 'desc';

        return this.http.get(`fusion/ItemsByAttributeType?fusionID=${fusionId}&fusionAttributeTypeID=${fusionAttributeTypeId}&pagenum=${pageNumber ? pageNumber : 0}&pagesize=${pageSize ? pageSize : 20}&sortDataField=${sortField ? sortField : ''}&sortOrder=${sortOrderText}`)
            .toPromise()
            .then(response => <FusionAttributePagedResults>response.json())
            .catch(err => this.handleError(err));
    }


    getFusionAttributeExcel(fusionId: number, fusionAttributeTypeId: number) {
        window.location.assign(`fusion/ExportItemsByAttributeType?fusionID=${fusionId}&fusionAttributeTypeID=${fusionAttributeTypeId}&filterscount=0`);
    }

    getFusionAttributeDetails(fusionAttributeId: number): Promise<FusionAttributeValueDetails> {
        return this.http.get(`fusion/details/FusionAttribute/${fusionAttributeId}`)
            .toPromise()
            .then(response => <FusionAttributeValueDetails>response.json())
            .catch(err => this.handleError(err));
    }
}