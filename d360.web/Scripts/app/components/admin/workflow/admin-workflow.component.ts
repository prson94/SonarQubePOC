import { Component, NgZone, OnDestroy } from '@angular/core';
import { HeaderBreadcrumbService, WorkflowService, RightSidebarService } from '../../../services/index';
import { WorkflowItem, WorkflowType } from '../../../models/workflow.model';
import { MenuItem } from 'primeng/primeng';
import { ActionBarItem } from '../../../models/action-bar.model';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { AdminBaseComponent} from '../admin-base.component';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'admin-workflow',
    providers: [WorkflowService],
    templateUrl: './admin-workflow.component.html'
}) 

export class AdminWorkflowComponent extends AdminBaseComponent  {
    messages = new Array<any>();

    isEditing = false;
    isDeleting = false;
    isAdding = false;

    private workflowItems : WorkflowItem[] = [];
    private selectedRow: WorkflowItem;
    private addingRow = new WorkflowItem();

    private addMenu: MenuItem[] = [];

    constructor(rightSidebarService : RightSidebarService, headerBreadcrumbService: HeaderBreadcrumbService, private workflowService: WorkflowService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        
        this.areaName = "Workflow";
        this.setCommonItems();

        let items: MenuItem[] = [];

        items.push({
            icon: null,
            label: 'Propose new artifact'
        });

        items.push({
            icon: null,
            label: 'Certify artifact'
        });

        items.push({
            icon: null,
            label: 'Work Issue'
        });

        items.push({
            icon: null,
            label: 'Challenge'
        });

        items.push({
            icon: null,
            label: 'Propose new Artifact (Multi-approval)'
        });

        this.addMenu.push({
            icon: 'fa fa-filter',
        });

        this.addMenu.push({
            icon: 'fa fa-plus',
            items: items
        });

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
    
    add(e: any): void {

        if (e.icon == 'fa fa-filter') {
            this.showSimpleFilter = !this.showSimpleFilter;
            return;
        }

        this.addingRow = new WorkflowItem();

        
        switch (e.label) {
            case 'Propose new artifact':
                this.addingRow.WorkflowType = WorkflowType.SuggestNewArtifact
                break;
            case 'Certify artifact':
                this.addingRow.WorkflowType = WorkflowType.CertifyArtifact
                break;
            case 'Work Issue':
                this.addingRow.WorkflowType = WorkflowType.WorkIssue
                break;
            case 'Challenge':
                this.addingRow.WorkflowType = WorkflowType.ChallengeArtifact
                break;
            case 'Propose new Artifact (Multi-approval)':
                this.addingRow.WorkflowType = WorkflowType.SuggestNewArtifactMulti
                break;
            default:
                console.warn(`workflow type defaulted to 'SuggestNewArtifact'. Supplied label was '${e.label}'`);
                this.addingRow.WorkflowType = WorkflowType.SuggestNewArtifact
                break;
        }

        this.isAdding = true;
        this.isEditing = false;
        this.isDeleting = false;
    }
    

    deleteRow(id: number): void {
        this.messages.push({ severity: 'info', summary: 'Workflow allocation deleted successfully', detail: '' });
        this.isDeleting = false;
        this.load();
    }

    editRow(workflow: WorkflowItem) {
        this.selectedRow = workflow;      
        this.isAdding = false;
        this.isDeleting = false;
        this.isEditing= true;
    }

    confirmEdit(e: any) {
        this.messages.push({ severity: 'info', summary: e.message, detail: '' });
        this.isEditing = false;
        this.load();
    }

    confirmAdd(e: any) {
        this.messages.push({ severity: 'info', summary: e.message, detail: '' });
        this.isAdding = false;
        this.load();
    }
    
}