
import { Component, NgZone, OnDestroy } from '@angular/core';
import { PageHeader, HeaderBreadcrumbService, WorkflowService, RightSidebarService } from '../../services/index';
import { WorkflowItem, WorkflowType } from '../../models/workflow.model';
import { MenuItem } from 'primeng/primeng';
import { ActionBarItem } from '../../models/action-bar.model';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { AdminBaseComponent} from './admin-base.component';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'admin-workflow',
    providers: [WorkflowService],
    templateUrl: 'scripts/app/components/admin/admin-workflow.component.html'
})

export class AdminWorkflowComponent extends AdminBaseComponent  {
    messages = new Array<any>();

    isEditing = false;
    isDeleting = false;
    isAdding = false;

    private workflowItems : WorkflowItem[] = [];
    private selectedRow: WorkflowItem;
    private addingRow = new WorkflowItem();

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
    
    add(): void {
        this.addingRow = new WorkflowItem();
        //TODO: replace with menu item list so user can choose workflowtype
        this.addingRow.WorkflowType = WorkflowType.CertifyArtifact
        this.isAdding = true;
    }
    

    deleteRow(id: number): void {
        this.messages.push({ severity: 'info', summary: 'Workflow allocation deleted successfully', detail: '' });
        this.load();
    }

    editRow(workflow: WorkflowItem) {
     //   this.selectedRow = workflow;
      //  console.log(this.selectedRow);
        this.isEditing= true;
    }
    
}