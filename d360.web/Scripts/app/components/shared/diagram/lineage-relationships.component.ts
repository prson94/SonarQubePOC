import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../services/diagram.service';
import { RelationItem } from '../../../models/lineage.model';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'd3s-lineage-relations',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <p-dataTable #dt [value]="items" [rowsPerPageOptions]="defaultPagingOptions" >
                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                <p-column field="TypeName" header="Type"></p-column>
                <p-column field="Name" header="Name"></p-column>
            </p-dataTable>
        </div>
    `,
    providers: [DiagramService]
})

export class LineageRelationshipsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() objectType: string;
    @Input() objectId: number;
    isLoading = false;

    items: RelationItem[] = [];

    constructor(private diagramService: DiagramService) {
        super();
    }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() { }

    load() {

        if (this.objectType == null || this.objectId == null) {
            this.items = [];
            return;
        }

        this.isLoading = true;
        this.diagramService.getRelations(this.objectType, this.objectId)
            .then(data => {
                this.isLoading = false;
                this.items = data;
            });
    }
}