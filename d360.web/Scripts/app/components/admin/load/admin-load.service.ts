import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {LoadDetail} from '../../../models/load.model';

import {MessagesService} from '../../../services/messages.service';
import {BaseObservableService} from "../../../services/baseObservable.service";

@Injectable()
export class AdminLoadService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getLoads(): Observable<LoadDetail[]> {
        return this.http.get('api/loads').pipe(
            map(response => <LoadDetail[]>response),
            catchError(err => this.handleError(err))
        );
    }
}
