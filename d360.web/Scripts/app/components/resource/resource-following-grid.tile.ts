import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { FollowingDetailForResource } from '../../models/resource.model';
import { ResourcesService } from '../../services/resources.service';
import { FormHelper } from '../../models/form.model';
import { Router } from '@angular/router';

@Component({
    selector: 'd3s-resource-following-grid-tile',
    template: `
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" i18n-placeholder placeholder="Search..." class="grid-simple-filter">
    <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Name','CurrentScore']" [paginator]="true" [rows]="10" >
        <ng-template pTemplate="header">
            <tr>
                <th [pSortableColumn]="'Name'"><ng-container i18n>Name</ng-container>
    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon></th>
    <th [pSortableColumn]="'CurrentScore'"><ng-container i18n>Governance Score</ng-container>
    <d3s-sortIcon [field]="'CurrentScore'"></d3s-sortIcon></th>
            </tr>
		    <tr [hidden]="showSimpleFilter">
                <th ><d3s-column-filter [field]="'Name'" [datatype]="'text'"></d3s-column-filter></th>
    <th ></th>
            </tr>
        </ng-template>
        <ng-template pTemplate="body" let-item>
            <tr (dblclick)="navigate(item)" [pSelectableRow]="item">
                <td>
                    <d3s-preview-tooltip [objectType]="item.ObjectType" [objectId]="item.ObjectID">{{item.Name}}</d3s-preview-tooltip>
            </td>
            <td>
                <div>{{item.CurrentScore != null ? (item.CurrentScore | scoreDisplay:1) : "" }}</div>
            </td>
            </tr>
        </ng-template>
	    <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords" ></d3s-grid-paging-info>
        </ng-template>
    </p-table>
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
            .subscribe(r => {
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