import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";
import { QualifierType, ResolutionObjectType } from '../models/qualifier.model';

@Injectable()
export class QualifierService extends BaseObservableService {
    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }


    getQualifierTypes(implementationId: number): Observable<QualifierType[]> {
        return this
            .http
            .get(`api/ruleimplementations/${implementationId}/qualifiers`)            
            .pipe(
                map(response => <QualifierType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    putMoveQualifierType(id: number, moveUp: boolean = false): Observable<any> {
        return this
            .http
            .put(`form/MoveRuleQualifierType`, { id, moveUp })
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    getQualifierResolutionObjects(): Observable<ResolutionObjectType[]> {
        return this
            .http
            .get(`api/qualifier/resolutiontypes`)
            .pipe(
                map(response => <ResolutionObjectType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getQualifierResolutionFields(id: number, type: string): Observable<any> {
        return this
            .http
            .get(`fields/${type}/${id}.json`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    postAddRuleQualifierType(model: QualifierType): Observable<any> {
        return this
            .http
            .post('form/AddRuleQualifierType', model)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    putEditRuleQualifierType(model: QualifierType): Observable<any> {
        return this
            .http
            .put('form/EditRuleQualifierType', model)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }
}