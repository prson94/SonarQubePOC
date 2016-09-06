///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { Column, Header } from 'primeng/primeng';
import { FollowingDetailForResource } from '../../models/resource.model';
import { ResourcesService } from '../../services/index';

@Component({
    selector: 'd3s-resource-following-grid-tile',
    template: `
<div *ngIf="isLoading" style="width:100%; text-align:center;">
    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
</div>
<div *ngIf="!isLoading">
   <p-dataTable [value]="items" [rows]="10" [paginator]="true" selectionMode="single">
        <p-column field="Name" header="Name"></p-column>
        <p-column header="Current Score">
            <template let-row="rowData">
                <div>{{row.CurrentScore | scoreDisplay }}</div>
            </template>
        </p-column>
    </p-dataTable>
</div>
`,
})
export class ResourceFollowingGridTile implements OnInit, OnChanges {
    @Input() resourceId: number;
    @Input() objectId: number;
    @Input() objectType: string;
    isLoading = false;
    private items: FollowingDetailForResource[] = new Array<FollowingDetailForResource>();

    constructor(private resourcesService: ResourcesService) {

    }

    ngOnInit() { }

    ngOnChanges() {
        this.load();
    }


    load() {
        this.isLoading = true;
        this.resourcesService.getFollowingByResourceByType(this.resourceId, this.objectType, this.objectId)
            .then(r => {
                this.items = r;
                console.log(r);
                this.isLoading = false;
            });
    }
}