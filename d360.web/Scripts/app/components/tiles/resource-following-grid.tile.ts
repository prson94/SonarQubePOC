
import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { Column, Header } from 'primeng/primeng';
import { FollowingDetailForResource } from '../../models/resource.model';
import { ResourcesService } from '../../services/index';
import { FormHelper } from '../../models/form.model';
import { Router } from '@angular/router';

@Component({
    selector: 'd3s-resource-following-grid-tile',
    template: `
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;" [hidden]="!simpleFilter">   
   <p-dataTable [globalFilter]="gb" [value]="items" [rows]="10" [paginator]="true" selectionMode="single" (onRowDblclick)="navigate($event)">
        <p-column header="Name" field="Name" [filter]="!simpleFilter">
            <template let-row="rowData" pTemplate type="body">
                <d3s-tooltip [objectType]="row.ObjectType" [objectId]="row.ObjectID" tooltipType="preview">{{row.Name}}</d3s-tooltip>
            </template>
        </p-column>
        <p-column header="Current Score">
            <template let-row="rowData" pTemplate type="body">
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

    @Input() simpleFilter: boolean = false;

    isLoading = false;
    private items: FollowingDetailForResource[] = new Array<FollowingDetailForResource>();

    constructor(private resourcesService: ResourcesService, private router: Router) {
        
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
                FormHelper.convertToNgUrl(this.items, 'Url');
                //console.log(r);
                this.isLoading = false;
            });
    }

    navigate(e: any) {
        let url = e.data.Url;
        this.router.navigateByUrl(url);

    }
}