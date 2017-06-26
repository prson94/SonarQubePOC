import { Input, Component, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { FusionService } from '../../../services/fusion.service';
import { MessagesService } from '../../../services/messages.service';
import { FusionRule } from '../../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-rule-list',
    template: `
<header>Rules<d3s-tile-actions hasAdd="true" (addClick)="add();" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
<input [hidden]="!showSimpleFilter" #gbRules type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
<p-dataTable #dtRules [globalFilter]="gbRules" [value]="values" selectionMode="single" [selection]="selection" (selectionChange)="selectionChange.emit($event)" paginator="true" pageLinks="3" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions">
    <footer *ngIf="dtRules.totalRecords"><d3s-grid-paging-info [totalRecords]="dtRules.totalRecords" [first]="dtRules.first" [rows]="dtRules.rows"></d3s-grid-paging-info></footer>
    <p-column header="Enabled" field="Enabled" sortable="true" [filter]="!showSimpleFilter" [style]="{width:'15%'}" filterMatchMode="equals">
        <ng-template let-item="rowData" pTemplate type="body">
            <i *ngIf="item.Enabled" class="fa fa-check enabled" title="Enabled"></i>
            <i *ngIf="!item.Enabled" class="fa fa-times disabled" title="Disabled"></i>
        </ng-template>
    </p-column>
    <p-column header="Name" field="ObjectName" [filter]="!showSimpleFilter"></p-column>
    <p-column header="Description" field="Description" [filter]="!showSimpleFilter"></p-column>
    <p-column header="" [style]="{ 'width' : '100px'}">
        <ng-template pTemplate type="body" let-row="rowData">
            <div class="RowTools">
                <a (click)="edit(row);"><i class="fa fa-pencil"></i></a>
                <a (click)="delete(row);"><i class="fa fa-trash-o"></i></a>
            </div>
        </ng-template>
    </p-column>
</p-dataTable>
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

    constructor(private fusionService: FusionService, private messagesService: MessagesService) {
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
        this.fusionService.getFusionRules(this.fusionID)
            .then(r => {
                this.values = r;
                if (this.values != null && this.values.length > 0 && this.selection == null)
                    this.selectionChange.emit(this.values[0]);
                this.isLoading = false;
            });
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