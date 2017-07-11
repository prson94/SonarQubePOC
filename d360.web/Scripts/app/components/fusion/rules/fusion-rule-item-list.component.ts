import { Input, Component, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { FusionService } from '../../../services/fusion.service';
import { MessagesService } from '../../../services/messages.service';
import { FusionRule, FusionRuleItem } from '../../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-rule-item-list',
    template: `
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div *ngIf="!isLoading">
        <header>Items for selected rule<d3s-tile-actions [hasAdd]="hasAdd" (addClick)="add();"></d3s-tile-actions></header>
        <p-dataTable #dt [value]="values" selectionMode="single" [selection]="selection" (selectionChange)="selectionChange.emit($event)" [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions">
            <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
            <p-column header="Limiting Attribute" field="FusionAttributeName"></p-column>
            <p-column header="">
                <ng-template pTemplate type="body" let-row="rowData">
                    <div class="RowTools" *ngIf="hasAdd">
                        <a (click)="delete(row);"><i class="fa fa-trash-o"></i></a>
                    </div>
                </ng-template>
            </p-column>
        </p-dataTable>
    </div>

`,
    providers: [FusionService]
})

export class FusionRuleItemListComponent extends BaseComponent implements OnChanges {
    @Input() fusionRule: FusionRule;
    @Input() selection: FusionRuleItem;
    @Output() selectionChange = new EventEmitter<FusionRuleItem>();
    @Output() onAddClick = new EventEmitter();
    @Output() onDeleteClick = new EventEmitter();
    @Input() hasAdd: boolean = true;

    values: FusionRuleItem[];

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
        this.fusionService.getFusionRuleItems(this.fusionRule.ID)
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

    delete(e: FusionRuleItem) {
        this.selectionChange.emit(e);
        this.onDeleteClick.emit();
    }

    add() {
        this.onAddClick.emit();
    }
}