///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { DataTable, Column } from 'primeng/primeng';
import { ClaimItem } from '../../models/claims.model';
import { ClaimsMatrixPart } from '../parts/claims-matrix.part';
import { ClaimsService } from '../../services/claims.service';


@Component({
    selector: 'd3s-claims-tile',
    directives: [DataTable, Column, ClaimsMatrixPart],
    templateUrl: 'scripts/app/components/tiles/claims.tile.html',
    providers: [ClaimsService]
})

export class ClaimsTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() title: string = "Permissions";
    @Input() readonly: boolean = true;

    private claimItems = new Array<ClaimItem>();
    private isLoading = false;

    constructor(private claimsService: ClaimsService) {
        this.claimsService = claimsService;
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'objectType') {
                this.objectType = changes['objectType'].currentValue;
            }
            if (p == 'objectID') {
                this.objectID = changes['objectID'].currentValue;
            }
        }

        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;

        this.claimsService.getClaims(this.objectID, this.objectType)
            .then(data => {
                this.claimItems = data;
                this.isLoading = false;
            });
    }
}
