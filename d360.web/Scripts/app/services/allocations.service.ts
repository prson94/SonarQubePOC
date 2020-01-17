import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JsonResult } from '../models/jsonresult.model';
import { MetricAssetViewModel, MetricFieldTypeViewModel, ScoreTypeAllocation } from '../models/metrics.model';
import { AssetTypeMetricModel } from '../models/asset.model';
import { Observable } from 'rxjs';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { map, catchError } from 'rxjs/operators';



@Injectable()
export class AllocationService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    public getAllocations(): Observable<ScoreTypeAllocation[]> {
        let url = `/api/v2/scoring/allocations?_state=Active`;
        return this.http.get(url)
            .pipe(map(response => <ScoreTypeAllocation[]>response),
                catchError(err => this.handleError(err, true)));
    }

    public export(filters: any) {
        var queryString = '?' + Object.keys(filters).map(key => key + '=' + filters[key].value).join('&');

        this.http.get('api/v2/relationships/export/types', { responseType: 'blob' }).subscribe(data => this.downloadFile(data, 'Allocations'));

    }

    downloadFile(data: Blob, name: string) {
        var filename = `${name} ${new Date().toDateString()}.xlsx`;
        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        }
        else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");
            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }
}