///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { ClaimItem, ClaimsMatrixDisplayModel, ClaimsMatrixEditorItemModel, IClaimsService } from '../models/claims.model';


@Injectable()
export class ClaimsService implements IClaimsService {

    constructor(private http: Http) { }

    getClaims(objectID: number, objectType: string): Promise<ClaimItem[]> {
        return this.http.get(`/api/ownership/${objectType}/${objectID}/responsibilitytypes`)
            .toPromise()
            .then(response => <ClaimItem[]>response.json())
            .catch(this.handleError);
    }

    getClaimsDisplayModel(objectID: number, objectType: string, responsibilityTypeID: number): Promise<ClaimsMatrixDisplayModel> {
        return this.http.get(`parts/ClaimsMatrix?type=${objectType}&id=${objectID}&responsibilityTypeID=${responsibilityTypeID}`)
            .toPromise()
            .then(response => <ClaimsMatrixDisplayModel>response.json())
            .catch(this.handleError);
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
            .catch(this.handleError);
    }

    private handleError(error: any) {
        console.error('An error occurred', error);
        return Promise.reject(error.message || error);
    }
}