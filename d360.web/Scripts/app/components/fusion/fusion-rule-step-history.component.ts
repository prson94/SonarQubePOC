import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { RuleStepPromotionHistoryModel } from '../../models/fusion.model';
import { Column } from 'primeng/primeng';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-fusion-rule-step-history',
    template: `
                        <header>Promotion History<d3s-tile-actions hasClose="true" (closeClick)="onClose.emit()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <input *ngIf="!isLoading" [hidden]="!showSimpleFilter" #gbRuleStepsHistory type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                        <p-dataTable *ngIf="!isLoading" #dtRuleStepsHistory [globalFilter]="gbRuleStepsHistory" [value]="ruleStepPromotions" selectionMode="single" paginator="true" pageLinks="3" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                            <footer *ngIf="dtRuleStepsHistory.totalRecords"><d3s-grid-paging-info [totalRecords]="dtRuleStepsHistory.totalRecords" [first]="dtRuleStepsHistory.first" [rows]="dtRuleStepsHistory.rows"></d3s-grid-paging-info></footer>
                            <p-column header="Fusion Attribute" field="FusionAttributeName" [style]="{width:'25%'}" [filter]="!showSimpleFilter"></p-column>
                            <p-column header="Object" field="ObjectName" [style]="{width:'25%'}" [filter]="!showSimpleFilter">
                                <template pTemplate type="body" let-row="rowData">
                                    <d3s-tooltip [objectType]="row.Object" [objectId]="row.ObjectID" tooltipType="preview">
                                        <a (click)="navigate(row.ObjectUrl)">{{row.ObjectName}}</a>
                                    </d3s-tooltip>
                                </template>
                            </p-column>

                            <p-column header="Created On" field="CreatedOn" [style]="{width:'25%'}" [filter]="!showSimpleFilter">
                                <template pTemplate type="body" let-row="rowData">
                                    <span>{{row.CreatedOn | date: 'short'}}</span>
                                </template>
                            </p-column>
                            <p-column header="Updated On" field="UpdatedOn" [style]="{width:'25%'}" [filter]="!showSimpleFilter">
                                <template pTemplate type="body" let-row="rowData">
                                    <span>{{row.UpdatedOn | date: 'short'}}</span>
                                </template>
                            </p-column>
                        </p-dataTable>
`,
    providers: [FusionService]
})

export class FusionRuleStepHistoryComponent extends BaseComponent implements OnInit {
    @Input() fusionRuleStepID: number;
    @Output() onClose = new EventEmitter();

    ruleStepPromotions: RuleStepPromotionHistoryModel[] = [];


    constructor(private fusionService: FusionService, private router: Router) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.fusionService.getFusionRuleStepPromotionHistory(this.fusionRuleStepID)
            .then(r => {
                this.ruleStepPromotions = r;
                this.isLoading = false;
            });
    }

    navigate(url: string) {
        this.router.navigateByUrl(url);
    }
}
