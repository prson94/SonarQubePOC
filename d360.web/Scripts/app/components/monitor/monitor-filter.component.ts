import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowListItem } from '../../models/workflow.model';


@Component({
    selector: 'd3s-monitor-filter',
    template: ` 


<div>
    <d3s-loading *ngIf="isLoading" isLoading="true"></d3s-loading>
    <div *ngIf="!isLoading">
        <header>Workflow Versions</header>
        <p-multiSelect [options]="items" [style]="{'width':'98%'}" [ngModel]="selection" (ngModelChange)="change($event)"></p-multiSelect>
    </div>
</div>


              `,
    providers: [WorkflowService],
})

export class MonitorFilterComponent extends BaseComponent implements OnInit {
    @Input() selectAll: boolean = false;
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

                this.selection = [];
                if (this.selectAll)
                    this.items.forEach(i => this.selection.push(i.value));
                    
                this.selectionChange.emit(this.selection);
                this.isLoading = false;
            });
    }

    change(e: any) {
        this.selection = e;
        this.selectionChange.emit(e);
    }
}