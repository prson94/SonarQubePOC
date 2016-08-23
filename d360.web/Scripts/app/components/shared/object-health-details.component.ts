///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, Output, EventEmitter, OnChanges, SimpleChange} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ScoreService } from '../../services/index';
import { PointBreakdown } from '../../models/score.model';

@Component({
    selector: 'd3s-object-health-details',
    template: `
            <div class="row">
                <div class="col l6 s12">
                    <header>Score History</header>
                </div>
                <div class="col l6 s12">
                    <div class="row">
                        <div class="col s12">
                            <header>Point Breakdown</header>
                            <p-dataTable  scrollable="true" scrollWidth="100%" [value]="pointBreakdown" selectionMode="single">                                
                                <p-column field="Name" header="Analytic" [style]="{'width':'250px'}"></p-column>                                
                                <p-column header="Score" [style]="{'width':'250px'}">
                                    <template let-col let-data="rowData">
                                        <span>{{data.Score}} out of {{data.MaxScore}}</span>
                                    </template>
                                </p-column>
                            </p-dataTable>  
                        </div>
                    </div>
                    <div class="row">
                        <div class="col s12">
                            Score KPIs
                        </div>
                    </div>
                </div>
            </div>
            
        `,
    providers: [ScoreService],
})

export class ObjectHealthDetailsComponent extends BaseComponent implements OnChanges{
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;

    private pointBreakdown: PointBreakdown[] = [];

    constructor(protected scoreService: ScoreService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let requiresLoad: boolean = false;
        for (let p in changes) {
            if (p == 'objectType') {
                requiresLoad = changes['objectType'].currentValue != changes['objectType'].previousValue;
            }
            if (p == 'objectID') {
                requiresLoad = changes['objectID'].currentValue != changes['objectID'].previousValue;
            }
        }

        if (requiresLoad)
            this.load();
    }

    private load() {
        this.isLoading = true;
        this.scoreService.getPointBreakdown(this.objectID, this.objectType)
            .then(res => {
                this.pointBreakdown = res;
                this.isLoading = false;
            });
    }
}