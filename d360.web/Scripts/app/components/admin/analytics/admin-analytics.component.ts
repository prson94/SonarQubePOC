import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { MessagesService } from '../../../services/messages.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-analytics-component',
    template: ` <div class="row">
                    <div class="col s7">
                        <div class="tile tile-detail">
                            <d3s-admin-metric-group-list (selectionChange)="selection = $event"></d3s-admin-metric-group-list>
                        </div>
                    </div>
                    <div class="col s5">
                        <div class="row" *ngIf="selection != null">
                            <div class="col s12">
                                <div class="tile tile-detail">  
                                    <d3s-admin-metric-map-list [groupId]="selection?.ID" [groupName]="selection?.Name"></d3s-admin-metric-map-list>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col s12">
                                <div class="tile tile-detail">  
                                    <d3s-admin-metric-item-list></d3s-admin-metric-item-list>
                                </div>
                            </div>
                        </div> 
                    </div>
                <div>
 
                `
})

export class AdminAnalyticsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    private selection = null;

    constructor(
        rightSidebarService: RightSidebarService,
        protected messagesService: MessagesService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Scoring";
        this.setCommonItems();
        this.setCommonRightSideBar(false);

        
    }

    ngOnInit() {
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

}