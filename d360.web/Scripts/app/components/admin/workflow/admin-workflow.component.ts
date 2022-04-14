import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';

import { WorkflowDiagramModel } from '../../../models/workflow.model';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';


@Component({
    selector: 'admin-workflow',
    providers: [],
    templateUrl: './admin-workflow.component.html'
})

export class AdminWorkflowComponent extends AdminBaseComponent implements OnInit {
    private mode: PageMode = PageMode.Default;
    PageMode = PageMode;
    private currentID: number = 1;
    private currentUid: string = "00000000-0000-0000-0000-000000000000";
    private newWorkflowID: number = 0;
    private workflow: WorkflowDiagramModel;
    private cloneWorkflow:boolean=false;

    constructor(
        secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
    }

    ngOnInit() {
        this.areaName = StringConstants.Section_Workflows;
        this.clearSidebar();
        this.titleService.setTitle($localize`Workflows`);
        this.setCommonItems();
    }

    viewReadOnlyDiagram(e: any) {
        this.currentUid = e;
        this.workflow = null;
        this.mode = PageMode.ReadOnlyDiagram;
    }

    closeEditor() {
        this.mode = PageMode.Default;
    }

    add() {
        this.currentID = 0;
        this.currentUid = "00000000-0000-0000-0000-000000000000";
        this.workflow = new WorkflowDiagramModel();
        this.mode = PageMode.Editor;
    }

    save(e: WorkflowDiagramModel) {
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