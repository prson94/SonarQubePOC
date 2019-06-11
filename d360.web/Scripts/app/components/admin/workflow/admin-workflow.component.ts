import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';

import { WorkflowDiagramModel } from '../../../models/workflow.model';


@Component({
    selector: 'admin-workflow',
    providers: [],
    templateUrl: './admin-workflow.component.html'
})

export class AdminWorkflowComponent extends AdminBaseComponent implements OnInit {
    private mode: PageMode = PageMode.Default;
    PageMode = PageMode;
    private currentID: number = 1;
    private newWorkflowID: number = 0;
    private workflow: WorkflowDiagramModel;
    private cloneWorkflow:boolean=false;

    constructor(rightSidebarService: RightSidebarService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
    }

    ngOnInit() {
        this.areaName = "Workflow";
        this.adminHeading = "Workflow";
        this.clearSidebar();
        this.titleService.setTitle('Workflow');
        this.setCommonItems();
    }

    viewReadOnlyDiagram(e: any) {
        this.currentID = e;
        this.workflow = null;
        this.mode = PageMode.ReadOnlyDiagram;
    }

    closeEditor() {
        this.mode = PageMode.Default;
    }

    add() {
        this.currentID = 0;
        this.workflow = new WorkflowDiagramModel();
        this.mode = PageMode.Editor;
    }

    save(e: WorkflowDiagramModel) {
      //  console.log('base save', e);
        this.workflow = e;
        this.currentID = this.workflow.Type.ID;
        this.mode = PageMode.DiagramEditor;
    }
}


export enum PageMode {
    Default,
    ReadOnlyDiagram,
    Editor,
    DiagramEditor,
    Delete,
}