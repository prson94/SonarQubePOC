import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { GroupService } from '../../services/group.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { GroupSearchResultModel } from '../../models/group.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { CompanySettingsService } from '../../services/settings.service';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-group-list',
    providers: [GroupService],
    templateUrl: 'group-list.component.html'
})

export class GroupListComponent extends BaseComponent implements OnInit {

    private groups: GroupSearchResultModel[] = [];
    private selected: GroupSearchResultModel;

    constructor(
        private groupService: GroupService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        protected titleService: Title,
        private router: Router
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Groups');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Groups`));

        this.load();
    }

    private groupUrl(id): string {
        return SiteUrlHelpers.SITE_URL_GROUP_ROOT + '/' + id;
    }

    private load() {
        this.isLoading = true;
        this.groupService.getGroupList().subscribe(
            (res) => {
                this.groups = res;

                this.isLoading = false;
            }
        );
    }

    private showGroup(group) {
        if (!group) {
            console.log("ERROR : NO GROUP SELECTED TO NAVIGATE TO.");

            return;
        }
		this.router.navigateByUrl(this.federateUrl(SiteUrlHelpers.getGroupUrl(group.ID)));
    }
}
