import { Component, OnDestroy } from '@angular/core';
import { AdminBaseComponent } from '../admin-base.component'
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { MessagesService } from '../../../services/messages.service';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-export-templates-component',
    template: ` <div class="row">
                    <div class="col l3 m5 s12">
                        <div class="tile tile-detail">
                            list of templates
                        </div>
                    </div>
                    <div class="col l9 m7 s12">
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">  
                                    selected template details
                                </div>
                            </div>
                        </div>
                    </div>
                <div>
                `,
    providers: [],       
})

export class AdminExportTemplatesComponent extends AdminBaseComponent implements OnDestroy {
    constructor(
            rightSidebarService: RightSidebarService,
            headerBreadcrumbService: HeaderBreadcrumbService,        
            titleService: Title,
            protected messagesService: MessagesService,
        ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Artifacts";
        this.setCommonItems();
        //this.load();
        
        this.setCommonRightSideBar(false);        
    }


    ngOnDestroy() {
        this.clearSidebar();
    }
}