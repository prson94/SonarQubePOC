import { Component, OnInit } from '@angular/core';
import { IOutputData } from 'angular-split';
import { SidePanelService } from '../../../services/side-panel.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { AssignmentItem } from '../../../models/workflow.model';
import { ActivatedRoute, Router } from '@angular/router';
import { WorkflowService } from '../../../services/workflow.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';

@Component({
	selector: 'd3s-assignment-details-container',
	templateUrl: './assignment-details-container.component.html',
	styleUrls: ['./assignment-details-container.component.less']
})
export class AssignmentDetailsContainerComponent extends BaseComponent implements OnInit {
	sidePanelOpen: boolean;
	sidePanelStorageKey: string;
	sidePanelTab: string;
	assignmentUid: string;
	assignmentItem: AssignmentItem;
	private flowContext: string = 'assignmentDetails';
	private isAdmin: boolean = false;
	private isRequestDetailsFlow: boolean = false;

	constructor(public sidePanelService: SidePanelService,
				private companySettingsService: CompanySettingsService,
				private authenticationService: AuthenticationService,
				private headerBreadcrumbService: HeaderBreadcrumbService,
				private workflowService: WorkflowService,
				private route: ActivatedRoute,
				private router: Router,
				secondaryNavService: SecondaryNavService) {
		super(companySettingsService);
		this.authenticationService.checkCurrentUserAdmin().subscribe((response: boolean): void => {
			this.isAdmin = response;
		});
		this.assignmentUid = this.getAssignmentUidFromUrlParam();
		this.secondaryNavService = secondaryNavService;
		if (this.router.url === '/requests') {
			this.flowContext = 'requestDetails';
			this.isRequestDetailsFlow = true;
		}
	}

	ngOnInit(): void {
		this.setFlowSpecificDetails();
		this.loadAssignmentItem();
	}

	getSidePanelMaxWidth(): number {
		return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
	}

	getSidePanelMinWidth(): number {
		return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
	}

	getSidePanelWidth() {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
	}

	onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData) {
		if (this.sidePanelOpen) {
			this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
		}
	}

	private setFlowSpecificDetails() {
		this.sidePanelOpen = true;
		this.sidePanelStorageKey = this.flowContext + '_' + this.companySettingsService.CurrentResourceID;
	}

	private setHeaderBreadCrumbs() {
		this.headerBreadcrumbService.clearBreadcrumbs();
		this.secondaryNavService.clearItems();
		this.secondaryNavService.clearCurrentObject();
		this.secondaryNavService.showHeader(true);
		if (this.isRequestDetailsFlow) {
			const pageTitle: string = $localize`Request Progress and Information`;
			this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Requests`, SiteUrlHelpers.SITE_URL_REQUESTS_ROOT));
			this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(pageTitle, null));
			this.setBrowserTitle(this.headerBreadcrumbService.getTitleService(), pageTitle);
			this.secondaryNavService.setCurrentArea(pageTitle, 'fa-list-ul', pageTitle, [this.getStatusBadge()]);
		} else {
			const pageTitle: string = $localize`Assignment Progress and Information`;
			this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Assignments`, SiteUrlHelpers.SITE_URL_ASSIGNMENTS_ROOT));
			this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(pageTitle, null));
			this.setBrowserTitle(this.headerBreadcrumbService.getTitleService(), pageTitle);
			this.secondaryNavService.setCurrentArea(pageTitle, 'fa-list-ul', pageTitle, [this.getStatusBadge()]);
		}
	}

	private loadAssignmentItem() {
		this.workflowService.getAssignmentItem(this.assignmentUid)
			.subscribe((assignmentItem: AssignmentItem): void => {
				this.assignmentItem = assignmentItem;
				this.setHeaderBreadCrumbs();
			});
	}

	private getAssignmentUidFromUrlParam() {
		return this.route.snapshot.paramMap.get('assignmentUid');
	}

	private getStatusBadge(): string {
		return JSON.stringify([{
			'name': this.getAssignmentStatus(this.assignmentItem.Status),
			'color': this.getBadgeColor(this.assignmentItem.Status)
		}]);
	}

	private getBadgeColor(assignmentStatus: string): string {
		if (assignmentStatus === 'Complete') {
			return '#6f7482';
		} else {
			return '#006fba';
		}
	}

	private getAssignmentStatus(Status: string): string {
		if (Status === 'Complete') {
			return $localize`Complete`;
		} else {
			return $localize`Pending`;
		}
	}
}
