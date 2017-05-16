import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../../services/diagram.service';
import { TechnicalRelation } from '../../../../models/lineage.model';
import { BaseComponent } from '../../base.component';

@Component({
    selector: 'd3s-lineage-technical',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <p-dataTable #dt [value]="items" [rowsPerPageOptions]="defaultPagingOptions" >
                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                <p-column field="ObjectName" header="Name">
                    <template let-item="rowData" pTemplate type="body">
                        <div class="cell-value-name">{{item.ObjectName}}</div>
                        <div class="cell-value-type">{{item.ObjectTypeName}}</div>
                    </template>
                </p-column>
            </p-dataTable>
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