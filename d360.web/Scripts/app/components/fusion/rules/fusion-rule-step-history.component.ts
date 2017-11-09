import { Input, Component, EventEmitter, Output, OnChanges } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { FusionService } from '../../../services/fusion.service';
import { RuleStepPromotionHistoryModel } from '../../../models/fusion.model';
import { Column } from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-rule-step-history',
    template: `
        <header>Promotion History<d3s-tile-actions hasClose="true" (closeClick)="onClose.emit()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <span *ngIf="!isLoading">
            <input [hidden]="!showSimpleFilter" #gbRuleStepsHistory type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
            <p-dataTable #dtRuleStepsHistory [globalFilter]="gbRuleStepsHistory" [value]="ruleStepPromotions" selectionMode="single" paginator="true" pageLinks="3" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                <p-footer *ngIf="dtRuleStepsHistory.totalRecords"><d3s-grid-paging-info [totalRecords]="dtRuleStepsHistory.totalRecords" [first]="dtRuleStepsHistory.first" [rows]="dtRuleStepsHistory.rows"></d3s-grid-paging-info></p-footer>
                <p-column header="Attribute" field="AttributeName" [style]="{width:'25%'}" [filter]="!showSimpleFilter"></p-column>
                <p-column header="Object" field="ObjectName" [style]="{width:'25%'}" [filter]="!showSimpleFilter">
                    <ng-template pTemplate type="body" let-row="rowData">
                        <d3s-tooltip [objectType]="row.Object" [objectId]="row.ObjectID" tooltipType="preview">
                            <a (click)="navigate(row.ObjectUrl)">{{row.ObjectName}}</a>
                        </d3s-tooltip>
                    </ng-template>
                </p-column>

                <p-column header="Created On" field="CreatedOn" [style]="{width:'25%'}" [filter]="!showSimpleFilter">
                    <ng-template pTemplate type="body" let-row="rowData">
                        <span>{{row.CreatedOn | date: 'short'}}</span>
                    </ng-template>
                </p-column>
                <p-column header="Updated On" field="UpdatedOn" [style]="{width:'25%'}" [filter]="!showSimpleFilter">
                    <ng-template pTemplate type="body" let-row="rowData">
                        <span>{{row.UpdatedOn | date: 'short'}}</span>
                    </ng-template>
                </p-column>
            </p-dataTable>
        </span>
`,
    providers: [FusionService]
})

export class FusionRuleStepHistoryComponent extends BaseComponent implements OnChanges {
    @Input() fusionRuleStepID: number;
    @Output() onClose = new EventEmitter();

    ruleStepPromotions: RuleStepPromotionHistoryModel[] = [];
    
    constructor(private fusionService: FusionService, private router: Router) {
        super();
    }

    ngOnChanges() {
        this.load();
    }

    load() {
        this.ruleStepPromotions = [];
        if (this.fusionRuleStepID == null)
            return;
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
