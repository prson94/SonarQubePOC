import { Component, NgZone, OnDestroy } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { StateService } from '../../../services/state.service';
import { MessagesService } from '../../../services/messages.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';


@Component({
    selector: 'd3s-admin-maps',
    providers: [],
    templateUrl: './admin-maps.component.html',
})

export class AdminMapsComponent extends AdminBaseComponent implements OnDestroy {
    searchFilter: string = "";
    objectType: string = "MapType";

    constructor(private stateService: StateService,
        rightSidebarService: RightSidebarService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        protected messagesService: MessagesService,
        private router: Router) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Maps";
        this.setCommonItems();
        this.load();
        this.setObjectInfo('MapType', -1);

    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load() {
        this.isLoading = true;
    }

    navigate(item: any) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('MapType', item.ID));
    }
}


