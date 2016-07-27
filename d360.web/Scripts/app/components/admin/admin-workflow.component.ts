///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone, OnDestroy } from '@angular/core';
import { PageHeader, HeaderBreadcrumbService, WorkflowService, RightSidebarService } from '../../services/index';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { WorkflowItem, WorkflowType } from '../../models/workflow.model';
import { WorkflowItemForm } from '../forms/workflow-item.form';
import { DeleteForm } from '../forms/delete.form';
import { DataTable, Column, Growl, MenuItem } from 'primeng/primeng';
import { ActionBar } from '../parts/action-bar.part';
import { ActionBarItem } from '../../models/action-bar.model';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { AdminBaseComponent} from './admin-base.component';
import { Title } from '@angular/platform-browser';
import { TileActionsComponent } from '../tiles/tile-actions.component';

@Component({
    selector: 'admin-workflow',
    providers: [WorkflowService],
    directives: [ObjectDetailTile, WorkflowItemForm, DeleteForm, DataTable, Column, Growl, ActionBar, TileActionsComponent ],
    templateUrl: 'scripts/app/components/admin/admin-workflow.component.html'
})

export class AdminWorkflowComponent extends AdminBaseComponent  {
    messages = new Array<any>();

    isEditing = false;
    isDeleting = false;
    isAdding = false;

    workflowItems = new Array<WorkflowItem>();
    selectedRow = new WorkflowItem();
    addingRow = new WorkflowItem();

    actions = new Array<ActionBarItem>();

    constructor(rightSidebarService : RightSidebarService, pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService, private workflowService: WorkflowService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService, rightSidebarService);
        this.areaDescription = 'Manage all workflow settings for types within your environment.';
        this.areaName = "Workflow";
        this.setCommonItems();
        
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
            this.selectedRow = this.workflowItems[0];
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