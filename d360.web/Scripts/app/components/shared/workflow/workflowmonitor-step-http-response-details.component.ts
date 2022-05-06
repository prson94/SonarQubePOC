import { Component, Input, OnInit } from "@angular/core";
import { CompanySettingsService } from "../../../services/settings.service";
import { BaseComponent } from "../../shared/base.component";

@Component({
    selector: "d3s-workflow-monitor-step-http-response-details",
    template:
        `
                <div class="row">
                    <div class="col s12">  
                        <div class="FieldName" i18n>Outputs</div>
                        <div>
                            <p-table #dt [value]="outputs" selectionMode="single" [metaKeySelection]="true" [pageLinks]="3" [paginator]="true" [rows]="10">
                                <ng-template pTemplate="header">
                                    <tr>
                                        <th [pSortableColumn]="'FieldName'">
                                            <ng-container i18n>Name</ng-container>
                                            <d3s-sortIcon [field]="'FieldName'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'Value'">
                                            <ng-container i18n>Path</ng-container>
                                            <d3s-sortIcon [field]="'Value'"></d3s-sortIcon>
                                        </th>
                                        <th [pSortableColumn]="'FormValue'">
                                            <ng-container i18n>Value</ng-container>
                                            <d3s-sortIcon [field]="'FormValue'"></d3s-sortIcon>
                                        </th>
                                    </tr>
                                </ng-template>
                                <ng-template pTemplate="body" let-item>
                                    <tr [pSelectableRow]="item">
                                        <td>{{item.Name}}</td>
                                        <td>{{item.Path}}</td>
                                        <td>{{item.Value}}</td>
                                    </tr>
                                </ng-template>
                                <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                    <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                                </ng-template>
                            </p-table>   
                        </div>
                    </div>
                </div>
`,

})
export class WorkflowMonitorStepHttpResponseDetailsComponent extends BaseComponent implements OnInit {
    @Input() step: any;

    outputs: any[] = [];

    constructor(
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    ngOnInit() {
        if (this.step != null && this.step.ItemFields != null && this.step.ItemSettings != null) {
            let stepFieldOutputs = this.step.ItemFields.Outputs.Output;
            let stepSettingOutputs = this.step.ItemSettings.HTTPResponse.Outputs.Output;

            if (stepFieldOutputs != null && stepFieldOutputs.length == null) {
                stepFieldOutputs = [stepFieldOutputs];
            }

            if (stepSettingOutputs != null) {
                if (stepSettingOutputs.length == null) {
                    stepSettingOutputs = [stepSettingOutputs];
                }

                stepSettingOutputs.forEach((o) => {
                    let field = stepFieldOutputs.find((f) => f.Id === o.Id);
                    if (field == null) {
                        field = {};
                    }
                    this.outputs.push({
                        Id: o.Id,
                        Name: o.Name,
                        Path: o.Path,
                        Value: field.Value
                    });
                });
            }
        }
    }
}