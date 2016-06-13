///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader } from '../../services/page-header.service';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { FieldsGridTile } from '../tiles/fields-grid.tile';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { WorkflowItem } from '../../models/workflow.model';
import { WorkflowItemForm } from '../forms/workflow-item.form';
import { DeleteForm } from '../forms/delete.form';
import { DataTable, Column, Growl } from 'primeng/primeng';

@Component({
    selector: 'admin-workflow',
    viewProviders: [HTTP_PROVIDERS],
    directives: [ObjectDetailTile, WorkflowItemForm, DeleteForm, DataTable, Column, Growl ],
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
    messages = new Array<any>();

    isEditing = false;
    isDeleting = false;

    workflowItems = new Array<WorkflowItem>();
    selectedRow = new WorkflowItem();
    lastSelectedRow = null;

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


    selectRow(): void {
        if (this.lastSelectedRow != this.selectedRow) {
            this.isEditing = false;
            this.isDeleting = false;
        }
        this.lastSelectedRow = this.selectedRow
    }

    deleteRow(id: number): void {
        this.messages.push({ severity: 'info', summary: 'Workflow allocation deleted successfully', detail: '' });
        this.load();
    }
}