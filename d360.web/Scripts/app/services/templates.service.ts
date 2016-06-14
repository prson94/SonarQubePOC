///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';

import { Template } from '../models/template.model';

import 'rxjs/add/operator/toPromise';

@Injectable()
export class TemplatesService {
    private templatesUrl = 'api/templates/tooltip';
    
    constructor(private http: Http) { }

    getTemplates(): Promise<Template[]> {
        return this.http.get(this.templatesUrl)
            .toPromise()
            .then(response => <Template[]>response.json())
            .catch(this.handleError);
    }

    getTemplate(id: number) {
        return this.getTemplates()
            .then(templates => templates.filter(template => template.ID === id)[0]);
    }

    deleteTemplateById(id: Number) {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');
        
        let url = `form/templates/tooltip/${id}`;

        return this.http
            .delete(url, headers)
            .toPromise()
            .catch(this.handleError);
    }

    putTemplate(template: Template) {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        let url = `form/EditTooltipTemplateRaw`;

        return this.http
            .put(url, JSON.stringify(template), { headers: headers })
            .toPromise()
            .catch(this.handleError);
    }


    postTemplate(template: Template) {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');
                
        return this.http
            .post(this.templatesUrl, JSON.stringify(template), { headers: headers })
            .toPromise()
            .catch(this.handleError);
    }

    private handleError(error: any) {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error);
    }
}