import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../../services/diagram.service';
import { Responsibility } from '../../../../models/lineage.model';
import { BaseComponent } from '../../base.component';
import { ObjectDetailService } from '../../../../services/object-detail.service';

@Component({
    selector: 'd3s-lineage-responsibilities',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true" [rowsPerPageOptions]="defaultPagingOptions" [rows]="5">
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
                <ng-template pTemplate="summary">
                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                </ng-template>
            </p-table>
        </div>
    `,
    providers: [DiagramService, ObjectDetailService]
})

export class LineageResponsibilitiesComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() assetId: number;
    isLoading = false;

    @Input() objectType: string;
    @Input() objectId: number;

    items: Responsibility[] = [];

    constructor(private diagramService: DiagramService, private objectDetailService: ObjectDetailService) {
        super();
    }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() { }

    private load() {
        // if the object type and objectid is passed and the assetid is null lookup the assetid then load responsibilities
        if (this.objectType && this.objectId != undefined && this.assetId == null) {
            this.objectDetailService.getObject(this.objectId, this.objectType)
                .then(data => {
                    this.assetId = data.AssetID;
                    this.loadResponsibilities();
                })
        }
        else {
            this.loadResponsibilities();
        }
    }

    private loadResponsibilities() {
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