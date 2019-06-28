import { Component, Input, OnChanges } from '@angular/core';
import { Column, Header } from 'primeng/primeng';
import { ResponsibilityDetailForResource } from '../../models/resource.model';
import { ResourcesService } from '../../services/resources.service';
import { FormHelper } from '../../models/form.model';
import { Router } from '@angular/router';

@Component({
    selector: 'd3s-resource-responsibility-grid-component',
    template: `
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <input type="text" [hidden]="!simpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
    <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['ObjectName','ResponsibilityTypeName','SecurityAssetName']" [paginator]="true" [rows]="10">
        <ng-template pTemplate="header">
            <tr>
                <th [pSortableColumn]="'ObjectName'">
                    Name
                    <d3s-sortIcon [field]="'ObjectName'"></d3s-sortIcon>
                </th>
                <th [pSortableColumn]="'ResponsibilityTypeName'">
                    Role
                    <d3s-sortIcon [field]="'ResponsibilityTypeName'"></d3s-sortIcon>
                </th>
                <th [pSortableColumn]="'SecurityAssetName'">
                    Via
                    <d3s-sortIcon [field]="'SecurityAssetName'"></d3s-sortIcon>
                </th>
            </tr>
            <tr [hidden]="simpleFilter">
                <th><d3s-column-filter [field]="'ObjectName'" [datatype]="'text'"></d3s-column-filter></th>
                <th><d3s-column-filter [field]="'ResponsibilityTypeName'" [datatype]="'text'"></d3s-column-filter></th>
                <th><d3s-column-filter [field]="'SecurityAssetName'" [datatype]="'text'"></d3s-column-filter></th>
            </tr>
        </ng-template>
        <ng-template pTemplate="body" let-item>
            <tr [pSelectableRow]="item">
                <td>
                    <d3s-preview-tooltip [objectType]="item.Object" [objectId]="item.ObjectID">{{item.ObjectName}}</d3s-preview-tooltip>
                </td>
                <td>{{item.ResponsibilityTypeName}}</td>
                <td>
                    <div *ngIf="item.SecurityAsset != 'R'">{{item.SecurityAssetName}}</div>
                </td>
            </tr>
        </ng-template>
        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
        </ng-template>
    </p-table>
</div>
`,
})
export class ResourceResponsibilityGridComponent implements OnChanges {
    @Input() Id: number;
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() responsibilityTypeId: number = null;
    @Input() type: string;
    @Input() simpleFilter: boolean = false;
    isLoading = false;
    private items: ResponsibilityDetailForResource[] = new Array<ResponsibilityDetailForResource>();

    constructor(private resourcesService: ResourcesService, private router: Router) {

    }
    
    ngOnChanges() {
        this.load();
    }


    load() {
        this.isLoading = true;
        this.resourcesService.getResponsibilitiesByResourceByType(this.type, this.Id, this.objectType, this.objectId, this.responsibilityTypeId)
            .subscribe(r => {
                this.items = r;
                //FormHelper.convertToNgUrl(this.items, 'ObjectUrl');
                this.isLoading = false;
            });
    }

    navigate(e: any) {
        //let url = e.data.ObjectUrl;
        //this.router.navigateByUrl(url);

    }
}