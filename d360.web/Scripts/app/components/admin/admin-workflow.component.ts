///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { FieldsGridTile } from '../tiles/fields-grid.tile';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { WorkflowItem, WorkflowType } from '../../models/workflow.model';
import { WorkflowItemForm } from '../forms/workflow-item.form';
import { DeleteForm } from '../forms/delete.form';
import { DataTable, Column, Growl, MenuItem } from 'primeng/primeng';
import { ActionBar } from '../parts/action-bar.part';
import { ActionBarItem } from '../../models/action-bar.model';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { WorkflowService } from '../../services/workflow.service';


@Component({
    selector: 'admin-workflow',
    providers: [WorkflowService],
    directives: [ObjectDetailTile, WorkflowItemForm, DeleteForm, DataTable, Column, Growl, ActionBar ],
    templateUrl: 'scripts/app/components/admin/admin-workflow.component.html'
})

export class AdminWorkflowComponent {
    isLoading = false;
    messages = new Array<any>();

    isEditing = false;
    isDeleting = false;
    isAdding = false;

    workflowItems = new Array<WorkflowItem>();
    selectedRow = new WorkflowItem();
    addingRow = new WorkflowItem();

    actions = new Array<ActionBarItem>();

    constructor(private pageHeader: PageHeader, private headerBreadcrumbService: HeaderBreadcrumbService, private workflowService: WorkflowService ) {
        this.workflowService = workflowService;
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Workflow';
        this.pageHeader.description = 'Manage all workflow settings for types within your environment.';

        headerBreadcrumbService.clearBreadcrumbs();
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Administration", ""));
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Workflow", ""));

        this.actions.push({
            icon: 'fa-plus',
            tooltip: 'Add a workflow allocation',
            action: null,
            menuItems: null
        });

        this.actions[0].menuItems = new Array<MenuItem>();
        
        this.actions[0].menuItems.push({ label: 'Propose new artifact', icon: '' });
        this.actions[0].menuItems.push({ label: 'Certify artifact', icon: '' });
        this.actions[0].menuItems.push({ label: 'Work Issue', icon: '' });
        this.actions[0].menuItems.push({ label: 'Challenge', icon: '' });

        this.load();
    }

    load() {
        this.isLoading = true;

        this.workflowService.getWorkflows().then(p => {
            this.workflowItems = p;
            this.isLoading = false;
        });
    }

    delete(id: number): void {
        this.selectedRow = this.workflowItems.find(w => w.ID == id);
        this.isDeleting = true;
    }

    edit(id: number): void {
        this.selectedRow = this.workflowItems.find(w => w.ID == id);
        this.isEditing = true;
    }

    add(): void {
        this.addingRow = new WorkflowItem();
        //TODO: replace with menu item list so user can choose workflowtype
        this.addingRow.WorkflowType = WorkflowType.CertifyArtifact
        this.isAdding = true;
    }

    select(): void {
            this.isAdding = false;
    }

    deleteRow(id: number): void {
        this.messages.push({ severity: 'info', summary: 'Workflow allocation deleted successfully', detail: '' });
        this.load();
    }
}