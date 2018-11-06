import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../../services/diagram.service';
import { TechnicalRelation } from '../../../../models/lineage.model';
import { BaseComponent } from '../../base.component';

@Component({
    selector: 'd3s-lineage-technical',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['ObjectName']" [rowsPerPageOptions]="defaultPagingOptions" >
                <ng-template pTemplate="header">
                    <tr>
                        <th>Name</th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-item>
                    <tr [pSelectableRow]="item">
                        <td>
                            <div class="cell-value-name">{{item.ObjectName}}</div>
                            <div class="cell-value-type">{{item.ObjectTypeName}}</div>
                        </td>
                    </tr>
                </ng-template>
	            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords" ></d3s-grid-paging-info>
                </ng-template>
            </p-table>
        </div>
    `,
    providers: [DiagramService]
})

export class LineageTechnicalRelationshipsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() source: string;
    @Input() sourceId: number;
    @Input() target: string;
    @Input() targetId: number;

    isLoading = false;

    items: TechnicalRelation[] = [];

    constructor(private diagramService: DiagramService) {
        super();
        
    }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() { }

    load() {
        if (this.source == null || this.sourceId == null || this.target == null || this.targetId == null) {
            this.items = [];
            return;
        }

        this.isLoading = true;
        this.diagramService.getLineageTechnicalRelationships(this.source, this.sourceId, this.target, this.targetId)
            .then(data => {
                this.isLoading = false;
                //console.log(data);
                this.items = data;
            });
    }
}