///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './index';
import { BaseService } from './base.service';
import { Issue } from '../models/issue.model';
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class IssuesService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getIssues(objectID: number, objectType: string): Promise<Issue[]> {
        let url = 'services/workflow/tasks/types/3/';

        if (objectID > 0 && objectType != undefined) {
            url += `${objectID}/${objectType}`;
        }

        return this.http.get(url)
            .toPromise()
            .then(response => <Issue[]>response.json())
            .catch(err => this.handleError(err));
    }

    updateIssue(issue: Issue, action: string, comment: string, assignTo?: string) : Promise<JsonResult> {
        let headers = new Headers({
                'Content-Type': 'application/json'
            });
        return this.http
            .post(`/services/workflow/tasks/${issue.WorkflowID}`, JSON.stringify({ WorkflowAction: action, AssignTo: assignTo, Comment: comment }), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(this.handleError);
    }
}