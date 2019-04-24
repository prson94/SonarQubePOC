import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';
import {takeUntil} from "rxjs/operators";

import {FusionRule, FusionRuleFilterEditorModel, FusionRuleFilterItem} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';
import {MessagesService} from '../../../services/messages.service';

import {BaseComponent} from '../../shared/base.component';
import {Subject} from "rxjs";

@Component({
    selector: 'd3s-fusion-rule-filter-editor',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <div class="row">
                <div class="col l5 m5 s12">
                    <div class="FieldNameRequired">Name:</div>
                    <div><input style="width: 100%" type="text" [(ngModel)]="model.Name" required/></div>
                </div>
            </div>
            <div class="row" style="margin-top: 20px">
                <div class="col l6 m6 s12">
                    <div style="float: left" class="FieldNameRequired">Filter Fields:</div>
                    <div style="float: right; width: 150px; text-align: right">
                        <input type="checkbox" [(ngModel)]="model.All"/> All Items
                    </div>
                    <div style="clear:both"></div>
                    <div class="form-instructions">Choose one or more fields below to act as filters for the set of
                        attributes you want to apply the rule to. If you want to use all items of this type, please
                        select the All Items checkbox above.
                    </div>
                    <table class="striped highlight responsive-table">
                        <thead>
                        <tr>
                            <th>Field</th>
                            <th>Condition</th>
                            <th>Value</th>
                            <th>
                                <button type="button" class="WhiteButton" (click)="addFilterItem()"
                                        [disabled]="model.All"><i class="fa fa-plus" title="Add filter field"></i>
                                </button>
                            </th>
                        </tr>
                        </thead>
                        <tbody>
                        <tr *ngFor="let fld of model.Items">
                            <td>
                                <select [disabled]="model.All" [(ngModel)]="fld.FieldTypeID"
                                        (change)="onFieldTypeChange(fld)">
                                    <option *ngFor="let item of model.FieldTypes"
                                            [value]="item.ID">{{ item.Name }}</option>
                                </select>
                            </td>
                            <td>
                                <select [disabled]="model.All" *ngIf="fld.Type == 'Boolean'" [(ngModel)]="fld.Operator">
                                    <option *ngFor="let item of model.BoolOperators" [value]="item">{{ item }}</option>
                                </select>
                                <select [disabled]="model.All" *ngIf="fld.Type == 'Text'" [(ngModel)]="fld.Operator">
                                    <option *ngFor="let item of model.TextOperators" [value]="item">{{ item }}</option>
                                </select>
                            </td>
                            <td>
                                <input [disabled]="model.All" *ngIf="fld.Type == 'Boolean'" type="checkbox"
                                       [(ngModel)]="fld.Value"/>
                                <input [disabled]="model.All" *ngIf="fld.Type == 'Text'" type="text"
                                       [(ngModel)]="fld.Value"/>
                            </td>
                            <td>
                                <button type="button" [disabled]="model.All" class="WhiteButton"
                                        (click)="deleteFilterItem(fld)">
                                    <i class="fa fa-trash" title="Remove filter field"></i>
                                </button>
                            </td>
                        </tr>
                        </tbody>
                    </table>
                </div>

                <div class="col l1 m1 s12">
                </div>

                <div class="col l5 m5 s12">
                    <div style="float: left" class="FieldName">Test Your Results:</div>
                    <div style="float: right; width: 75px; text-align: right">
                        <button type="button" label="Test" (click)="getTestResults()"
                                [disabled]="QueryExecuting || model.All" pButton></button>
                    </div>
                    <div style="clear:both"></div>
                    <div class="form-instructions">Run a test to see the returned results, according to your filters.
                    </div>

                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                           (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
                           class="grid-simple-filter">
                    <p-table #dt [value]="queryValues" selectionMode="single" [metaKeySelection]="true"
                             [globalFilterFields]="['Name']" [pageLinks]="3" [paginator]="true"
                             [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Name'" style="width: 90%">
                                    Name
                                    <d3s-sortIcon [field]="'Name'"></d3s-sortIcon>
                                </th>
                            </tr>
                            <tr [hidden]="showSimpleFilter">
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr [pSelectableRow]="item">
                                <td>{{item.Name}}</td>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows"
                                                  [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>

                </div>
            </div>

            <div class="row" style="margin-top: 20px">
                <div class="col s12">
                    <button type="button" label="Save" (click)="save()"
                            [disabled]="isLoading || model?.Name?.length < 1" pButton></button>
                    <button type="button" label="Close" (click)="onClose.emit()" pButton></button>
                </div>
            </div>
        </div>
    `,
    providers: [FusionService]
})

export class FusionRuleFilterEditorComponent extends BaseComponent implements OnInit {
    @Input() fusionRule: FusionRule;
    @Input() fusionRuleFilterID: number;
    @Output() onSave = new EventEmitter();
    @Output() onClose = new EventEmitter();
    @Output() onError = new EventEmitter();

    QueryExecuting: boolean = false;
    queryValues: any[] = [];

    model: FusionRuleFilterEditorModel;

    destroySubject$: Subject<void> = new Subject();

    constructor(private fusionService: FusionService, private messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        if (this.fusionRule == null || this.fusionRule.ID == null)
            return;
        this.isLoading = true;

        if (this.fusionRuleFilterID == null) {
            //This is an ADD
            this.fusionService
                .getAddFusionRuleFilter(this.fusionRule.ID)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.model = r;

                        let fti = new FusionRuleFilterItem();

                        fti.FieldTypeID = 0;
                        fti.Operator = this.model.TextOperators[0];
                        fti.Type = "Text";

                        this.model.Items.push(fti);

                        this.isLoading = false;
                    });
        } else {
            //This is an EDIT
            this.fusionService
                .getEditFusionRuleFilter(this.fusionRuleFilterID)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.model = r;

                        this.isLoading = false;
                    }
                )
            ;
        }
    }

    onFieldTypeChange(ft) {
        let selectedFieldType = this.model.FieldTypes.find(o => {
            return o.ID == ft.FieldTypeID
        });

        ft.Type = selectedFieldType.Type;
    }

    addFilterItem() {
        let fti = new FusionRuleFilterItem();

        fti.FieldTypeID = 0;
        fti.Operator = this.model.TextOperators[0];
        fti.Type = "Text";

        this.model.Items.push(fti);
    }

    deleteFilterItem(fld) {
        let ix: number = this.model.Items.indexOf(fld);

        this.model.Items.splice(ix, 1);
    }

    getTestResults() {
        this.QueryExecuting = true;
        this.fusionService
            .getFusionRuleFilterTestResults(this.model)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                r => {
                    this.queryValues = <any>r;

                    this.QueryExecuting = false;
                }
            )
        ;
    }

    save() {
        let form: any = {};

        if (this.isLoading) {
            return;
        }

        this.isLoading = true;

        if (this.fusionRuleFilterID == null) {
            //This is an ADD
            this.fusionService
                .postAddFusionRuleFilter(this.model)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.showMessageForResult(this.messagesService, <any>r);

                        this.isLoading = false;
                        this.onSave.emit();
                    }
                )
            ;
        } else {
            //This is an EDIT
            this.fusionService
                .putEditFusionRuleFilter(this.model)
                .pipe(takeUntil(this.destroySubject$))
                .subscribe(
                    r => {
                        this.showMessageForResult(this.messagesService, <any>r);

                        this.isLoading = false;
                        this.onSave.emit();
                    }
                )
            ;
        }
    }
}
