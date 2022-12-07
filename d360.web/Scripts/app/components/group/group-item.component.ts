import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { GroupService } from '../../services/group.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { GroupEditorModel } from '../../models/group.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { CompanySettingsService } from '../../services/settings.service';
import { UsageAction } from '../../models/web-analytics-activity.model';

/* FIXME: Extract templates and styles to their own files
*  https://angular.io/guide/styleguide#style-05-04 */
@Component({
    selector: 'd3s-group-item',
    providers: [GroupService],
    templateUrl: 'group-item.component.html'
})

export class GroupItemComponent extends BaseComponent implements OnInit {

    sub: any;
    model: GroupEditorModel;
    groupId: number;
    groupUid: string;

    constructor(
        private groupService: GroupService,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        protected titleService: Title,
        private route: ActivatedRoute,
        private router: Router) {
        super(settingsService);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe((params) => {
            this.groupId = +params['groupId']; // (+) converts string 'id' to a number
            this.headerBreadcrumbService.setCurrentObjectInfo('Group', this.groupId);
            this.isLoading = true;

            this.groupService.getGroup(this.groupId, "").subscribe(
                (group) => {
                    this.model = group;
                    this.headerBreadcrumbService.clearBreadcrumbs();
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Groups`, SiteUrlHelpers.SITE_URL_GROUP_ROOT));
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.model.group.Name));

                    this.setBrowserTitle(this.titleService, this.model.group.Name);
					this.logAssetAction(UsageAction.View, this.model.group.Uid);

                    this.isLoading = false;
                }
            );
        });
    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
        this.clearSidebar();
    }

    private load() {

    }
}