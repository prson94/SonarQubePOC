import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../../services/diagram.service';
import { RelationItem } from '../../../../models/lineage.model';
import { BaseComponent } from '../../base.component';

@Component({
    selector: 'd3s-lineage-relations',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
        <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true" [rowsPerPageOptions]="defaultPagingOptions">
            <ng-template pTemplate="header">
                <tr>
                    <th>Type</th>
                    <th>Name</th>
                </tr>
            </ng-template>
            <ng-template pTemplate="body" let-item>
                <tr [pSelectableRow]="item">
                    <td>{{item.TypeName}}</td>
                    <td>{{item.Name}}</td>
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