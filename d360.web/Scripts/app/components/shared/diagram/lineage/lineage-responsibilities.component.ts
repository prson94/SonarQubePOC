import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../../services/diagram.service';
import { Responsibility } from '../../../../models/lineage.model';
import { BaseComponent } from '../../base.component';

@Component({
    selector: 'd3s-lineage-responsibilities',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <p-dataTable #dt [value]="items" [rowsPerPageOptions]="defaultPagingOptions" >
                <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                <p-column field="ResponsibilityTypeName" header="Role"></p-column>
                <p-column field="SecurityAssetName" header="Resource/Group"></p-column>
            </p-dataTable>
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