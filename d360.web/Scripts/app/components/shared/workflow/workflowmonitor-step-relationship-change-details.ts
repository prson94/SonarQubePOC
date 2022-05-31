import { Component, Input } from "@angular/core";
import { BaseComponent } from "../../shared/base.component";
import { WorkflowStepRelationshipChangeDetail } from "../../../models/workflow.model";
import { CompanySettingsService } from "../../../services/settings.service";


@Component({
    selector: 'd3s-workflow-monitor-step-relationship-change-details',
    template: `
<div class="row" >  
    <div class="col s8">  
            <span class="FieldName">
                <ng-container i18n>Relationship Type Name</ng-container>:
            </span>
            <span>
                 {{relationshipChange.TypeName}}
            </span>
        </div>
    <div class="col s4">  
            <span class="FieldName">
                <ng-container i18n>Status</ng-container>: 
            </span>
            <span *ngIf="relationshipChange.ClearValue" i18n>Removed</span>
            <span *ngIf="relationshipChange.AppendValue" i18n>Added</span>
            <span *ngIf="!relationshipChange.ClearValue && !relationshipChange.AppendValue" i18n>Updated</span>
    </div>
</div>
<div class="row" >  
<div class="col s6">  
            <span class="FieldName">
                <ng-container i18n>Relationship</ng-container>:
            </span>
            <span>
                 {{relationshipChange.Relationship}}
            </span>
</div>
</div>
`
})
export class WorkflowMonitorStepRelationshipChangeDetailsComponent extends BaseComponent {
    @Input() relationshipChange: WorkflowStepRelationshipChangeDetail;
    constructor(
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }
}