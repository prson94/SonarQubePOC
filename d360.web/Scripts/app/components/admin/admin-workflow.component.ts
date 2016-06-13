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
import {DataTable, Column, Growl } from 'primeng/primeng';

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


    selectRow(id: number): void {
        this.selectedRow = this.workflowItems[this.workflowItems.findIndex(d => d.ID == id)];
    }


    editRow(id: number): void {
        var row = this.workflowItems.find(w => w.ID == id);

        this.workflowItems.forEach(w => {
            w.isDeleting = false;
            if (w.ID == id)
                w.isEditing = true;
            else
                w.isEditing = false;
        });

    }

    deleteRow(id: number): void {
        var row = this.workflowItems.find(w => w.ID == id);

        this.workflowItems.forEach(w => {
            w.isEditing = false;
            if (w.ID == id)
                w.isDeleting = true;
            else
                w.isDeleting = false;
        });
    }

    confirmDeleteRow(id: number): void {
        this.messages.push({ severity: 'info', summary: 'Workflow allocation deleted successfully', detail: '' });
        this.load();
    }

    updateRow(event: any): void {
        console.log(event);
        var message = event.message;
        var item = event.item;
        var initialItem = event.initialItem;

        if (message.isSuccess) {
            this.messages.push({ severity: 'info', summary: 'Workflow allocation updated', detail: '' });
            item.isEditing = false;
        } else {
            this.messages.push({ severity: 'error', summary: 'An error occurred while updating the workflow allocation', detail: '' });
        }
            
    }


}

class WorkflowRowItem extends WorkflowItem {
    isEditing = false;
    isDeleting = false;
}