import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { JsonResult } from "../models/jsonresult.model";
import {
    Classification,
    AssetDetail,
    NymType,
    Synonym,
    SynonymItem,
    ObjectDetail
} from "../models/object-detail.model";

import { BaseObservableService } from "./baseObservable.service";
import { MessagesObservableService } from "./messages-observable.service";

@Injectable({
    providedIn: 'root'
})
export class ObjectDetailService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesObservableService
    ) {
        super(messagesService);
    }

    getObjectDetailByUid(uid: string, objectType: string, useSingleColumn: boolean = false, includeHeader: boolean = false, useAssetDetailColumnDefinition: boolean = false): Observable<any> {
        return this.http.get(`api/${objectType}/${uid}/detail?useSingleColumn=${useSingleColumn}&includeHeader=${includeHeader}&useAssetDetailColumnDefinition=${useAssetDetailColumnDefinition}`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    getObjectDetail(
        objectID: number,
        objectType: string,
        useSingleColumn: boolean = false,
        includeHeader: boolean = false,
        useAssetDetailColumnDefinition: boolean = false
    ): Observable<any> {
        return this.http.get(`api/${objectType}/${objectID}/detail?useSingleColumn=${useSingleColumn}&includeHeader=${includeHeader}&useAssetDetailColumnDefinition=${useAssetDetailColumnDefinition}`)
            .pipe(
                map(response => <any>response),
                catchError(err => this.handleError(err))
            );
    }

    getObject(
        objectID: number,
        objectType: string
    ): Observable<ObjectDetail> {
        return this.http.get(`api/${objectType}/${objectID}`)
            .pipe(
                map(response => <ObjectDetail>response),
                catchError(err => this.handleError(err))
            );
    }

    getAsset(assetID: number): Observable<AssetDetail> {
        return this.http.get(`api/asset/${assetID}`)
            .pipe(
                map(response => <AssetDetail>response),
                catchError(err => this.handleError(err))
            );
    }

    getObjectSynonyms(
        objectID: number,
        objectType: string,
        predicateId: number
    ): Observable<Synonym[]> {
        return this.http.get(`api/${objectType}/${objectID}/${predicateId}/synonyms`)
            .pipe(
                map(response => <Synonym[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getSynonymTypes(
        objectID: number,
        objectType: string,
        predicateId: number
    ): Observable<any> {
        return this.http.get(`form/SynonymTypes?id=${objectID}&type=${objectType}&predicateId=${predicateId}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getSynonymOptions(
        predicateId: number,
        type: string,
        typeId: number,
        object: string,
        objectId: number,
        query: string = ''
    ): Observable<SynonymItem[]> {
        return this.http.get(`form/SynonymsOptions?type=${type}&typeId=${typeId}&obj=${object}&objid=${objectId}&query=${query}&predicateId=${predicateId}`)
            .pipe(
                map(response => <SynonymItem[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteCustomSynonym(synonym: Synonym): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, 'customsynonym', synonym.CustomID);
    }

    postCustomSynonym(
        synonymName: string,
        predicateID: number,
        object: string,
        objectId: number
    ): Observable<JsonResult> {
        var synonym: any = new Object();

        synonym.Name = synonymName;
        synonym.PredicateID = predicateID;
        synonym.Object = object;
        synonym.ObjectID = objectId;

        return this.postDynamic(this.http, 'customsynonym', synonym);
    }
    
    getNymAllocations(
        objectID: number,
        object: string
    ): Observable<NymType[]> {
        return this.http.get(`api/${object}/${objectID}/NymAllocations`)
            .pipe(
                map(response => <NymType[]>response),
                catchError(err => this.handleError(err))
            );
    }

    saveNymAllocations(
        objectID: number,
        object: string,
        nyms: NymType[]
    ): Observable<JsonResult> {
        let model = {
            Object: object,
            ObjectID: objectID,
            PredicateIDs: nyms.filter(x => x.Enabled).map((a) => a.ID)
        };

        return this.http.post('form/AddNymAllocation', model)
            .pipe(
                map(response => <JsonResult>response),
                catchError(err => this.handleError(err))
            );
    }


    getClassifications(objectType: string): Observable<Classification[]> {
        return this.http.get(`api/${objectType}?$orderby=Name`)
            .pipe(
                map(response => <Classification[]>response),
                catchError(err => this.handleError(err))
            );
    }

    deleteClassification(
        id: number,
        objectType: string
    ): Observable<JsonResult> {
        return this.deleteDynamicWithResult(this.http, objectType, id);
    }

    saveClassification(
        classification: Classification,
        objectType: string
    ): Observable<JsonResult> {
        if (classification.ID == undefined || !classification.ID) {
            return this.postDynamic(this.http, objectType, classification);
        }

        return this.putDynamic(this.http, objectType, classification);
    }
}
