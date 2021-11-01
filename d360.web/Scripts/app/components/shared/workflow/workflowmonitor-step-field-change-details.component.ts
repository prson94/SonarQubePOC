import { Component, Input, OnChanges, SimpleChanges, OnInit } from "@angular/core";
import { BaseComponent } from "../../shared/base.component";
import { WorkflowStepFieldChangeDetail } from "../../../models/workflow.model";
import { CompanySettingsService } from "../../../services/settings.service";

 
@Component({
    selector: 'd3s-workflow-monitor-step-field-change-details',
    template:
        `
                <div class="row" >                    
                    <div class="col s12">                
                        <p-table #dt [value]="fieldChanges" selectionMode="single" [metaKeySelection]="true" [pageLinks]="3" [paginator]="true" [rows]="10">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th [pSortableColumn]="'FieldName'">
                                        Name
                                        <d3s-sortIcon [field]="'FieldName'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'Value'">
                                        Value
                                        <d3s-sortIcon [field]="'Value'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'FormValue'">
                                        Form
                                        <d3s-sortIcon [field]="'FormValue'"></d3s-sortIcon>
                                    </th>
                                   <th [pSortableColumn]="'OutputValue'">
                                        Output
                                        <d3s-sortIcon [field]="'OutputValue'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'AppendValue'">
                                        Appended
                                        <d3s-sortIcon [field]="'AppendValue'"></d3s-sortIcon>
                                    </th>
                                    <th [pSortableColumn]="'ClearValue'">
                                        Cleared
                                        <d3s-sortIcon [field]="'ClearValue'"></d3s-sortIcon>
                                    </th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr [pSelectableRow]="item">
                                    <td>{{getFieldName(item)}}</td>
                                    <td>
                                         <div [ngSwitch]="item.Type" style="overflow-x: auto">
	                                        <span *ngSwitchCase="'Html'" style="display:block; word-wrap:break-word !important" [innerHtml]="getHtmlFieldValue(item)"></span>
                                            <span *ngSwitchCase="'Link'" style="display:block; word-wrap:break-word !important"><a href="{{getUrl(item.Value)}}" target="_blank" style="font-weight:bold">{{getName(item.Value)}}</a></span>
	                                        <span *ngSwitchDefault style="display:block; word-wrap:break-word !important" >{{item.Value}}</span>
                                        </div>
                                    </td>
                                    <td>{{item.FormValue}}</td>
                                    <td>{{item.UseOutputValue}}</td>
                                    <td>{{item.AppendValue}}</td>
                                    <td>{{item.ClearValue}}</td>
                                </tr>
                            </ng-template>
                            <ng-template *ngIf="dt.totalRecords" pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>                         
                    </div>
                </div>               
`,

})
export class WorkflowMonitorStepFieldChangeDetailsComponent extends BaseComponent {
    @Input() fieldChanges: any;

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    getHtmlFieldValue(item: any) {
        if (typeof item.Value == 'undefined')
            return '';
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
            return 'Asset Field::' + item.FieldName;
        return 'Action Field::' + item.FieldName;
    }

}