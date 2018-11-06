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
        <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
        <p-table #dt [value]="values" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['FusionAttributeName']" [pageLinks]="3" [paginator]="true" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
            <ng-template pTemplate="header">
                <tr>
                    <th>Limiting Attribute</th>
                    <th></th>
                </tr>
                <tr [hidden]="showSimpleFilter">
                    <th></th>
                    <th></th>
                </tr>
            </ng-template>
            <ng-template pTemplate="body" let-item>
                <tr [pSelectableRow]="item">
                    <td>{{item.FusionAttributeName}}</td>
                    <td>
                        <div class="RowTools" *ngIf="hasAdd">
                            <a (click)="delete(item);"><i class="fa fa-trash-o"></i></a>
                        </div>
                    </td>
                </tr>
            </ng-template>
            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
            </ng-template>
        </p-table>
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