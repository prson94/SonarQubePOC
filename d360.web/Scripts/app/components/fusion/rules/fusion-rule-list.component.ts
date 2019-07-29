import {Component, EventEmitter, Input, OnChanges, Output, SimpleChanges} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionRule} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';

import {BaseComponent} from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-fusion-rule-list',
    template: `
        <header>Rules
            <d3s-tile-actions hasAdd="true" (addClick)="add();" [hasFilterMode]="true"
                              [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
               (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
               class="grid-simple-filter">
        <p-table #dt [value]="values" selectionMode="single" [selection]="selection"
                 (selectionChange)="selectionChange.emit($event)" [metaKeySelection]="true"
                 [globalFilterFields]="['Enabled','ObjectName','Description']" [pageLinks]="3" [paginator]="true"
                 [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
            <ng-template pTemplate="header">
                <tr>
                    <th [pSortableColumn]="'Enabled'" style="width: 15%">
                        Enabled
                        <d3s-sortIcon [field]="'Enabled'"></d3s-sortIcon>
                    </th>
                    <th>Name</th>
                    <th>Description</th>
                    <th style="width:    100px"></th>
                </tr>
                <tr [hidden]="showSimpleFilter">
                    <th>
                        <d3s-column-filter [field]="'Enabled'" [datatype]="'text'"></d3s-column-filter>
                    </th>
                    <th>
                        <d3s-column-filter [field]="'ObjectName'" [datatype]="'text'"></d3s-column-filter>
                    </th>
                    <th>
                        <d3s-column-filter [field]="'Description'" [datatype]="'text'"></d3s-column-filter>
                    </th>
                    <th></th>
                </tr>
            </ng-template>
            <ng-template pTemplate="body" let-item>
                <tr [pSelectableRow]="item">
                    <td>
                        <i *ngIf="item.Enabled" class="fa fa-check enabled" title="Enabled"></i>
                        <i *ngIf="!item.Enabled" class="fa fa-times disabled" title="Disabled"></i>
                    </td>
                    <td>{{item.ObjectName}}</td>
                    <td>{{item.Description}}</td>
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
    `,
    providers: [FusionService]
})

export class FusionRuleListComponent extends BaseComponent implements OnChanges {
    @Input() fusionID: number;
    @Input() selection: FusionRule;
    @Output() selectionChange = new EventEmitter<FusionRule>();
    @Output() onEditClick = new EventEmitter();
    @Output() onAddClick = new EventEmitter();
    @Output() onDeleteClick = new EventEmitter();

    values: FusionRule[] = [];

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService, private messagesService: MessagesObservableService) {
        super();

    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['fusionID'] && changes['fusionID'].currentValue != changes['fusionID'].previousValue)
            this.load();
    }

    load() {
        if (this.fusionID == null) {
            this.values = [];
            this.selectionChange.emit(null);

            return;
        }

        this.isLoading = true;

        this.fusionService
            .getFusionRules(this.fusionID)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                r => {
                    this.values = r;

                    if (this.values != null && this.values.length > 0 && this.selection == null) {
                        this.selectionChange.emit(this.values[0]);
                    }

                    this.isLoading = false;
                }
            )
        ;
    }

    edit(e: FusionRule) {
        this.selectionChange.emit(e);
        this.onEditClick.emit(e);
    }

    delete(e: FusionRule) {
        this.selectionChange.emit(e);
        this.onDeleteClick.emit(e);
    }

    add() {
        this.onAddClick.emit();
    }
}
