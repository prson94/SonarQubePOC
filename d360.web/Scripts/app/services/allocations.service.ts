import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JsonResult } from '../models/jsonresult.model';
import { MetricAssetViewModel, MetricFieldTypeViewModel, Allocation } from '../models/metrics.model';
import { AssetTypeMetricModel } from '../models/asset.model';
import { Observable } from 'rxjs';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { map, catchError } from 'rxjs/operators';



@Injectable()
export class AllocationService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    public getAllocations(): Observable<Allocation[]> {
        let url = `/api/v2/scoring/allocations?_state=Active`;
        return this.http.get(url)
            .pipe(map(response => <Allocation[]>response),
                catchError(err => this.handleError(err, true)));
    }

}