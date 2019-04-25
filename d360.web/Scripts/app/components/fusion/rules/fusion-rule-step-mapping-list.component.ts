import {Component, EventEmitter, Input, OnChanges, Output, SimpleChanges} from '@angular/core';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionRuleMapping, FusionRuleStep} from '../../../models/fusion.model';

import {FusionService} from '../../../services/fusion.service';
import {MessagesService} from '../../../services/messages.service';

import {BaseComponent} from '../../shared/base.component';

declare var CompanySettings;

@Component({
    selector: 'd3s-fusion-rule-step-mapping-list',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading">
            <header>Mappings for selected step
                <d3s-tile-actions hasAdd="true" (addClick)="add();" [hasFilterMode]="true"
                                  [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
            </header>
            <input type="text" [hidden]="!showSimpleFilter" pInputText size="100"
                   (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..."
                   class="grid-simple-filter">
            <div *ngIf="UnMappedKeyColumns && UnMappedKeyColumns.length >0" class="red-text left"
                 style="font-weight:bold">
                <span>**Warning: All key fields not mapped < </span><span
                    *ngFor="let c of UnMappedKeyColumns;let first=first;let last=last;">{{c}}<i *ngIf="!last">,</i><i
                    *ngIf="last">></i></span>
            </div>
            <p-table #dt [value]="values" selectionMode="single" [selection]="selection"
                     (selectionChange)="selectionChange.emit($event)" [metaKeySelection]="true"
                     [globalFilterFields]="['SourceFieldName','TargetFieldName']" [pageLinks]="3" [paginator]="true"
                     [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
                <ng-template pTemplate="header">
                    <tr>
                        <th>Source</th>
                        <th>Target</th>
                        <th></th>
                    </tr>
                    <tr [hidden]="showSimpleFilter">
                        <th>
                            <d3s-column-filter [field]="'SourceFieldName'" [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th>
                            <d3s-column-filter [field]="'TargetFieldName'" [datatype]="'text'"></d3s-column-filter>
                        </th>
                        <th></th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-item>
                    <tr [pSelectableRow]="item">
                        <td>{{item.SourceFieldName}}</td>
                        <td>{{item.TargetFieldName}}</td>
                        <td>
                            <div class="RowTools">
                                <a (click)="edit(item);"><i class="fa fa-pencil"></i></a>
                                <a (click)="delete(item);"><i class="fa fa-trash-o"></i></a>
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

export class FusionRuleStepMappingListComponent extends BaseComponent implements OnChanges {
    @Input() fusionRuleStep: FusionRuleStep;
    @Input() selection: FusionRuleMapping;
    @Output() selectionChange = new EventEmitter<FusionRuleMapping>();
    @Output() onAddClick = new EventEmitter();
    @Output() onEditClick = new EventEmitter();
    @Output() onDeleteClick = new EventEmitter();

    values: FusionRuleMapping[];
    UnMappedKeyColumns: string[] = [];

    destroySubject$: Subject<void> = new Subject();

    constructor(
        private fusionService: FusionService,
        private messagesService: MessagesService
    ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['fusionRuleStep'] && changes['fusionRuleStep'].currentValue != changes['fusionRuleStep'].previousValue) {
            this.load();
        }
    }

    load() {
        if (this.fusionRuleStep == null) {
            this.values = [];

            return;
        }

        this.isLoading = true;

        this.fusionService
            .getFusionRuleStepMappings(this.fusionRuleStep.ID)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                r => {
                    //update Source/Target subject area fields with company settings value
                    r.Items.filter(i => i.TargetFieldName == "TaxonomyTypeID").forEach(i => {
                        i.TargetFieldName = (CompanySettings.ArtifactType_TaxonomyTypeID || "Subject Area");
                    });
                    r.Items.filter(i => i.SourceFieldName == "TaxonomyTypeID").forEach(i => {
                        i.SourceFieldName = (CompanySettings.ArtifactType_TaxonomyTypeID || "Subject Area");
                    });

                    this.UnMappedKeyColumns = r.UnMappedKeyColumns;
                    this.values = r.Items;

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

    add() {
        this.onAddClick.emit();
    }

    edit(e: FusionRuleMapping) {
        this.selectionChange.emit(e);
        this.onEditClick.emit();
    }

    delete(e: FusionRuleMapping) {
        this.selectionChange.emit(e);
        this.onDeleteClick.emit();
    }
}
