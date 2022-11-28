import { Component, Input, OnChanges, SimpleChanges, OnInit } from "@angular/core";
import { BaseComponent } from "../../shared/base.component";
import { WorkflowStepFieldChangeDetail } from "../../../models/workflow.model";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: 'd3s-workflow-monitor-step-field-change-details',
    templateUrl: 'workflowmonitor-step-field-change-details.component.html',
})
export class WorkflowMonitorStepFieldChangeDetailsComponent extends BaseComponent {
    @Input() fieldChanges: any;

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    getHtmlFieldValue(item: any) {
        if (typeof item.Value == 'undefined')
            {return '';}
        return item.Value;
    }

    getUrl(val: string): string {
        if (typeof val !== "undefined") {
            var url = val.split("|");
            return url[1];
        }
        return "";
    }

    getName(val: string): string {
        if (typeof val !== "undefined") {
            var name = val.split("|");
            return name[0];
        }
        return "";
    }

    getFieldName(item: WorkflowStepFieldChangeDetail): string {
        if (item.ObjectType != '' && item.ObjectType != 'Issue')
            {return $localize`Asset Field` + '::' + item.FieldName;}
        return $localize`Action Field` + '::' + item.FieldName;
    }

}