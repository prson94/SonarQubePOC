///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Issue } from '../models/issue.model';

@Injectable()
export class IssuesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getIssues(objectID: number, objectType: string): Promise<Issue[]> {
        return this.http.get(`services/workflow/tasks/types/3/${objectID}/${objectType}`)
            .toPromise()
            .then(response => <Issue[]>response.json())
            .catch(err => this.handleError(err));
    }
}