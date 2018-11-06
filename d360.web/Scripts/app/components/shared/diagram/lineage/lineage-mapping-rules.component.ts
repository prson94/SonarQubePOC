import { Component, Input, OnInit, OnChanges, Output, EventEmitter } from '@angular/core';
import { DiagramService } from '../../../../services/diagram.service';
import { BaseComponent } from '../../base.component';
import { MapItem } from '../../../../models/lineage.model';

@Component({
    selector: 'd3s-lineage-mapping-rules',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <header>
                &nbsp;
                <d3s-tile-actions hasExport="true" (exportClick)="export()"></d3s-tile-actions>     
            </header>
            
            <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
            <p-table #dt [value]="items" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['searchableSource','searchableSourceFusion','searchableTarget','searchableTargetFusion']" [rowsPerPageOptions]="defaultPagingOptions" [rows]="10">
                <ng-template pTemplate="header">
                    <tr>
                        <th colspan="2" style="text-align: center">Source</th>
                        <th colspan="2" style="text-align: center">Target</th>
                    </tr>
                    <tr>
                        <th>Business</th>
                        <th>Technical</th>
                        <th>Business</th>
                        <th>Technical</th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-item>
                    <tr [pSelectableRow]="item">
                        <td>
                            <span style="margin: 3px 0px 3px 0px">
                                <b>{{item.SourceName}}</b><br />
                                {{item.SourceType}}
                            </span>
                        </td>
                        <td>
                            <span style="margin: 3px 0px 3px 0px">
                                {{item.SourceFusion}}<br />
                                {{item.SourceFusionAttributeType}}<br />
                                {{item.SourceFusionAttribute}}
                            </span>
                        </td>
                        <td>
                            <span style="margin: 3px 0px 3px 0px">
                                <b>{{item.TargetName}}</b><br />
                                {{item.TargetType}}
                            </span>
                        </td>
                        <td>
                            <span style="margin: 3px 0px 3px 0px">
                                {{item.TargetFusion}}<br />
                                {{item.TargetFusionAttributeType}}<br />
                                {{item.TargetFusionAttribute}}
                            </span>
                        </td>
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