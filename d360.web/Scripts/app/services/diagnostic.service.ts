import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { DiagnosticInvalidTextPath } from '../models/diagnostic.model';

@Injectable()
export class DiagnosticService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getObjectsWithInvalidTextpath(): Promise<DiagnosticInvalidTextPath[]> {
        return this.http.get('api/diagnostic/invalidtextpaths')
            .toPromise()
            .then(response => <DiagnosticInvalidTextPath[]>response.json())
            .catch(err => this.handleError(err));
    }
}