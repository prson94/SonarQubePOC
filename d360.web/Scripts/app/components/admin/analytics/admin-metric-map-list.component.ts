import { Input, Component, EventEmitter, Output, OnInit, OnChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { Item } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { MessagesService } from '../../../services/messages.service';

@Component({
    selector: 'd3s-admin-metric-map-list',
    template: ` 
                <header *ngIf="formMode == FormMode.Default">
                    {{groupName == "" ? "Mappings" : "Mappings for " + groupName}}
                    <d3s-tile-actions hasAdd="true" (addClick)="add()"></d3s-tile-actions>   
                </header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
               <div *ngIf="!isLoading">
                    <div [ngSwitch]="formMode">
                        <div *ngSwitchCase="FormMode.Default">                                            
                            <p-dataTable [value]="maps" selectionMode="single" [(selection)]="selection">
                                <p-column field="itemName" header="Item" ></p-column>
                                <p-column field="objectName" header="Object" ></p-column>
                                <p-column field="Weight" header="Weight" ></p-column>
                               <p-column field="ID" header="Status" [sortable]="false" [filter]="!showSimpleFilter">
                                    <ng-template pTemplate type="body" let-item="rowData">
                                        <span>{{isActive(item)}}</span>
                                    </ng-template>
                                </p-column>
                                <p-column [style]="{width:'40px'}">
                                    <ng-template let-map="rowData" pTemplate type="body">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selection = map; edit()"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </ng-template>
                                </p-column>                            
                                <p-column  [style]="{width:'40px'}">
                                    <ng-template let-map="rowData" pTemplate type="body">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="selection = map; delete()"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </ng-template>
                                </p-column> 
                            </p-dataTable>  
                        </div>
                        <div *ngSwitchCase="FormMode.Adding">
                             <d3s-admin-metric-map-editor 
                                [mapId]="0"
                                [groupId]="groupId"
                                (onCancel)="formMode = FormMode.Default;"
                                (onSave)="formMode = FormMode.Default; load();">
                            </d3s-admin-metric-map-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Editing">
                            <d3s-admin-metric-map-editor 
                                [mapId]="selection?.ID"
                                [groupId]="groupId"
                                (onCancel)="formMode = FormMode.Default;"
                                (onSave)="formMode = FormMode.Default; load();">
                            </d3s-admin-metric-map-editor>
                        </div>
                        <div *ngSwitchCase="FormMode.Deleting">
                            <header>
                                Delete Mapping
                            </header>
                            <d3s-delete-form
                                [uri]="'form/MetricMap?id=' + selection?.ID"
                                [method]="'delete'"
                                [prompt]="'Are you sure you want to delete this mapping?'"                                         
                                (onCancel)="formMode = FormMode.Default"
                                (onDeleteSuccess)="deleteEvent($event); formMode = FormMode.Default; load();"
                                (onDeleteFail)="deleteEvent($event); formMode = FormMode.Default">
                            </d3s-delete-form> 
                        </div>
                    </div>    
                </div>
                `,
    providers: [MetricsService]
})

export class AdminMetricMapListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() groupId: number;
    @Input() groupName: string = "";
    @Output() editClick = new EventEmitter();
    @Output() deleteClick = new EventEmitter();
    @Output() addClick = new EventEmitter();

    private maps = [];
    private selection = null;
    private formMode = FormMode.Default;
    FormMode = FormMode;

    constructor(private metricsService: MetricsService, protected messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges() {
        this.formMode = FormMode.Default;
        this.load();
    }

    load(): Promise<any> {
        this.isLoading = true;
        return this.metricsService.getMaps(this.groupId)
            .then(r => {
                this.maps = r;
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

    deleteEvent(e: any) {
        this.showMessageForResult(this.messagesService, e.result);
    }

    isActive(item: Item): string {
        //if (item.EffectiveStartDate == null || item.EffectiveStartDate == "")
        //    return 'Inactive';

        //var start = new Date(item.EffectiveStartDate);
        //var end = new Date(item.EffectiveEndDate);
        //var now = new Date(Date.now());

        //if (start < now) {
        //    if (item.EffectiveEndDate == null || item.EffectiveEndDate == "")
        //        return 'Active';
        //    if (end > now)
        //        return 'Active';
        //}

        //return 'Inactive';
        return 'Active';

    }
};