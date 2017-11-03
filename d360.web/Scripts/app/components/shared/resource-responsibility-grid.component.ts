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
    <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter" [hidden]="!simpleFilter">  
    <p-dataTable #dt [globalFilter]="gb" [value]="items" [rows]="10" [paginator]="true" selectionMode="single">
        <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
        <p-column header="Name" field="ObjectName" [filter]="!simpleFilter" sortable="true">
            <ng-template let-row="rowData" pTemplate type="body">
                <d3s-tooltip [objectType]="row.Object" [objectId]="row.ObjectID" tooltipType="preview">{{row.ObjectName}}</d3s-tooltip>
            </ng-template>
        </p-column>
        <p-column field="ResponsibilityTypeName" header="Role" [filter]="!simpleFilter" sortable="true"></p-column>
        <p-column header="Via" field="SecurityAssetName" [filter]="!simpleFilter" sortable="true">
            <ng-template let-row="rowData" pTemplate type="body">
                <div *ngIf="row.SecurityAsset != 'R'">{{row.SecurityAssetName}}</div>
            </ng-template>
        </p-column>
    </p-dataTable>
</div>
`,
})
export class ResourceResponsibilityGridComponent implements OnChanges {
    @Input() Id: number;
    @Input() objectId: number;
    @Input() objectType: string;
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
        this.resourcesService.getResponsibilitiesByResourceByType(this.type, this.Id, this.objectType, this.objectId)
            .then(r => {
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