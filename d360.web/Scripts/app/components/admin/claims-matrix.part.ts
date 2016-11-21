import { Input, Component, OnInit } from '@angular/core';
import { FormMessage, MessageType } from '../../models/form.model';
import { ClaimsMatrixDisplayModel, Claim, ClaimObject, ClaimsMatrixEditorItemModel } from '../../models/claims.model';
import { ClaimsService } from '../../services/claims.service';

@Component({
    selector: 'd3s-claims-matrix',
    providers: [ClaimsService],
    template: `
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading">
                        <table class="striped">
                            <thead>
                                <tr>
                                    <th class="permission-header"></th>
                                    <th style="width: 15%;" class="permission-header" *ngFor="let o of claimObject">{{o.text}}</th>
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
})

export class ClaimsMatrixPart implements OnInit {
    @Input() readonly: boolean = true;
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() responsibilityTypeID: number;

    claimsModel: ClaimsMatrixDisplayModel;

    claim = [];
    claimObject = [];
    items: ClaimEditorItem[][];

    isLoading = false;
    isSaving = false;

    constructor(private claimsService: ClaimsService) {
        this.claimsService = claimsService;
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;

        this.claimsService.getClaimsDisplayModel(this.objectID, this.objectType, this.responsibilityTypeID)
            .then(data => {
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

                for (var i = 0; i < this.claimsModel.Items.length; i++) {
                    var item = this.claimsModel.Items[i];
                    var c = new ClaimEditorItem();
                    c.ID = item.ID;
                    c.ClaimObject = item.ClaimObject;
                    c.Claim = item.Claim;
                    c.checked = true;

                    this.items[item.ClaimObject - 1][item.Claim - 1] = c;
                }

                this.isLoading = false;
            });                
    }

    save() {
        this.isSaving = true;
        var flatItems = [];

        for (var i = 0; i < this.claimObject.length; i++) {
            for (var j = 0; j < this.claim.length; j++) {
                var item = this.items[i][j];
                item.ClaimObject = this.claimObject[i].val;
                item.Claim = this.claim[j].val;
                flatItems.push(this.items[i][j]);
            }
        }

        var claims = flatItems.filter(i => i.checked);

        this.claimsService.putClaims(this.objectID, this.objectType, this.responsibilityTypeID, claims)
            .then(data => {
                this.isSaving = false;
            });
    }
}

class ClaimEditorItem extends ClaimsMatrixEditorItemModel {
    checked = false;
}
