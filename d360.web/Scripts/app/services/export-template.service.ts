import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {catchError, map} from 'rxjs/operators';

import { ExportTemplate, ExportTemplateStyle } from '../models/export-template.model';

import { MessagesObservableService } from './messages-observable.service';
import {BaseObservableService} from "./baseObservable.service";

@Injectable()
export class ExportTemplateService extends BaseObservableService {

    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) { super(messagesService); }

    public getExportTemplates(): Observable<ExportTemplate[]> {
        return this.http.get('/api/v2/exporttemplates/').pipe(            
            map(item => { return <ExportTemplate[]>item }),
            catchError(err => this.handleError(err)),);
    }

    public getExportTemplatesForAssetType(assetTypeUID: string): Observable<ExportTemplate[]> {
        return this.http.get(`/api/v2/exporttemplates/${assetTypeUID}`).pipe(            
            map(item => { return <ExportTemplate[]>item }),
            catchError(err => this.handleError(err)));
    }

    public deleteExportTemplates(id: number): Observable<any> {
       return this.http.delete(`/api/v2/exporttemplates/${id}`);            
    }

    public saveExportTemplate(exportTemplate: ExportTemplate): Observable<ExportTemplate> {
        if (exportTemplate.ID > 0) {
            return this.http.put(`/api/v2/exporttemplates/${exportTemplate.ID}`, exportTemplate).pipe(
                map(item => { return <ExportTemplate>item }),
                catchError(err => this.handleError(err)),);
        }
        
        return this.http
                .post<ExportTemplate>(`/api/v2/exporttemplates`, exportTemplate).pipe(
                map(item => { return <ExportTemplate>item }),
                catchError(err => this.handleError(err)),);
        
    }

    public saveTemplateFile(exportTemplate: ExportTemplate): Observable<any> {        
        return this.http.post(`/api/v2/exporttemplates/templatefile/${exportTemplate.ID}`,'');
    }

    public getExportTemplateStyles(templateId:number): Observable<ExportTemplateStyle[]> {
        return this.http.get(`/api/v2/exporttemplates/Styles/${templateId}`).pipe(
            map(item => { return <ExportTemplateStyle[]>item }),
            catchError(err => this.handleError(err)),);
    }

    public saveExportTemplateStyle(templateStyle: ExportTemplateStyle): Observable<ExportTemplateStyle> {
        if (templateStyle.ID > 0) {
            return this.http.put(`/api/v2/exporttemplates/Style/${templateStyle.ID}`, templateStyle).pipe(
                map(item => { return <ExportTemplateStyle>item }),
                catchError(err => this.handleError(err)),);
        }

        return this.http
            .post<ExportTemplateStyle>(`/api/v2/exporttemplates/Style`, templateStyle).pipe(
            map(item => { return <ExportTemplateStyle>item }),
            catchError(err => this.handleError(err)),);

    }

    public deleteExportTemplateStyle(id: number): Observable<any> {
        return this.http.delete(`/api/v2/exporttemplates/Style/${id}`);
    }
}
