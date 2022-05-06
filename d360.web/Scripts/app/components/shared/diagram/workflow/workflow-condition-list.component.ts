import { Component, Output, EventEmitter, Input, OnChanges, SimpleChanges, OnInit } from '@angular/core';
import { CompanySettingsService } from '../../../../services/settings.service';
import { BaseComponent } from '../../../shared/base.component';

@Component({
    selector: 'd3s-workflow-condition-list',
    template: `
    <header>
                <div class="row" *ngIf="!isLoading && isAllAnyVisible()">
                        <input type="radio" name="isAll"
                               [(ngModel)]="satisfyAll"
                               (ngModelChange)="connectorChange.emit($event)"
                               [value]="true"
                               style="width: 15px;height:20px;" />
                        <div class="FieldName" style="display:inline-block;">
                            Satisfy all&nbsp;&nbsp;&nbsp;&nbsp;
                        </div>
                        <input type="radio" name="isAll"
                               [(ngModel)]="satisfyAll"
                               (ngModelChange)="connectorChange.emit($event)"
                               [value]="false"
                               style="width: 15px;height:20px;" />
                        <div class="FieldName" style="display:inline-block;" i18n>
                            Satisfy any
                        </div>
                    </div>
        <d3s-tile-actions hideTooltip="true" [hasAdd]="!readonly" (addClick)="addClick.emit()"></d3s-tile-actions>
</header>
    <p-table #dt [value]="filteredConditions" selectionMode="single" [metaKeySelection]="true" [pageLinks]="3" [paginator]="true" [rows]="10" [rowsPerPageOptions]="defaultPagingOptions">
        <ng-template pTemplate="header">
            <tr>
                <th i18n>Field Name</th>
                <th i18n>Operator</th>
                <th i18n>Value</th>
                <th *ngIf="!readonly"></th>
            </tr>
        </ng-template>
        <ng-template pTemplate="body" let-item>
            <tr [pSelectableRow]="item">
                <td>{{item['@FieldName'] ? item['@FieldName'] :  item['@FieldLabel']}}</td>
                <td>
                    {{operatorLabel(item)}}
                </td>
                <td>
                    {{valueLabel(item)}}
                </td>
                <td *ngIf="!readonly">
                    <div class="RowTools">
                        <a style="cursor:pointer;" (click)="removeClick.emit(item)"><i class="fa fa-trash"></i></a>
                    </div>
                </td>
            </tr>
        </ng-template>
        <ng-template pTemplate="summary">
            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
        </ng-template>
    </p-table>
`
})

export class WorkflowConditionListComponent extends BaseComponent implements OnChanges, OnInit {
    @Input() conditions: any[] = [];
    @Input() selection;
    @Input() readonly = false;
    @Input() satisfyAll: boolean = true;
    @Input() hideAllAnyOption: boolean = false;
    @Output() selectionChange = new EventEmitter();
    @Output() addClick = new EventEmitter();
    @Output() removeClick = new EventEmitter();
    @Output() editClick = new EventEmitter();
    @Output() connectorChange = new EventEmitter();

    filteredConditions: any[] = [];

    excludedContextualFields = [
        'IssueObject',
        'IssueObjectID',
        'ScoreType'
    ];

    ngOnInit() {
        this.satisfyAll = this.conditions.every(c => c["@Connector"] == "AND");
    }

    ngOnChanges(changes: SimpleChanges) {
        this.filteredConditions = this.conditions.filter(c => c['@ContextualFieldID'] == null || this.excludedContextualFields.indexOf(c['@ContextualFieldID']) == -1);
    }

    isAllAnyVisible() {
        return !this.hideAllAnyOption && this.conditions.filter(x => x["@FieldTypeID"]).length > 1;
    }

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    operatorLabel(item: any): string {
        if (item == null || item['@Operator'] == null)
            return null;

        switch (item['@Operator']) {
            case 'C':
                return 'value changed';
            case 'P':
                return 'is populated';
            case 'NP':
                return 'is not populated';
            default:
                return item['@Operator']
        }
    }

    valueLabel(item: any): string {
        if (item == null || item['@Operator'] == null)
            return null;

        switch (item['@Operator']) {
            case 'C':
                return '[any value change]';
            case 'P':
                return '[any value]';
            case 'NP':
                return '[no value]';
            default:
                return (item['@ValueLabel'] == null ? item['@Value'] : item['@ValueLabel']);
        }
    }
}