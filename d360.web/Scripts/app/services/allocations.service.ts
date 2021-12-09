import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ScoreTypeAllocation, ScoreType } from '../models/metrics.model';
import { Observable } from 'rxjs';
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from './baseObservable.service';
import { map, catchError } from 'rxjs/operators';



@Injectable({
    providedIn: 'root'
})
export class AllocationService extends BaseObservableService {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }

    public getAllocations(): Observable<ScoreTypeAllocation[]> {
        let url = `/api/v2/scoring/allocations?_state=Active&includeFlags=true`;
        return this.http.get(url)
            .pipe(map(response => <ScoreTypeAllocation[]>response),
                catchError(err => this.handleError(err, true)));
    }

    public getAllocationsByAssetTypeUid(assetTypeUid: string, state: string = "Active", orderBy = "", direction = ""): Observable<ScoreTypeAllocation[]> {
        let url = `/api/v2/scoring/allocations?_state=${state}&includeFlags=true&assetTypeUid=${assetTypeUid}`;

        if (orderBy.length > 0) {
            url += "&_order=" + orderBy;
        }

        if (direction.length > 0) {
            url += "&_direction=" + direction;
        }

        return this.http.get(url)
            .pipe(map(response => <ScoreTypeAllocation[]>response),
                catchError(err => this.handleError(err, true)));
    }

    public deleteAllocationByUid(uid: string): Observable<any> {
        let url = `api/v2/scoring/allocations/${uid}`;
        return this.http.delete(url)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));
    }

    public getunallocatedAssetTypes(scoreType: ScoreType): Observable<any[]> {
        let url = `api/v2/scoring/unallocatedAssetTypes/` + scoreType;
        return this.http.get(url)
            .pipe(map(response => <any>response),
                catchError(err => this.handleError(err, true)));

    }

    public save(allocation: ScoreTypeAllocation): Observable<any> {
        let url = `api/v2/scoring/allocations`;
        if (allocation.uid == undefined) {
            return this.http.post(url, allocation)
                .pipe(map(response => <any>response),
                    catchError(err => this.handleError(err, true)));
        }
        else {
            return this.http.put(url + "/" + allocation.uid, allocation)
                .pipe(map(response => <any>response),
                    catchError(err => this.handleError(err, true)));
        }

    }

    public export(filters: any) {
        var queryString = '?' + Object.keys(filters).map(key => key + '=' + filters[key].value).join('&');

        this.http.get('api/v2/scoring/export' + queryString, { responseType: 'blob' }).subscribe(data => this.downloadFile(data, 'Scores'));

    }
}