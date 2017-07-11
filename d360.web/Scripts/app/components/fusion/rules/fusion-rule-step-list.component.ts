import { Input, Component, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { FusionService } from '../../../services/fusion.service';
import { MessagesService } from '../../../services/messages.service';
import { FusionRule, FusionRuleStep } from '../../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-rule-step-list',
    template: `
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div *ngIf="!isLoading">
        <header>Steps for selected rule <d3s-tile-actions hasAdd="true" (addClick)="add();" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
        <input [hidden]="!showSimpleFilter" #gbRuleSteps type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
        <p-dataTable #dtRuleSteps [globalFilter]="gbRuleSteps" [value]="values" selectionMode="single" [selection]="selection" (selectionChange)="selectionChange.emit($event)" paginator="true" pageLinks="3" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
            <p-footer *ngIf="dtRuleSteps.totalRecords"><d3s-grid-paging-info [totalRecords]="dtRuleSteps.totalRecords" [first]="dtRuleSteps.first" [rows]="dtRuleSteps.rows"></d3s-grid-paging-info></p-footer>
            <p-column header="Step" field="Step" [style]="{width:'10%'}" [filter]="!showSimpleFilter"></p-column>
            <p-column header="Action" field="Action" [style]="{width:'15%'}" [filter]="!showSimpleFilter"></p-column>
            <p-column header="Description" field="Description" [filter]="!showSimpleFilter"></p-column>
            <p-column header="" [style]="{width:'210px'}">
                <ng-template pTemplate type="body" let-row="rowData" let-i="rowIndex">
                    <div class="RowTools">
                        <a (click)="history(row)"><i class="fa fa-history"></i></a>
                        <a (click)="edit(row);"><i class="fa fa-pencil"></i></a>
                        <a (click)="delete(row);"><i class="fa fa-trash-o"></i></a>
                        <a *ngIf="i > 0" (click)="move(row, true);"><i class="fa fa-caret-up"></i></a>
                        <a *ngIf="i < (values.length - 1)" (click)="move(row, false);"><i class="fa fa-caret-down"></i></a>
                    </div>
                </ng-template>
            </p-column>
        </p-dataTable>
    </div>

`,
    providers: [FusionService]
})

export class FusionRuleStepListComponent extends BaseComponent implements OnChanges {
    @Input() fusionRule: FusionRule;
    @Input() selection: FusionRuleStep = null;
    @Output() selectionChange = new EventEmitter<FusionRuleStep>();
    @Output() onAddClick = new EventEmitter();
    @Output() onHistoryClick = new EventEmitter();
    @Output() onEditClick = new EventEmitter();
    @Output() onDeleteClick = new EventEmitter();

    values: FusionRuleStep[] = [];

    constructor(private fusionService: FusionService, private messagesService: MessagesService) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['fusionRule'] && changes['fusionRule'].currentValue != changes['fusionRule'].previousValue)
            this.load();
    }

    load() {
        if (this.fusionRule == null || this.fusionRule.ID == 0) {
            this.values = [];
        } else {
            this.isLoading = true;
            this.fusionService.getFusionRuleSteps(this.fusionRule.ID)
                .then(r => {
                    this.values = r;
                    if (this.values.length > 0) {
                        if (this.selection == null || this.values.findIndex(v => v.ID == this.selection.ID) < 0)
                            this.selectionChange.emit(this.values[0]);
                    } else
                        this.selectionChange.emit(null);
                    this.isLoading = false;
                });
        }
    }

    add() {
        this.onAddClick.emit();
    }

    edit(e: FusionRuleStep) {
        this.selectionChange.emit(e);
        this.onEditClick.emit(e);
    }

    delete(e: FusionRuleStep) {
        this.selectionChange.emit(e);
        this.onDeleteClick.emit(e);
    }

    history(e: FusionRuleStep) {
        this.selectionChange.emit(e);
        this.onHistoryClick.emit(e);
    }

    move(e: FusionRuleStep, up: boolean) {
        this.selectionChange.emit(e);
        if (e == null)
            return;
        this.fusionService.putMoveFusionRuleStep(e.RuleID, e.ID, up)
            .then(() => this.load());
    }
}