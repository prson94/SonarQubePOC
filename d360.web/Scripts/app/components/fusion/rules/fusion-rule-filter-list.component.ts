import {Component, EventEmitter, Input, OnChanges, Output, SimpleChanges} from '@angular/core';
import {BaseComponent} from '../../shared/base.component';
import {FusionService} from '../../../services/fusion.service';
import {FusionRule, FusionRuleFilter} from '../../../models/fusion.model';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-fusion-rule-filter-list',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <header>Filters for selected rule
                <d3s-tile-actions [hasAdd]="true" (addClick)="add();"></d3s-tile-actions>
            </header>

            <p-table #dt [value]="values" selectionMode="single" [metaKeySelection]="true"
                     [globalFilterFields]="['Name']" [pageLinks]="3" [selection]="selection"
                     (selectionChange)="selectionChange.emit($event)" [paginator]="true"
                     [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                <ng-template pTemplate="header">
                    <tr>
                        <th>Filter Name</th>
                        <th></th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-item>
                    <tr [pSelectableRow]="item">
                        <td>{{item.Name}}</td>
                        <td>
                            <div class="RowTools">
                                <a (click)="edit(item);"><i class="fa fa-pencil"></i></a>
                                <a (click)="delete(item);"><i class="fa fa-trash-o"></i></a>
                            </div>
                        </td>
                    </tr>
                </ng-template>
                <ng-template pTemplate="summary">
                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows"
                                          [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                </ng-template>
            </p-table>

        </div>

    `,
    providers: [FusionService]
})

export class FusionRuleFilterListComponent extends BaseComponent implements OnChanges {
    @Input() fusionRule: FusionRule;
    @Input() selection: FusionRuleFilter;
    @Output() selectionChange = new EventEmitter<FusionRuleFilter>();
    @Output() onAddClick = new EventEmitter();
    @Output() onDeleteClick = new EventEmitter();
    @Output() onEditClick = new EventEmitter();

    values: FusionRuleFilter[];

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
        if (this.fusionRule == null) {
            return;
        }

        this.isLoading = true;

        this.fusionService
            .getFusionRuleFilters(this.fusionRule.ID)
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
            )
        ;
    }

    delete(e: FusionRuleFilter) {
        this.selectionChange.emit(e);
        this.onDeleteClick.emit();
    }

    edit(e: FusionRuleFilter) {
        this.selectionChange.emit(e);
        this.onEditClick.emit();
    }

    add() {
        this.onAddClick.emit();
    }
}
