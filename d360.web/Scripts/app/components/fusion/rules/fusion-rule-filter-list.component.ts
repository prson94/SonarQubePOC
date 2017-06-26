import { Input, Component, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { FusionService } from '../../../services/fusion.service';
import { MessagesService } from '../../../services/messages.service';
import { FusionRule, FusionRuleFilter } from '../../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-rule-filter-list',
    template: `
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div *ngIf="!isLoading">
        <header>Filters for selected rule<d3s-tile-actions [hasAdd]="true" (addClick)="add();"></d3s-tile-actions></header>
        <p-dataTable #dt [value]="values" selectionMode="single" [selection]="selection" (selectionChange)="selectionChange.emit($event)" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions">
            <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
            <p-column header="Filter Name" field="Name"></p-column>
            <p-column header="">
                <ng-template pTemplate type="body" let-row="rowData">
                    <div class="RowTools">
                        <a (click)="edit(row);"><i class="fa fa-pencil"></i></a>
                        <a (click)="delete(row);"><i class="fa fa-trash-o"></i></a>
                    </div>
                </ng-template>
            </p-column>
        </p-dataTable>
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

    constructor(private fusionService: FusionService, private messagesService: MessagesService) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes['fusionRule'] && changes['fusionRule'].currentValue != changes['fusionRule'].previousValue)
            this.load();
    }

    load() {
        if (this.fusionRule == null)
            return;
        this.isLoading = true;
        this.fusionService.getFusionRuleFilters(this.fusionRule.ID)
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