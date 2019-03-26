import {Component, EventEmitter, Input, OnChanges, Output} from '@angular/core';
import {Router} from '@angular/router';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {RuleStepPromotionHistoryModel} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';

import {BaseComponent} from '../../shared/base.component';

@Component({
    selector: 'd3s-fusion-rule-step-history',
    template: `
        <header>Promotion History
            <d3s-tile-actions hasClose="true" (closeClick)="onClose.emit()" [hasFilterMode]="true"
                              [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
        </header>
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <span *ngIf="!isLoading">
            <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                   (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
                   class="grid-simple-filter">
            <p-table #dt [value]="ruleStepPromotions" selectionMode="single" [metaKeySelection]="true"
                     [globalFilterFields]="['AttributeName','ObjectName','CreatedOn','UpdatedOn']" [pageLinks]="3"
                     [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                <ng-template pTemplate="header">
                    <tr>
                        <th [pSortableColumn]="'AttributeName'" style="width: 25%">
                            Attribute
                            <d3s-sortIcon [field]="'AttributeName'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'ObjectName'" style="width: 25%">
                            Object
                            <d3s-sortIcon [field]="'ObjectName'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'CreatedOn'" style="width: 25%">
                            Created On
                            <d3s-sortIcon [field]="'CreatedOn'"></d3s-sortIcon>
                        </th>
                        <th [pSortableColumn]="'UpdatedOn'" style="width: 25%">
                            Updated On
                            <d3s-sortIcon [field]="'UpdatedOn'"></d3s-sortIcon>
                        </th>
                    </tr>
                    <tr [hidden]="showSimpleFilter">
                        <th><d3s-column-filter [field]="'AttributeName'" [datatype]="'text'"></d3s-column-filter></th>
                        <th><d3s-column-filter [field]="'ObjectName'" [datatype]="'text'"></d3s-column-filter></th>
                        <th><d3s-column-filter [field]="'CreatedOn'" [datatype]="'text'"></d3s-column-filter></th>
                        <th><d3s-column-filter [field]="'UpdatedOn'" [datatype]="'text'"></d3s-column-filter></th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-item>
                    <tr [pSelectableRow]="item">
                        <td>{{item.AttributeName}}</td>
                        <td>
                                <d3s-preview-tooltip [objectType]="item.Object" [objectId]="item.ObjectID"
                                                     (click)="navigate(item.ObjectUrl)">{{item.ObjectName}}</d3s-preview-tooltip>
                        </td>
                        <td>
                              <span>{{item.CreatedOn | date: 'short'}}</span>
                        </td>
                        <td>
                              <span>{{item.UpdatedOn | date: 'short'}}</span>
                        </td>
                    </tr>
                </ng-template>
                <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows"
                                          [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                </ng-template>
            </p-table>
        </span>
    `,
    providers: [FusionService]
})

export class FusionRuleStepHistoryComponent extends BaseComponent implements OnChanges {
    @Input() fusionRuleStepID: number;
    @Output() onClose = new EventEmitter();

    ruleStepPromotions: RuleStepPromotionHistoryModel[] = [];

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService, private router: Router) {
        super();
    }

    ngOnChanges() {
        this.load();
    }

    load() {
        this.ruleStepPromotions = [];

        if (this.fusionRuleStepID == null) {
            return;
        }

        this.isLoading = true;

        this.fusionService
            .getFusionRuleStepPromotionHistory(this.fusionRuleStepID)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                r => {
                    this.ruleStepPromotions = r;

                    this.isLoading = false;
                }
            )
        ;
    }

    navigate(url: string) {
        this.router.navigateByUrl(url);
    }
}
