import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";

import {LoadFilePostModel} from '../../models/load.model';
import {JsonResult} from '../../models/jsonresult.model';
import {MessagesService} from '../messages.service';
import {BaseObservableService} from "../baseObservable.service";

@Injectable()
export class PostLoadService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    postLoad(model: LoadFilePostModel): Observable<JsonResult> {
        return this.http.post('form/AddLoad', model).pipe(
            map(response => <JsonResult>response),
            catchError(err => this.handleError(err))
        );
    }
}
