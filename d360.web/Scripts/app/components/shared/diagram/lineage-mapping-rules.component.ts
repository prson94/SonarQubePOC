import { Component, Input, OnInit, OnChanges, Output, EventEmitter } from '@angular/core';
import { DiagramService } from '../../../services/diagram.service';
import { BaseComponent } from '../base.component';
import { MapItem } from '../../../models/lineage.model';

@Component({
    selector: 'd3s-lineage-mapping-rules',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <header>
                &nbsp;
                <d3s-tile-actions hasExport="true" (exportClick)="export()"></d3s-tile-actions>     
            </header>
            <input #gb type="text" pInputText placeholder="Search..." class="grid-simple-filter">         
            <p-dataTable #dt [globalFilter]="gb" [value]="items" [rowsPerPageOptions]="defaultPagingOptions">
                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                <p-headerColumnGroup>
                    <p-row>
                        <p-column header="Source" colspan="2" [style]="{'text-align' : 'center' }"></p-column>
                        <p-column header="Target" colspan="2" [style]="{'text-align' : 'center' }"></p-column>
                    </p-row>
                    <p-row>
                        <p-column header="Business"></p-column>
                        <p-column header="Technical"></p-column>
                        <p-column header="Business"></p-column>
                        <p-column header="Technical"></p-column>
                    </p-row>
                </p-headerColumnGroup>
                <p-column field="searchableSource" [filter]="!showSimpleFilter">
                    <template let-item="rowData" pTemplate type="body">
                        <span style="margin: 3px 0px 3px 0px">
                            <b>{{item.SourceName}}</b><br/>
                            {{item.SourceType}}
                        </span>
                    </template>
                </p-column>
                <p-column field="searchableSourceFusion" [filter]="!showSimpleFilter">
                    <template let-item="rowData" pTemplate type="body">
                        <span style="margin: 3px 0px 3px 0px">
                            {{item.SourceFusion}}<br/>
                            {{item.SourceFusionAttributeType}}<br/>
                            {{item.SourceFusionAttribute}}
                        </span>
                    </template>
                </p-column>
                <p-column field="searchableTarget" [filter]="!showSimpleFilter">
                    <template let-item="rowData" pTemplate type="body">
                        <span style="margin: 3px 0px 3px 0px">
                            <b>{{item.TargetName}}</b><br/>
                            {{item.TargetType}}
                        </span>
                    </template>
                </p-column>
                <p-column field="searchableTargetFusion" [filter]="!showSimpleFilter">
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
    @Output() onExpandClick = new EventEmitter();

    isLoading = false;

    items: MapItem[];


    constructor(private diagramService: DiagramService) {
        super();
        this.showSimpleFilter = true;
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

                if (this.items && this.items.length > 0)
                    this.items.forEach(i => {
                        //for global filter
                        i.searchableSource = i.SourceName + ' ' + i.SourceType;
                        i.searchableTarget = i.TargetName + ' ' + i.TargetType;
                        i.searchablSourceFusion = i.SourceFusion + ' ' + i.SourceFusionAttribute + ' ' + i.SourceFusionAttributeType;
                        i.searchableTargetFusion = i.TargetFusion + ' ' + i.TargetFusionAttribute + ' ' + i.TargetFusionAttributeType;
                    });
                this.isLoading = false;
            });
    }

    export() {
        //console.log('export');
        this.diagramService.getLineageMapItemsExport(this.source, this.sourceId, this.target, this.targetId);
    }
}