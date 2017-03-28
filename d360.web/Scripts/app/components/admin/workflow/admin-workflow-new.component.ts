import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';

import { WorkflowDiagramModel } from '../../../models/workflow.model';


@Component({
    selector: 'admin-workflow-new',
    providers: [],
    templateUrl: './admin-workflow-new.component.html'
})

export class AdminWorkflowNewComponent extends AdminBaseComponent implements OnInit {
    private mode: PageMode = PageMode.Default;
    PageMode = PageMode;
    private currentID: number = 1;
    private newWorkflowID: number = 0;
    private workflow: WorkflowDiagramModel;

    constructor(rightSidebarService: RightSidebarService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.clearSidebar();
        this.headerBreadcrumbService.clearBreadcrumbs();
        let b = new Breadcrumb();
        b.text = 'Workflow';
        this.headerBreadcrumbService.showBreadcrumb(b);
        this.titleService.setTitle("Workflow");

    }

    ngOnInit() {
        this.areaName = "Workflow";

        //this.load();
    }

    load() {
        //this.isLoading = true;

    }

    viewReadOnlyDiagram(e: any) {
        this.currentID = e;
        this.mode = PageMode.ReadOnlyDiagram;
    }

    closeEditor() {
        this.mode = PageMode.Default;
    }

    add() {
        this.mode = PageMode.Editor;
    }

    save(e: WorkflowDiagramModel) {
        this.workflow = e;
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