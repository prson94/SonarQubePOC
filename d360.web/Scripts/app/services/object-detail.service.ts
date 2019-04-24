import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {catchError, map} from "rxjs/operators";
import {TreeNode} from 'primeng/components/common/api';

import {FormHelper} from '../models/form.model';
import {JsonResult} from '../models/jsonresult.model';
import {
    Classification,
    AssetDetail,
    NymType,
    Synonym,
    SynonymEditorModel,
    SynonymEditModel,
    AttributeHeirarchyItem,
    ToolbarItemNg,
    ObjectDetail
} from '../models/object-detail.model';
import {LookupGrid} from '../models/grid-definition.model';

import {BaseObservableService} from "./baseObservable.service";
import {MessagesService} from './messages.service';

@Injectable()
export class ObjectDetailService extends BaseObservableService {
    constructor(
        private http: HttpClient,
        messagesService: MessagesService
    ) {
        super(messagesService);
    }

    getObjectDetail(
        objectID: number,
        objectType: string
    ): Observable<any> {
        return this.http.get(`api/${objectType}/${objectID}/detail`)
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
    ): Observable<SynonymEditorModel> {
        return this.http.get(`form/SynonymsOptions?type=${type}&typeId=${typeId}&obj=${object}&objid=${objectId}&query=${query}&predicateId=${predicateId}`)
            .pipe(
                map(response => <SynonymEditorModel>response),
                map(r => {
                    r.items.forEach(
                        i => {
                            i.ID = i[0].Value;
                            i.Name = i[1].Value;
                            i.TargetingSubject = i[2].Value;
                        }
                    );

                    return r;
                }),
                catchError(err => this.handleError(err))
            );
    }

    postSynonym(model: SynonymEditModel): Observable<any> {
        return this.http.post('form/AddSynonym', model)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getAttributeHierarchyItems(
        objectID: number,
        objectType: string
    ): Observable<AttributeHeirarchyItem[]> {
        return this.http.get(`attributes/hierarchy/${objectType}/${objectID}`)
            .pipe(
                map(response => <AttributeHeirarchyItem[]>response),
                catchError(err => this.handleError(err))
            );
    }

    // @ts-ignore
    getAttributeHierarchyTree(
        objectID: number,
        objectType: string
    ): Observable<TreeNode[]> {
        return this.getAttributeHierarchyItems(objectID, objectType).pipe(
            map(
                (result) => {
                    let data = FormHelper.flattenTree(result, 'Items', 'ID', 'ParentUID');

                    return <TreeNode[]>FormHelper.formTree(data, 'ID', 'ParentUID');
                }
            )
        );
    }

    getAttributeActions(
        objectID: number,
        objectType: string,
        ownerID: number,
        ownerType: string,
        attributeID: number = null
    ): Observable<ToolbarItemNg[]> {
        let url = `attributes/actions/${objectType}/${objectID}/${ownerType}/${ownerID}/`;

        if (attributeID != null) {
            url += `${attributeID}`;
        }

        return this.http.get(url)
            .pipe(
                map(response => <ToolbarItemNg[]>response),
                catchError(err => this.handleError(err))
            );
    }

    getLookupGrid(uri: string): Observable<LookupGrid> {
        return this.http.get(uri)
            .pipe(
                map(result => <LookupGrid>result),
                catchError(err => this.handleError(err))
            );
    }

    getLookupGridExport(
        type: string,
        id: number,
        fieldTypeID: number,
        lookupType: number
    ) {
        let uri = `api/dynamiclookup/export/${type}/${id}/${fieldTypeID}/${lookupType}/excel.xls`;

        this.http.get(uri, {responseType: 'blob'}).subscribe(
            d => this.downloadFile(d)
        );
    }

    downloadFile(data: Blob) {
        var filename = `Item List ${new Date().toDateString()}.xlsx`;

        if (window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(data, filename);
        } else {
            var url = window.URL.createObjectURL(data);
            var anchor = document.createElement("a");

            anchor.setAttribute("style", "display:none;");
            document.body.appendChild(anchor);
            anchor.setAttribute("download", filename);
            anchor.href = url;
            anchor.click();
        }
    }

    deleteSynonym(synonym: Synonym): Observable<JsonResult> {
        if (synonym.IntersectID) {
            return this.deleteDynamicWithResult(this.http, 'synonym', synonym.IntersectID);
        }

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
