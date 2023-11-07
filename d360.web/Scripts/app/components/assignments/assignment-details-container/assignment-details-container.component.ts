import { Component, OnInit, ViewChild } from '@angular/core';
import { IOutputData } from 'angular-split';
import { SidePanelService } from '../../../services/side-panel.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { AuthenticationService } from '../../../services/authentication.service';
import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import {
	AssignmentItem,
	AssignmentItemStep,
	WorkflowAssignmentItem,
	WorkflowAssignments,
	WorkflowStepDetail
} from '../../../models/workflow.model';
import { ActivatedRoute, Router } from '@angular/router';
import { WorkflowService } from '../../../services/workflow.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { DynamicButton } from '../../../models/secondaryNav.model';
import { CompleteAssignmentComponent } from '../complete-assignment/complete-assignment.component';

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
	completeAssignmentButton: DynamicButton;
	private flowContext: string = 'assignmentDetails';
	private isAdmin: boolean = false;
	private isRequestDetailsFlow: boolean = false;

	@ViewChild('completeAssignmentComponent') private completeAssignmentComponent: CompleteAssignmentComponent;
	private workflowStepDetail: WorkflowStepDetail;
	private assignmentItemStep: AssignmentItemStep;
	private workflowAssignment: WorkflowAssignmentItem;

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
		if (this.router.url.startsWith('/requests/')) {
			this.flowContext = 'requestDetails';
			this.isRequestDetailsFlow = true;
		}
		this.completeAssignmentButton = new DynamicButton($localize`Complete Assignment`);
		this.setHeaderButton();
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

	private setHeaderBreadCrumbs(): void {
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
				this.loadAssignmentSteps();
			});
	}

	private getAssignmentUidFromUrlParam() {
		return this.route.snapshot.paramMap.get('assignmentUid');
	}

	private getStatusBadge(): string {
		return JSON.stringify([{
			'name': this.getBadgeLabel(this.assignmentItem.Status),
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

	private getBadgeLabel(assignmentStatus: string): string {
		if (assignmentStatus === 'Complete') {
			return $localize`Complete`;
		} else {
			return $localize`Pending`;
		}
	}

	workflowClicked(workflowUid: string): void {

	}

	assetClicked(assetUid: string): void {

	}

	initiatorClicked(initiatorUid: string): void {

	}

	private setHeaderButton(): void {
		this.secondaryNavService.clearButtons();
		if (!this.isRequestDetailsFlow && this.workflowStepDetail?.ItemSettings?.hasPendingForms && this.workflowStepDetail?.IsAssignedLoginUser && !(this.workflowStepDetail?.CompletedOn) && this.workflowAssignment.isCurrentUserAssigned) {
			this.secondaryNavService.showButton(this.completeAssignmentButton);
			this.completeAssignmentButton.dynamicCallback = () => {
				this.completeAssignmentButton.disabled = true;
				this.completeAssignmentButton.isLoading = true;
				this.onCompleteAssignmentClicked();
			};
		}
	}

	onCompleteAssignmentModalClose({ action }: {
		isBack: boolean,
		removeSelected: boolean,
		action?: string
	}): void {
		if (action?.toLowerCase() === 'complete') {
			this.assignmentItem = null;
			this.loadAssignmentItem();
		}
		this.setHeaderButton();
	}

	onCompleteAssignmentClicked(): void {
		this.completeAssignmentComponent.openModal({
			workflowItemUid: this.assignmentItem?.WorkflowItemUid,
			stepUid: this.assignmentItemStep?.Uid
		});
	}

	private loadAssignmentSteps() {
		this.workflowStepDetail = null;
		this.workflowService.getAssignmentItemSteps(this.assignmentItem.WorkflowItemUid)
			.subscribe((assignmentItemSteps: AssignmentItemStep[]): void => {
				this.assignmentItemStep = null;
				let stepCounter: number = 0;
				for (; stepCounter < assignmentItemSteps.length; stepCounter++) {
					if (!assignmentItemSteps[stepCounter].CompletedOn) {
						this.assignmentItemStep = assignmentItemSteps[stepCounter];
						this.workflowService.getAssignmentStepDetail(assignmentItemSteps[stepCounter].Uid).subscribe((response: WorkflowStepDetail) => {
							this.workflowStepDetail = response;
							this.loadWorkflowAssignment(this.assignmentItem.WorkflowItemUid);
						});
						break;
					}
				}
				if (stepCounter === assignmentItemSteps.length) {
					this.setHeaderButton();
				}
			});
	}

	private loadWorkflowAssignment(workflowItemUid: string) {
		this.workflowService.getWorkflowAssignments(1, 1, null, '(workflowItemUid eq \'' + workflowItemUid + '\')').subscribe((workflowAssignments: WorkflowAssignments): void => {
			this.workflowAssignment = workflowAssignments.items[0];
			this.setHeaderButton();
		});
	}
}
