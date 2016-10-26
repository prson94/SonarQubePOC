import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { Column, Header } from 'primeng/primeng';
import { ResponsibilityDetailForResource } from '../../models/resource.model';
import { ResourcesService } from '../../services/index';
import { FormHelper } from '../../models/form.model';
import { Router } from '@angular/router';

@Component({
    selector: 'd3s-resource-responsibility-grid-component',
    template: `
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;" [hidden]="!simpleFilter">  
    <p-dataTable [globalFilter]="gb" [value]="items" [rows]="10" [paginator]="true" selectionMode="single" (onRowDblclick)="navigate($event)">
        <p-column header="Name" field="ObjectName" [filter]="!simpleFilter">
            <template let-row="rowData" pTemplate type="body">
                <d3s-tooltip [objectType]="row.ObjectType" [objectId]="row.ObjectID" tooltipType="preview">{{row.ObjectName}}</d3s-tooltip>
            </template>
        </p-column>
        <p-column field="Role" header="Role" [filter]="!simpleFilter"></p-column>
        <p-column header="Current Score">
            <template let-row="rowData" pTemplate type="body">
                <div>{{row.CurrentScore | scoreDisplay }}</div>
            </template>
        </p-column>
    </p-dataTable>
</div>
`,
})
export class ResourceResponsibilityGridComponent implements OnInit, OnChanges {
    @Input() Id: number;
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() type: string;
    @Input() simpleFilter: boolean = false;
    isLoading = false;
    private items: ResponsibilityDetailForResource[] = new Array<ResponsibilityDetailForResource>();

    constructor(private resourcesService: ResourcesService, private router: Router) {

    }

    ngOnInit() { }

    ngOnChanges() {
        this.load();
    }


    load() {
        this.isLoading = true;
        this.resourcesService.getResponsibilitiesByResourceByType(this.type, this.Id, this.objectType, this.objectId)
            .then(r => {
                this.items = r;
                FormHelper.convertToNgUrl(this.items, 'ObjectUrl');
                this.isLoading = false;
            });
    }

    navigate(e: any) {
        let url = e.data.ObjectUrl;
        this.router.navigateByUrl(url);

    }
}