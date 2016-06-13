///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, OnInit } from '@angular/core';
import { Http } from '@angular/http';
import { FormMessage, MessageType } from '../../models/form.model';
import { ClaimsMatrixDisplayModel, Claim, ClaimObject, ClaimsMatrixEditorItemModel } from '../../models/claims.model';


@Component({
    selector: 'd3s-claims-matrix',
    template: `
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
    `
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

    constructor(http: Http) {
        this.http = http;
    }

    ngOnInit() {
        this.load();
    }

    load() {

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

                console.log(this.items);

                for (var i = 0; i < this.claimsModel.Items.length; i++) {
                    var item = this.claimsModel.Items[i];
                    var c = new ClaimEditorItem();
                    c.ID = item.ID;
                    c.ClaimObject = item.ClaimObject;
                    c.Claim = item.Claim;
                    c.checked = true;

                    this.items[item.ClaimObject - 1][item.Claim - 1] = c;
                }


                console.log(data);
                console.log(this.claimsModel);
            });
    }

}

class ClaimEditorItem extends ClaimsMatrixEditorItemModel {
    checked = false;
}
