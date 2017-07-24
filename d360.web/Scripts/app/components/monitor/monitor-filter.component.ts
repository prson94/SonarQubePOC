import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowListItem } from '../../models/workflow.model';


@Component({
    selector: 'd3s-monitor-filter',
    template: ` 
<d3s-loading [isLoading]="isLoading"></d3s-loading>
<div *ngIf="!isLoading">
    <div class="tile tile-detail" style="padding-bottom: 15px">
        <header>Choose Workflow Types</header>
        <p-multiSelect [options]="items" [style]="{'width':'98%'}" [ngModel]="selection" (ngModelChange)="change($event)"></p-multiSelect>
    </div>
</div>

              `,
    providers: [WorkflowService],
})

export class MonitorFilterComponent extends BaseComponent implements OnInit {
    @Input() selection: any[];
    @Output() selectionChange = new EventEmitter();
    items: any[];

    constructor(protected workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getTypes()
            .then(r => {
                this.items = r;

                this.items.forEach(i => {
                    i.label = i.Name;
                    i.value = i.ID.toString();
                });
                this.isLoading = false;
            });
    }

    change(e: any) {
        this.selection = e;
        this.selectionChange.emit(e);

        //console.log(this.selection);
    }
}