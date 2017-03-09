import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'admin-workflow-new',
    providers: [],
    templateUrl: './admin-workflow-new.component.html'
})

export class AdminWorkflowNewComponent extends AdminBaseComponent implements OnInit {

    constructor(rightSidebarService: RightSidebarService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
    }

    ngOnInit() {
        this.areaName = "New Workflow";
        //this.setCommonItems();

        //this.load();
    }

    load() {
        this.isLoading = true;

    }
}