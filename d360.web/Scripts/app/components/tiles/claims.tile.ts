///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { DataTable, Column } from 'primeng/primeng';
import { ClaimItem } from '../../models/claims.model';
import { ClaimsMatrixPart } from '../parts/claims-matrix.part';


@Component({
    selector: 'd3s-claims-tile',
    directives: [DataTable, Column, ClaimsMatrixPart],
    templateUrl: 'scripts/app/components/tiles/claims.tile.html',
    viewProviders: [HTTP_PROVIDERS],
    styles: [`
    `]
})

export class ClaimsTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: string;
    @Input() title: string = "Permissions";
    @Input() readonly: boolean = true;

    private claimItems = new Array<ClaimItem>();
    private isLoading = false;

    http: Http;

    constructor(http: Http) {
        this.http = http;
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
        this.http.get(`/api/ownership/${this.objectType}/${this.objectID}/responsibilitytypes`)
            .map(data => data.json())
            .subscribe(data => {
                this.claimItems = data;
                //console.log(this.claimItems);

                this.isLoading = false;
            });

    }

    //selectRow(id: string): void {
    //    this.selectedRow = this.fieldDefinitions[this.fieldDefinitions.findIndex(d => d.ID == id)];
    //}
}
