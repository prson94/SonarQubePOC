import { Input, Output, Component, EventEmitter, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FusionConfiguration, FusionType, FusionFilter, FusionSchedule, FusionScheduleDay } from '../../../models/fusion.model';
import { FusionService } from '../../../services/fusion.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesService } from '../../../services/messages.service';
 
@Component({
    selector: 'd3s-fusion-schedule',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div *ngIf="!isLoading && !showEditor && !showDelete">            
            <header>Agent Execution Schedule<d3s-tile-actions hasClose="true" (closeClick)="onClose.emit()" [hasAdd]="true" (addClick)="selected=null;showEditor=true;" [hasFilterMode]="false" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
            </header>                        
            <p-dataTable #dt scrollable="true" scrollWidth="100%" [value]="schedules" [rows]="20" [paginator]="true" [(selection)]="selected">
                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                <p-column field="DayText" header="Day"></p-column>
                <p-column field="Time" header="Time (UTC)"></p-column>
                <p-column field="FullRefresh" header="Full Refresh?">
                    <ng-template let-data="rowData" pTemplate type="body">
                        <span>
                            <i *ngIf="data.ForceRefresh" class="fa fa-check enabled" title="True"></i>
                            <i *ngIf="!data.ForceRefresh" class="fa fa-times disabled" title="False"></i>
                        </span>
                    </ng-template>
                </p-column> 
                <p-column [style]="{width:'40px'}">
                    <ng-template let-dimension="rowData" pTemplate type="body">
                        <div class="RowTools">
                            <a style="cursor:pointer;" (click)="selected=dimension;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                        </div>
                    </ng-template>
                </p-column>                            
                <p-column  [style]="{width:'40px'}">
                    <ng-template let-dimension="rowData" pTemplate type="body">
                        <div class="RowTools">                                
                            <a style="cursor:pointer;" (click)="selected=dimension;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                        </div>
                    </ng-template>
                </p-column>                          
            </p-dataTable>            
        </div>
        <d3s-fusion-schedule-editor *ngIf="showEditor" 
            [selection]="selected" 
            (saveClick)="saveSchedule($event)" 
            (closeClick)="closeEditor()">
        </d3s-fusion-schedule-editor>
        <d3s-delete-form *ngIf="showDelete"
                    [callback]="theDeleteCallback"
                    [itemId]="selected?.ID"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to delete the selected schedule?'"                                         
                    (onCancel)="showDelete=false;">
        </d3s-delete-form> 
    `,
    providers: [FusionService]
})

export class FusionScheduleComponent extends BaseComponent implements OnInit {
    @Input() fusionId: number;    
    @Input() fusionTypeId: number;
    @Output() onClose = new EventEmitter();

    showDelete: boolean = false;
    showEditor: boolean = false;
    theDeleteCallback: Function;
        
    schedules: FusionSchedule[];
    selected: FusionSchedule;

    constructor(private fusionService: FusionService,
            private messagesService: MessagesService
        )
    {
        super();
        this.theDeleteCallback = this.deleteScheduleItem.bind(this);        
    }
        
    ngOnInit() {
        this.load();
    }

    load(): void {
        this.isLoading = true;
        this.fusionService.getFusionConfigurationSchedules(this.fusionTypeId, this.fusionId)
            .then(data => {
                for (let item of data) {
                    item.DayText = FusionScheduleDay[item.Day];
                }
                this.schedules = data;
                this.isLoading = false;
            });
    }

    private closeEditor(): void {
        this.showEditor = false;
    }

    private saveSchedule(event): void {
        event.schedule.FusionID = this.fusionId;
        this.fusionService.saveFusionConfigurationSchedule(event.schedule)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.load();
                this.showEditor = false;
            });
    }

    private deleteScheduleItem(id: number): void {
        this.fusionService.deleteFusionConfigurationSchedule(id)
            .then(result => {
                this.showMessageForResult(this.messagesService, result);
                this.showDelete = false;
                if (result.type != 'error') {
                    this.schedules = this.schedules.filter(x => x.ID != id);
                }
            });
    }
}