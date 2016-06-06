///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader } from '../../services/page-header.service';
import { ObjectDetailTile } from '../../tiles/object-detail.tile';
import { FieldsGridTile } from '../../tiles/fields-grid.tile';
import { PeopleResponsibilitiesTile } from '../../tiles/people-responsibilities.tile';
import { DataTable, DataTableDirectives } from 'angular2-datatable/datatable';
import { WorkflowItem } from '../../models/workflow.model';
import { WorkflowItemForm } from '../../forms/workflow-item.form';

@Component({
    selector: 'admin-workflow',
    viewProviders: [HTTP_PROVIDERS],
    directives: [ObjectDetailTile, DataTableDirectives, WorkflowItemForm],
    templateUrl: 'scripts/app/components/admin/admin-workflow.component.html',
    styles: [`
        .selected {
        background-color: #86ccf9;        
        }
        tbody tr:not(.selected):not(.inline-edit):hover {
        background-color: #ddd;
        }
        td {
            padding-left:3px;
        }
    `]
})

export class AdminWorkflowComponent {
    http: Http;
    pageHeader: PageHeader;
    isLoading = false;

    workflowItems = new Array<WorkflowRowItem>();
    selectedRow = new WorkflowRowItem();

    constructor(http: Http, pageHeader: PageHeader) {
        this.http = http;
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Workflow';
        this.pageHeader.description = 'Manage all workflow settings for types within your environment.';

        this.load();
    }

    load() {

        this.isLoading = true;
        this.http.get('/api/workflows/relations')
            .map(data => data.json())
            .subscribe(data => {
                this.workflowItems = data;
                //console.log(this.workflowItems);
                this.selectedRow = this.workflowItems[0];
                this.isLoading = false;
            });

    }


    selectRow(id: string): void {
        this.selectedRow = this.workflowItems[this.workflowItems.findIndex(d => d.ID == id)];
    }

    editRow(id: string): void {

        var row = this.workflowItems.find(w => w.ID == id);

        if (row && row.isEditing) {
            row.isEditing = false;
            return;
        }

        this.workflowItems.forEach(w => {
            if (w.ID == id)
                w.isEditing = true;
            else
                w.isEditing = false;
        });
    }

}

class WorkflowRowItem extends WorkflowItem {
    isEditing = false;
}