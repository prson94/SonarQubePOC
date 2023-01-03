import { Component, EventEmitter, Input, Output } from "@angular/core";
import { CompanySettingsService } from "../../services/settings.service";
import { BaseComponent } from "../shared/base.component";

@Component({
    selector: `d3s-monitor-workflow-version`,
    templateUrl: './monitor-workflow-version.component.html'
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
    title: string = $localize`Workflow Versions`;
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