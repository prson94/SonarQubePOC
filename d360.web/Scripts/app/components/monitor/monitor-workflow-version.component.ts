import { Component, OnInit, EventEmitter, Output, Input } from "@angular/core";
import { CompanySettingsService } from "../../services/settings.service";
import { BaseComponent } from "../shared/base.component";


@Component({
    selector: `d3s-monitor-workflow-version`,
    template: `
    <div style="padding-bottom: 15px">
    <d3s-loading *ngIf="isLoading" isLoading="true"></d3s-loading>
        <div *ngIf="!isLoading">    
        <header *ngIf="showHeader">   
                Workflow Versions
                 <d3s-tile-actions [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>
         </header>
            <d3s-monitor-filter *ngIf="showHeader" [hidden]="isFiltered" (selectionChange)="filterChange($event)" [selectAll]="selectAll"></d3s-monitor-filter>
            <d3s-monitor-filter *ngIf="!showHeader" [(filterMode)]="showSimpleFilter" [selection]="selectedWorkflowTypes"  [showFilter]="true" [hidden]="isFiltered" (selectionChange)="filterChange($event)" [selectAll]="selectAll"></d3s-monitor-filter>

            <d3s-monitor-list 
                    [showSimpleFilter]="showSimpleFilter"
                    [workflowTypes]="selectedWorkflowTypes" 
                    (selectionChange)="monitorListChange($event)" 
                    [objectType]="objectType" 
                    [objectId]="objectId" 
                    (filteredTypes)="monitorFilterTypesChange($event)"
                    (onLoadComplete)="onMonitorListLoadCompleted.emit($event)">
            </d3s-monitor-list>
        </div>
   </div>

`
})
export class MonitorWorkflowVersionComponent extends BaseComponent {

    @Output() onFilterChanged = new EventEmitter();
    @Output() onMonitorListChanged = new EventEmitter();
    @Output() onMonitorFilterTypesChanged = new EventEmitter();
    @Output() onMonitorListLoadCompleted = new EventEmitter();

    @Input() objectType: string;
    @Input() objectId: number;
    @Input() selectAll: boolean = true;
    @Input() showHeader: boolean = true;

    @Input() selectedWorkflowTypes: any[];
    selectedWorkflowType: any = null;
    showSimpleFilter: boolean = true;


    isFiltered: boolean = false;
    filteredTypes: any[];
    expandRow: boolean = false;

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    filterChange($event) {
        this.selectedWorkflowTypes = $event;
        this.onFilterChanged.emit($event);
    }

    monitorListChange($event) {
        this.selectedWorkflowType = $event;
        this.onMonitorListChanged.emit($event);
    }

    monitorFilterTypesChange($event) {
        this.filteredTypes = $event;
        this.onMonitorFilterTypesChanged.emit($event);
    }

}