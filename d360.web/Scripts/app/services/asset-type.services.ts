import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { TreeNode } from 'primeng/primeng';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';
import { JsonResult } from '../models/jsonresult.model';
import { AssetTypeEditorModel, AssetTypeClass } from "../models/asset.model";

@Injectable()
export class AssetTypeService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }

    getAssetTypeEditor(cls: AssetTypeClass, id: number, parentID: number): Promise<AssetTypeEditorModel> {
        return this.http.get(`form/AssetType?class=${cls}&parentID=${parentID}&id=${id}`)
            .toPromise()
            .then(response => <AssetTypeEditorModel>response.json())
            .catch(err=>this.handleError(err));
    }

    putAssetType(model: AssetTypeEditorModel): Promise<JsonResult> {
        return this.http.put('form/AssetType', model)
            .toPromise()
            .then(function(response) {
                let msg: JsonResult = response.json();
                if (msg.type == "error") {
                    this.messages.showError('Error', msg.message);
                }
                return <JsonResult>response.json();
            })
            .catch(err=>this.handleError(err));
    }

    postAssetType(model: AssetTypeEditorModel): Promise<JsonResult> {
        return this.http.post('form/AssetType', model)
            .toPromise()
            .then(function(response) {
                let msg: JsonResult = response.json();
                if (msg.type == "error") {
                    this.messages.showError('Error', msg.message);
                }
                return <JsonResult>response.json();
            })
            .catch(err=>this.handleError(err));
    }

    public deleteAssetType(id: number): Promise<JsonResult> {
        return this.http.delete(`form/AssetType?id=${id}`)
            .toPromise()
            .then(function(response) {
                let msg: JsonResult = response.json();
                if (msg.type == "error") {
                    this.messages.showError('Error', msg.message);
                }
                return <JsonResult>response.json();
            })
            .catch(err => this.handleError(err));
    }
}