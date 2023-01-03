import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { State } from '../../models/asset.model';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-monitor-filter',
    templateUrl: './monitor-filter.component.html',
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
            .subscribe((r) => {
                this.items = r;

                this.items.forEach((i) => {
                    i.label = i.State === State.InActive ? i.Name + " ( " + $localize`Inactive` + " )" : i.Name;
                    i.value = i.ID.toString();
                });


                if (this.selectAll) {
                    this.selection = [];
                    this.items.forEach((i) => this.selection.push(i.value));
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