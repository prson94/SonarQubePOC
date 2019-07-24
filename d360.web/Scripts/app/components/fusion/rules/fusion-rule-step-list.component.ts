import {Component, EventEmitter, Input, OnChanges, Output, SimpleChanges} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionRule, FusionRuleStep} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';

import {BaseComponent} from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-fusion-rule-step-list',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <header>Steps for selected rule
                <d3s-tile-actions hasAdd="true" (addClick)="add();" [hasFilterMode]="true"
                                  [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
            </header>
            <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                   (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
                   class="grid-simple-filter">
            <p-table #dt [value]="values" [selection]="selection" (selectionChange)="selectionChange.emit($event)"
                     selectionMode="single" [metaKeySelection]="true"
                     [globalFilterFields]="['Step','Action','Description']" [pageLinks]="3" [paginator]="true"
                     [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                <ng-template pTemplate="header">
                    <tr>
                        <th style="width: 10%">Step</th>
                        <th style="width: 15%">Action</th>
                        <th>Description</th>
                        <th style="width: 210px"></th>
                    </tr>
                    <tr [hidden]="showSimpleFilter">
                        <th>
                            <d3s-column-filter [field]="'Step'" [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th>
                            <d3s-column-filter [field]="'Action'" [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th>
                            <d3s-column-filter [field]="'Description'" [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th></th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-item let-rowIndex="rowIndex">
                    <tr [pSelectableRow]="item">
                        <td>{{item.Step}}</td>
                        <td>{{item.Action}}</td>
                        <td>{{item.Description}}</td>
                        <td>
                            <div class="RowTools">
                                <a (click)="history(item)"><i class="fa fa-history"></i></a>
                                <a (click)="edit(item);"><i class="fa fa-pencil"></i></a>
                                <a (click)="delete(item);"><i class="fa fa-trash-o"></i></a>
                                <a *ngIf="rowIndex > 0" (click)="move(item, true);"><i class="fa fa-caret-up"></i></a>
                                <a *ngIf="rowIndex < (values.length - 1)" (click)="move(item, false);"><i
                                        class="fa fa-caret-down"></i></a>
                            </div>
                        </td>
                    </tr>
                </ng-template>
                <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows"
                                          [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                </ng-template>
            </p-table>
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

    destroySubject$: Subject<void> = new Subject();

    constructor(
        private fusionService: FusionService,
        private messagesService: MessagesObservableService
    ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['fusionRule'] && changes['fusionRule'].currentValue != changes['fusionRule'].previousValue) {
            this.load();
        }
    }

    load() {
        if (this.fusionRule == null || this.fusionRule.ID == 0) {
            this.values = [];
        } else {
            this.isLoading = true;

            this.fusionService
                .getFusionRuleSteps(this.fusionRule.ID)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.values = r;

                        if (this.values.length > 0) {
                            if (this.selection == null || this.values.findIndex(v => v.ID == this.selection.ID) < 0) {
                                this.selectionChange.emit(this.values[0]);
                            }
                        } else {
                            this.selectionChange.emit(null);
                        }

                        this.isLoading = false;
                    }
                );
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

    move(e: FusionRuleStep,
         up: boolean
    ) {
        this.selectionChange.emit(e);

        if (e == null) {
            return;
        }
        this.fusionService
            .putMoveFusionRuleStep(e.RuleID, e.ID, up)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                () => this.load()
            )
        ;
    }
}
