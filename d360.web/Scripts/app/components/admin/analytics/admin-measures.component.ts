import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { MessagesService } from '../../../services/messages.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../../models/rightsidebar.model';

@Component({
    selector: 'd3s-admin-measures-component',
    template: ` <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">  
                            <d3s-admin-metric-item-list></d3s-admin-metric-item-list>
                        </div>
                    </div>
                </div> 
                `
})

export class AdminMeasuresComponent extends AdminBaseComponent implements OnInit, OnDestroy {

    constructor(
        rightSidebarService: RightSidebarService,
        protected messagesService: MessagesService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Measures";


    }

    ngOnInit() {
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

}