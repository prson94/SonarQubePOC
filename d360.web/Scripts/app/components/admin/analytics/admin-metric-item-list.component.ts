import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { Item } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-admin-metric-item-list',
    template: ` 
                <header *ngIf="formMode == FormMode.Default">
                    Measures
                    <d3s-tile-actions hasAdd="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>   
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
               <div *ngIf="!isLoading">
                    <div [ngSwitch]="formMode">
                        <div *ngSwitchCase="FormMode.Default">
                            <input [hidden]="!showSimpleFilter" #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                              
                            <p-dataTable #dt  [globalFilter]="gb" [value]="items" selectionMode="single" [paginator]="true" [pageLinks]="3" [rows]="10" [rowsPerPageOptions]="[5,10,20]" [(selection)]="selection">
                                <p-column field="Name" header="Name" [sortable]="false" [filter]="!showSimpleFilter"></p-column>
                                <p-column field="Description" header="Description" [sortable]="false" [filter]="!showSimpleFilter">
                                    <ng-template pTemplate type="body" let-item="rowData">
                                        <span *ngIf="item?.Description == null"></span>
                                        <span *ngIf="item?.Description != null" [innerHTML]="item?.Description"></span>
                                    </ng-template>
                                </p-column>
                               <p-column field="ID" header="Status" [sortable]="false" [filter]="!showSimpleFilter">
                                    <ng-template pTemplate type="body" let-item="rowData">
                                        <span>{{isActive(item)}}</span>
                                    </ng-template>
                                </p-column>
                                <p-column [style]="{width:'40px'}">
                                    <ng-template let-metric="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selection = metric; edit()"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </ng-template>
                                </p-column>                            
                                <p-column  [style]="{width:'40px'}">
                                    <ng-template let-metric="rowData" pTemplate type="body">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selection = metric; delete()"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </ng-template>
                                </p-column> 
                            </p-dataTable>  
                        </div>
                        <div *ngSwitchCase="FormMode.Adding">
                            <d3s-dynamic-editor 
                                [objectID]="selection?.ID" 
                                [objectType]="'MetricItem'" 
                                [title]="'Metric Item'" 
                                [createUri]="'form/dynamicedit/create/metricitem'"
                                [selection]="null" 
                                (saveClick)="formMode = FormMode.Default; load();" 
                                (closeClick)="formMode = FormMode.Default">
                            </d3s-dynamic-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Editing">
                            <d3s-dynamic-editor 
                                [objectID]="selection?.ID" 
                                [objectType]="'MetricItem'" 
                                [title]="'Metric Item'" 
                                [selection]="selection" 
                                [editUri]="'form/dynamicedit/edit/metricitem'"
                                (saveClick)="formMode = FormMode.Default; load();" 
                                (closeClick)="formMode = FormMode.Default">
                            </d3s-dynamic-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Deleting">
                            <header>
                                Delete Item
                            </header>
                            <d3s-delete-form
                                [uri]="'form/MetricItem?id=' + selection?.ID"
                                [method]="'delete'"
                                [prompt]="'Are you sure you want to delete the metric item [' + [selection?.Name] + ']?'"                                         
                                (onCancel)="formMode = FormMode.Default"
                                (onDeleteSuccess)="formMode = FormMode.Default; load();"
                                (onDeleteFail)="formMode = FormMode.Default">
                            </d3s-delete-form> 
                        </div>
                    </div>    
                </div>
                `,
providers: [MetricsService]
})

export class AdminMetricItemListComponent extends BaseComponent implements OnInit {
    @Output() editClick = new EventEmitter();
    @Output() deleteClick = new EventEmitter();
    @Output() addClick = new EventEmitter();

    private items = [];
    private selection = null;
    private formMode = FormMode.Default;
    FormMode = FormMode;

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load(): Promise<any> {
        this.isLoading = true;
        return this.metricsService.getItems()
            .then(r => {
                this.items = r;
                //console.log(this.items, r);
                this.isLoading = false;
            });
    }

    add() {
        this.selection = null;
        this.formMode = FormMode.Adding;
    }

    edit(e: any) {
        this.formMode = FormMode.Editing;
    }

    delete(e: any) {
        this.formMode = FormMode.Deleting;
    }

    isActive(item: Item): string {
        if (item.EffectiveStartDate == null || item.EffectiveStartDate == "")
            return 'Inactive';

        var start = new Date(item.EffectiveStartDate);
        var end = new Date(item.EffectiveEndDate);
        var now = new Date(Date.now());

        if (start < now) {
            if (item.EffectiveEndDate == null || item.EffectiveEndDate == "")
                return 'Active';
            if (end > now)
                return 'Active';
        }

        return 'Inactive';

    }
};