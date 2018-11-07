import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../../services/diagram.service';
import { Responsibility } from '../../../../models/lineage.model';
import { BaseComponent } from '../../base.component';

@Component({
    selector: 'd3s-lineage-responsibilities',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true" [rowsPerPageOptions]="defaultPagingOptions">
                <ng-template pTemplate="header">
                    <tr>
                        <th>Role</th>
                        <th>Resource/Group</th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-item>
                    <tr [pSelectableRow]="item">
                        <td>{{item.ResponsibilityTypeName}}</td>
                        <td>{{item.SecurityAssetName}}</td>
                    </tr>
                </ng-template>
                <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                </ng-template>
            </p-table>
        </div>
    `,
    providers: [DiagramService]
})

export class LineageResponsibilitiesComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() assetId: number;
    isLoading = false;

    items: Responsibility[] = [];

    constructor(private diagramService: DiagramService) {
        super();
    }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() { }

    load() {

        if (this.assetId == null || this.assetId < 1) {
            this.items = [];
            return;
        }

        this.isLoading = true;
        this.diagramService.getLineageResponsibilities(this.assetId)
            .then(data => {
                this.isLoading = false;
                this.items = data;
            });
    }
}