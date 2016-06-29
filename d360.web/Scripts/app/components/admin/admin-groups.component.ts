///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import {AdminBaseComponent} from './admin-base.component';

@Component({
    selector: 'admin-groups',
    templateUrl: 'scripts/app/components/admin/admin-groups.component.html'
})

export class AdminGroupsComponent extends AdminBaseComponent {

    constructor(pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService) {
        super(headerBreadcrumbService, pageHeader);
        this.areaDescription = "Here you will find groups and membership.";
        this.areaName = "Groups";
        this.setCommonItems();
    }
}