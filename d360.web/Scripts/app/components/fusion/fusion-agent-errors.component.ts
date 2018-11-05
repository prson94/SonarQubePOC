import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/fusion.service';
import { FusionAgentError } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-agent-errors',
    template: ` 
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <span *ngIf="!isLoading">
                    <header>Agent Error History</header>
                    <input type="text" [hidden]="!showSimpleFilter" pInputText size="100" (input)="dt.filterGlobal($event.target.value, 'contains')" placeholder="Search..." class="grid-simple-filter">
                    <p-table #dt [scrollable]="true" scrollWidth="100%" [value]="errors" selectionMode="single" [metaKeySelection]="true" [globalFilterFields]="['Message','FusionType','Fusion','Date','MachineName']" [pageLinks]="3" [paginator]="true" [rows]="5" [rowsPerPageOptions]="[5,10,20]" [(selection)]="selected">
                        <ng-template pTemplate="colgroup" >
                            <colgroup>
                                <col style="width:300px">
                                <col style="width:150px">
                                <col style="width:150px">
                                <col style="width:150px">
                                <col style="width:150px">
                            </colgroup>
                        </ng-template>
                        <ng-template pTemplate="header">
                            <tr>
                                <th [pSortableColumn]="'Message'" style="width: 300px">
                                    Error
                                    <d3s-sortIcon [field]="'Message'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'FusionType'" style="width: 150px">
                                    Type
                                    <d3s-sortIcon [field]="'FusionType'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Fusion'" style="width: 150px">
                                    Configuration
                                    <d3s-sortIcon [field]="'Fusion'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Date'" style="width: 150px">
                                    Date
                                    <d3s-sortIcon [field]="'Date'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'MachineName'" style="width: 150px">
                                    Host
                                    <d3s-sortIcon [field]="'MachineName'"></d3s-sortIcon>
                                </th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr (dblclick)="selected=item" [pSelectableRow]="item">
                                <td>{{item.Message}}</td>
                                <td>{{item.FusionType}}</td>
                                <td>{{item.Fusion}}</td>
                                <td>
                                    <span>{{item.Date | date: 'short'}}</span>
                                </td>
                                <td>{{item.MachineName}}</td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                        </ng-template>
                    </p-table>                    
                </span>
          `,
    providers: [FusionService],
})

export class FusionAgentErrorsComponent extends BaseComponent implements OnInit {    
    private errors: FusionAgentError[] = [];
    private selected: FusionAgentError;

    @Input() maxRows: number = 1000;
    @Input() days: number = 0; // 0 = all up to max

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionAgentErrorHistory(this.maxRows, this.days)
            .then(res => {
                this.errors = res;
                this.selected = this.errors.length > 0 ? this.errors[0] : null;
                this.isLoading = false;
            });
    }
};