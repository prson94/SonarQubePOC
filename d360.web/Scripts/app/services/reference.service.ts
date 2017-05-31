import { Injectable } from '@angular/core';
import { Headers, Http, Response, ResponseContentType } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { ReferenceItemType, ReferenceItem } from '../models/reference.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class ReferenceService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getReferenceItemTypes(): Promise<ReferenceItemType[]> {
        return this.http.get(`api/referenceItemTypes`)
            .toPromise()
            .then(response => <ReferenceItemType[]>response.json())
            .catch(err => this.handleError(err));
    }

    saveReferenceItemType(item: ReferenceItemType) {
        if (item.ID == undefined || !item.ID) {
            return this.postDynamic(this.http, 'referenceItemType', item);
        }
        return this.putDynamic(this.http, 'referenceItemType', item);
    }

    deleteReferenceItemType(id: number): Promise<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'referenceItemType', id);
    }

    exportReferenceItems(id: number, name: string) {
        this.http.get(`api/referenceItems/${id}/items.xls`, { responseType: ResponseContentType.Blob }).subscribe(data => this.downloadFile(data, name));  
    }

    downloadFile(data: Response, name: string) {
        var filename = `${name} List ${new Date().toDateString()}.xlsx`;
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
}