import { Injectable } from '@angular/core';
import { Headers, Http, ResponseContentType, Response } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { QualifierType, ResolutionObjectType } from '../models/qualifier.model';

@Injectable()
export class QualifierService extends BaseService {
    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }


    getQualifierTypes(ruleID: number): Promise<QualifierType[]> {
        return this.http.get(`api/rules/${ruleID}/qualifiers`)
            .toPromise()
            .then(response => <QualifierType[]>response.json())
            .catch(err => this.handleError(err));
    }

    putMoveQualifierType(id: number, moveUp: boolean = false): Promise<any> {
        return this.http.put(`form/MoveRuleQualifierType`, { id, moveUp })
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getQualifierResolutionObjects(): Promise<ResolutionObjectType[]> {
        return this.http.get(`api/qualifier/resolutiontypes`)
            .toPromise()
            .then(response => <ResolutionObjectType[]>response.json())
            .catch(err => this.handleError(err));
    }

    getQualifierResolutionFields(id: number, type: string): Promise<any> {
        return this.http.get(`fields/${type}/${id}.json`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    postAddRuleQualifierType(model: QualifierType): Promise<any> {
        return this.http.post('form/AddRuleQualifierType', model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    putEditRuleQualifierType(model: QualifierType): Promise<any> {
        return this.http.put('form/EditRuleQualifierType', model)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

}