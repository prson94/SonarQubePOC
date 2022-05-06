import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { State } from '../../models/asset.model';
import { CompanySettingsService } from '../../services/settings.service';
import '@angular/localize/init';


@Component({
    selector: 'd3s-monitor-filter',
    template: ` 
        <div class="row">
            <div class="col s3 FieldName" i18n>Workflow Types</div>
            <div class="col s9">
                <d3s-loading *ngIf="isLoading" isLoading="true"></d3s-loading>
                <div *ngIf="!isLoading">
                    <table style="table-layout: fixed">
                        <tbody>
                            <tr>
                                <td>
                                    <p-multiSelect [options]="items" [style]="{'width':'98%'}" [ngModel]="selection" (ngModelChange)="change($event)" selectedItemsLabel="{0} items selected"></p-multiSelect>
                                </td>
                                <td *ngIf="showFilter" style="width:32px">
                                    <a style="font-size:1.1em" [style.color]="filterMode ? '#000' : '#f00'" (click)="filterModeChange.emit(!filterMode)"><i class="fa fa-filter"></i></a>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>        
            </div>
        </div>   
           `,
    providers: [WorkflowService],
})

export class MonitorFilterComponent extends BaseComponent implements OnInit {
    @Input() selectAll: boolean = false;
    @Input() showFilter: boolean = false;
    @Input() filterMode: boolean = false;
    @Output() filterModeChange = new EventEmitter();
    @Input() selection: any[];
    @Output() selectionChange = new EventEmitter();
    @Output() filterClick = new EventEmitter();
    items: any[];

    constructor(
        protected settingsService: CompanySettingsService,
        protected workflowService: WorkflowService) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getTypes()
            .subscribe(r => {
                this.items = r;

                this.items.forEach(i => {
                    i.label = i.State == State.InActive ? i.Name + " ( " + $localize`Inactive` + " )" : i.Name;
                    i.value = i.ID.toString();
                });


                if (this.selectAll) {
                    this.selection = [];
                    this.items.forEach(i => this.selection.push(i.value));
                }

                this.selectionChange.emit(this.selection);
                this.isLoading = false;
            });
    }

    change(e: any) {
        this.selection = e;
        this.selectionChange.emit(e);
    }
}