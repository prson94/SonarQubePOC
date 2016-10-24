import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { Template } from '../models/template.model';
import {BaseService} from './base.service';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';


import 'rxjs/add/operator/toPromise';

@Injectable()
export class TemplatesService extends BaseService {
    private templatesUrl = 'api/templates/tooltip';

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService);  }

    getTemplates(): Promise<Template[]> {
        return this.http.get(this.templatesUrl)
            .toPromise()
            .then(response => <Template[]>response.json())
            .catch(err => this.handleError(err) );
    }

    getTemplate(id: number) {
        return this.getTemplates()
            .then(templates => templates.filter(template => template.ID === id)[0]);
    }

    deleteTemplateById(id: Number): Promise<JsonResult> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');
        
        let url = `form/templates/tooltip/${id}`;

        return this.http
            .delete(url, headers)
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err) );
    }

    putTemplate(template: Template): Promise<JsonResult> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/EditTooltipTemplateRaw`;

        return this.http
            .put(url, JSON.stringify(template), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err) );
    }


    postTemplate(template: Template): Promise<JsonResult>  {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/AddTooltipTemplateRaw`;
                
        return this.http
            .post(url, JSON.stringify(template), { headers: headers })
            .toPromise()
            .then(res => <JsonResult>res.json())
            .catch(err => this.handleError(err) );
    }    
}