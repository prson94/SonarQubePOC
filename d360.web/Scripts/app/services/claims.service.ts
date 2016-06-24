///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { ClaimItem, ClaimsMatrixDisplayModel, ClaimsMatrixEditorItemModel, IClaimsService } from '../models/claims.model';
import { BaseService } from './base.service';
import { MessagesService } from './index';

@Injectable()
export class ClaimsService extends BaseService implements IClaimsService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService);}

    getClaims(objectID: number, objectType: string): Promise<ClaimItem[]> {
        return this.http.get(`/api/ownership/${objectType}/${objectID}/responsibilitytypes`)
            .toPromise()
            .then(response => <ClaimItem[]>response.json())
            .catch(err=>this.handleError(err));
    }

    getClaimsDisplayModel(objectID: number, objectType: string, responsibilityTypeID: number): Promise<ClaimsMatrixDisplayModel> {
        return this.http.get(`parts/ClaimsMatrix?type=${objectType}&id=${objectID}&responsibilityTypeID=${responsibilityTypeID}`)
            .toPromise()
            .then(response => <ClaimsMatrixDisplayModel>response.json())
            .catch(err=>this.handleError(err));
    }

    putClaims(objectID: number, objectType: string, responsibilityTypeID: number, claims: ClaimItem[]): Promise<any> {
        let headers = new Headers();
        headers.append('Content-Type', 'application/json');

        var model = {
            claims: claims,
            objectType: objectType,
            objectID: objectID,
            responsibilityTypeID: responsibilityTypeID
        };

        return this.http.put('form/EditClaimsMatrix', JSON.stringify(model), { headers: headers })
            .toPromise()
            .catch(err=>this.handleError(err));
    }
}