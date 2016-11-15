import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../services/index';
import { BaseComponent } from '../base.component';
import { MapItem } from '../../../models/lineage.model';

@Component({
    selector: 'd3s-lineage-mapping-rules',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <p-dataTable #dt [value]="items" [rowsPerPageOptions]="defaultPagingOptions">
                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                <p-headerColumnGroup>
                    <p-row>
                        <p-column header="Source" colspan="2"></p-column>
                        <p-column header="Target" colspan="2"></p-column>
                    </p-row>
                    <p-row>
                        <p-column header="Business"></p-column>
                        <p-column header="Technical"></p-column>
                        <p-column header="Business"></p-column>
                        <p-column header="Technical"></p-column>
                    </p-row>
                </p-headerColumnGroup>
                <p-column field="Source">
                    <template let-item="rowData" pTemplate type="body">
                        <span style="margin: 3px 0px 3px 0px">
                            <b>{{item.SourceName}}</b><br/>
                            {{item.SourceType}}
                        </span>
                    </template>
                </p-column>
                <p-column field="SourceID">
                    <template let-item="rowData" pTemplate type="body">
                        <span style="margin: 3px 0px 3px 0px">
                            {{item.SourceFusion}}<br/>
                            {{item.SourceFusionAttributeType}}<br/>
                            {{item.SourceFusionAttribute}}
                        </span>
                    </template>
                </p-column>
                <p-column field="Target">
                    <template let-item="rowData" pTemplate type="body">
                        <span style="margin: 3px 0px 3px 0px">
                            <b>{{item.TargetName}}</b><br/>
                            {{item.TargetType}}
                        </span>
                    </template>
                </p-column>
                <p-column field="TargetID">
                    <template let-item="rowData" pTemplate type="body">
                        <span style="margin: 3px 0px 3px 0px">
                            {{item.TargetFusion}}<br/>
                            {{item.TargetFusionAttributeType}}<br/>
                            {{item.TargetFusionAttribute}}
                        </span>
                    </template>
                </p-column>
            </p-dataTable>
        </div>
    `,
    providers: [DiagramService]
})

export class LineageMappingRulesComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() source: string;
    @Input() sourceId: number;
    @Input() target: string;
    @Input() targetId: number;

    isLoading = false;

    items: MapItem[];

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
        this.diagramService.getLineageMapItems(this.source, this.sourceId, this.target, this.targetId)
            .then(data => {
                this.items = data;
                this.isLoading = false;
            });
    }
}