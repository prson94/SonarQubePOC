import { Component, NgZone, OnDestroy } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { StateService } from '../../../services/state.service';
import { MessagesService } from '../../../services/messages.service';
import { MapsService } from '../../../services/maps.service';
import { AdminBaseComponent } from '../admin-base.component'
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { FormMode } from '../../../models/form.model';
import { MapType } from '../../../models/map.model';

@Component({
    selector: 'd3s-admin-maps',
    providers: [MapsService],
    templateUrl: './admin-maps.component.html',
})

export class AdminMapsComponent extends AdminBaseComponent implements OnDestroy {
    mapType: MapType;
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    constructor(
        private mapsService: MapsService,
        private stateService: StateService,
        rightSidebarService: RightSidebarService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        protected messagesService: MessagesService,
        private router: Router) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Maps";
        this.setCommonItems();
        this.setObjectInfo('MapType', -1);

    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    add() {
        this.mapType = null;
        this.formMode = FormMode.Adding;
    }

    edit(mapType: MapType) {
        this.mapType = mapType;
        this.formMode = FormMode.Editing;
    }

    delete(mapType: MapType) {
        this.mapType = mapType;
        this.formMode = FormMode.Deleting;
    }

    deleteMapType() {
        if (this.mapType == null)
            return;
        this.isLoading = true;
        this.mapsService.deleteMapType(this.mapType.ID)
            .then(r => {
                this.showMessageForResult(this.messagesService, r);
                this.isLoading = false;
                this.formMode = FormMode.Default;
            })
    }

}


