import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { ExportTemplate, ExportTemplateStyle } from '../models/export-template.model';
import { Subject } from 'rxjs/Subject';
import { Observable } from "rxjs/Observable";
import { JsonResult } from '../models/jsonresult.model';

@Injectable()
export class ExportTemplateService extends BaseService {

    constructor(private http: HttpClient, messagesService: MessagesService) { super(messagesService); }

    public getExportTemplates(): Observable<ExportTemplate[]> {
        return this.http.get('/api/v2/exporttemplates/')            
            .map(item => { return <ExportTemplate[]>item })
            .catch(err => this.handleError(err));
    }

    public deleteExportTemplates(id: number): Observable<any> {
       return this.http.delete(`/api/v2/exporttemplates/${id}`);            
    }

    public saveExportTemplate(exportTemplate: ExportTemplate): Observable<ExportTemplate> {
        if (exportTemplate.ID > 0) {
            return this.http.put(`/api/v2/exporttemplates/${exportTemplate.ID}`, exportTemplate)
                .map(item => { return <ExportTemplate>item })
                .catch(err => this.handleError(err));
        }
        
        return this.http
                .post<ExportTemplate>(`/api/v2/exporttemplates`, exportTemplate)
                .map(item => { return <ExportTemplate>item })
                .catch(err => this.handleError(err));
        
    }

    public saveTemplateFile(exportTemplate: ExportTemplate): Observable<any> {        
        return this.http.post(`/api/v2/exporttemplates/templatefile/${exportTemplate.ID}`,'');
    }

    public getExportTemplateStyles(templateId:number): Observable<ExportTemplateStyle[]> {
        return this.http.get(`/api/v2/exporttemplates/Styles/${templateId}`)
            .map(item => { return <ExportTemplateStyle[]>item })
            .catch(err => this.handleError(err));
    }

    public saveExportTemplateStyle(templateStyle: ExportTemplateStyle): Observable<ExportTemplateStyle> {
        if (templateStyle.ID > 0) {
            return this.http.put(`/api/v2/exporttemplates/Style/${templateStyle.ID}`, templateStyle)
                .map(item => { return <ExportTemplateStyle>item })
                .catch(err => this.handleError(err));
        }

        return this.http
            .post<ExportTemplateStyle>(`/api/v2/exporttemplates/Style`, templateStyle)
            .map(item => { return <ExportTemplateStyle>item })
            .catch(err => this.handleError(err));

    }

    public deleteExportTemplateStyle(id: number): Observable<any> {
        return this.http.delete(`/api/v2/exporttemplates/Style/${id}`);
    }
}