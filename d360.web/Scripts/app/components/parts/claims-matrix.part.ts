///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, OnInit } from '@angular/core';
import { Http, Headers } from '@angular/http';
import { FormMessage, MessageType } from '../../models/form.model';
import { ClaimsMatrixDisplayModel, Claim, ClaimObject, ClaimsMatrixEditorItemModel } from '../../models/claims.model';
import { Button } from 'primeng/primeng';

@Component({
    selector: 'd3s-claims-matrix',
    directives: [ Button ],
    template: `
<div *ngIf="isLoading">
    <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
</div>
<div *ngIf="!isLoading">
        <table class="striped">
            <thead>
                <tr>
                    <th></th>
                    <th style="width: 15%" *ngFor="let o of claimObject">{{o.text}}</th>
                </tr>
            </thead>
            <tbody>
                <tr *ngFor="let c of claim">
                    <td>{{c.text}}</td>
                    <td *ngFor="let o of claimObject"> 
                        <input type="checkbox" [disabled]="readonly" [(ngModel)]="items[o.val - 1][c.val - 1].checked" /> 
                    </td>
                </tr>
            </tbody>
        </table>
        <div *ngIf="!readonly" class="pull-right" style="padding:5px">
            <button pButton label="Save Changes" (click)="save()" [disabled]="isSaving || isLoading"></button><span *ngIf="isSaving"><i class="fa fa-spinner fa-spin"></i></span>
        </div>
</div>

    `,
    styles: [
    `
    th {
    border-top: 0 !important;
    border-left: 0 !important;
    border-right: 0 !important;
    border-bottom: 1px solid #C1C1C1 !important;    
}
`]
})

export class ClaimsMatrixPart implements OnInit {
    @Input() readonly: boolean = true;
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() responsibilityTypeID: number;

    http: Http;
    claimsModel: ClaimsMatrixDisplayModel;

    claim = [];
    claimObject = [];
    items: ClaimEditorItem[][];

    isLoading = false;
    isSaving = false;

    constructor(http: Http) {
        this.http = http;
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.http.get(`parts/ClaimsMatrix?type=${this.objectType}&id=${this.objectID}&responsibilityTypeID=${this.responsibilityTypeID}`)
            .map(data => data.json())
            .subscribe(data => {
                this.claimsModel = data;

                for (var o in Claim) {
                    if (typeof Claim[o] === 'number') this.claim.push({ val: Claim[o], text: o });
                }
                for (var o in ClaimObject) {
                    if (typeof ClaimObject[o] === 'number') this.claimObject.push({ val: ClaimObject[o], text: o });
                }

                this.items = [];

                for (var i = 0; i < this.claimObject.length; i++) {
                    this.items[i] = [];
                    for (var j = 0; j < this.claim.length; j++) {
                        this.items[i][j] = new ClaimEditorItem();
                        this.items[i][j].checked = false;
                    }
                }

                //console.log(this.items);

                for (var i = 0; i < this.claimsModel.Items.length; i++) {
                    var item = this.claimsModel.Items[i];
                    var c = new ClaimEditorItem();
                    c.ID = item.ID;
                    c.ClaimObject = item.ClaimObject;
                    c.Claim = item.Claim;
                    c.checked = true;

                    this.items[item.ClaimObject - 1][item.Claim - 1] = c;
                }


                //console.log(data);
                //console.log(this.claimsModel);
                this.isLoading = false;
            });
    }

    save() {
        this.isSaving = true;
        var flatItems = [];
        var headers = new Headers();
        headers.append('Content-Type', 'application/json');

        for (var i = 0; i < this.claimObject.length; i++) {
            for (var j = 0; j < this.claim.length; j++) {
                var item = this.items[i][j];
                item.ClaimObject = this.claimObject[i].val;
                item.Claim = this.claim[j].val;
                flatItems.push(this.items[i][j]);
            }
        }


        var model = {
            claims: flatItems.filter(i => i.checked),
            objectType: this.objectType,
            objectID: this.objectID,
            responsibilityTypeID: this.responsibilityTypeID
        }

        console.log(model);

        this.http.put('form/EditClaimsMatrix', JSON.stringify(model), { headers: headers })
            .map(data => data.json())
            .subscribe(data => {
                this.isSaving = false;
                console.log(data);
            });
    }
}

class ClaimEditorItem extends ClaimsMatrixEditorItemModel {
    checked = false;
}
