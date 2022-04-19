import { Component, Input } from "@angular/core";
import { CompanySettingsService } from "../../../services/settings.service";
import { BaseComponent } from "../../shared/base.component";

@Component({
    selector: 'd3s-workflow-monitor-step-http-details',
    template:
        `
                <div class="row">                    
                    <div class="col s12">                
                        <div>
                            <span class="FieldName">
                               <ng-container i18n>Url</ng-container>:
                            </span>
                            <span>
                                {{step?.Settings?.HTTPRequest?.Url}}
                            </span>
                        </div>
                    </div>
                </div>   
                <div class="row">
                    <div class="col s6">                
                        <div>
                            <span class="FieldName">
                                <ng-container i18n>Method</ng-container>:
                            </span>
                            <span>
                                {{step?.Settings?.HTTPRequest?.Method}}
                            </span>
                        </div>
                    </div>
                    <div class="col s6">                
                        <div>
                            <span class="FieldName">
                                <ng-container i18n>Request Timeout</ng-container>:
                            </span>
                            <span>
                                {{step?.Settings?.HTTPRequest?.Timeout}}
                            </span>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12" *ngIf="step?.ItemFields?.HTTPResponse?.StatusCode">                
                        <div>
                            <span class="FieldName">
                                <ng-container i18n>Response Status Code</ng-container>:
                            </span>
                            <span>
                                {{step?.ItemFields?.HTTPResponse?.StatusCode}}
                            </span>
                        </div>
                    </div>
                </div>
`,

})
export class WorkflowMonitorStepHttpDetailsComponent extends BaseComponent {
    @Input() step: any;
    constructor(
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }
}