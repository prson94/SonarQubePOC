import {Component, Input, OnInit} from '@angular/core';
import {Subject} from "rxjs";
import {takeUntil} from "rxjs/operators";

import {FusionExecutionError} from '../../models/fusion.model';

import {FusionService} from '../../services/fusion.service';

import {BaseComponent} from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-execution-errors',
    template: `
        <header>Execution History - Error Details
            <d3s-tile-actions [hasExport]="true" (exportClick)="export()"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <span *ngIf="!isLoading">
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                           (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
                           class="grid-simple-filter">
                    <p-table #dt [value]="errors" selectionMode="single" [scrollable]="true" scrollWidth="100%"
                             [metaKeySelection]="true" [globalFilterFields]="['Date','Error']" [pageLinks]="3"
                             [paginator]="true" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [(selection)]="selected">
                        <ng-template pTemplate="colgroup">
                            <colgroup>
                                <col style="width:100px">
                                <col style="width:175px">
                            </colgroup>
                        </ng-template>
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Date'" style="width: 100px">
                                    Date
                                    <d3s-sortIcon [field]="'Date'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Error'" style="width: 175px">
                                    Error
                                    <d3s-sortIcon [field]="'Error'"></d3s-sortIcon>
                                </th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr [pSelectableRow]="item">
                                <td>
                                    <span>{{item.Date | date: 'short'}}</span>
                                </td>
                                <td>{{item.Error}}</td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows"
                                                  [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>
                    </span>
    `,
    providers: [FusionService],
})

export class FusionExecutionErrorsComponent extends BaseComponent implements OnInit {
    @Input() executionId: number;

    private errors: FusionExecutionError[] = [];
    private selected: FusionExecutionError;

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;

        this.fusionService
            .getFusionExecutionErrors(this.executionId)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(res => {
                this.errors = res;
                this.selected = this.errors.length > 0 ? this.errors[0] : null;
                this.isLoading = false;
            });
    }

    private export() {
        this.fusionService.getFusionExecutionErrorsExport(this.executionId)
    }
}
