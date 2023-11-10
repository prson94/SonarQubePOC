import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { IOutputData } from 'angular-split';
import { SidePanelService } from '../../../services/side-panel.service';
import { CompanySettingsService } from '../../../services/settings.service';
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
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { Subscription } from 'rxjs';
import { SidePanelSwitcherComponent } from '../side-panel-switcher/side-panel-switcher.component';
import { SidePanelButton } from '../../../models/side-panel.model';
import { AssignmentDetailsComponent } from './assignment-details/assignment-details.component';

@Component({
	selector: 'd3s-assignment-details-container',
	templateUrl: './assignment-details-container.component.html',
	styleUrls: ['./assignment-details-container.component.less']
})
export class AssignmentDetailsContainerComponent extends BaseComponent implements OnInit, OnDestroy {
	sidePanelOpen: boolean;
	sidePanelStorageKey: string;
	sidePanelTab: string;
	assignmentUid: string;
	assignmentItem: AssignmentItem;
	selectedItem: object;
	workflowAssignment: WorkflowAssignmentItem;
	sidePanelButtons: SidePanelButton[] = [
		new SidePanelButton({
			label: $localize`Information`,
			tooltip: $localize`Information`,
			disabledTooltip: null,
			nothingSelectedMessage: $localize`Select one of the links on the left to display its information`,
			notApplicableMessage: $localize`Information is not available for the selected option`,
			multipleSelectedMessage: $localize`Select a single link to display it’s information`,
			key: 'information',
			icon: 'fa-info-circle',
			disabled: false,
			visible: true,
			needsSelection: true
		})
	];
	@ViewChild('completeAssignmentComponent') private completeAssignmentComponent: CompleteAssignmentComponent;
	@ViewChild('sidePanelSwitcherComponent') private sidePanelSwitcherComponent: SidePanelSwitcherComponent;
	@ViewChild('assignmentDetailsComponent') private assignmentDetailsComponent: AssignmentDetailsComponent;

	private workflowStepDetail: WorkflowStepDetail;
	private assignmentItemStep: AssignmentItemStep;
	private linkInterceptorSubscription: Subscription;
	private readonly isRequestDetailsFlow: boolean = false;
	private readonly flowContext: string = 'assignmentDetails';

	constructor(public sidePanelService: SidePanelService,
				private companySettingsService: CompanySettingsService,
				private headerBreadcrumbService: HeaderBreadcrumbService,
				private workflowService: WorkflowService,
				private route: ActivatedRoute,
				private router: Router,
				private linkClickInterceptor: LinkClickInterceptor,
				secondaryNavService: SecondaryNavService) {
		super(companySettingsService);
		this.assignmentUid = this.getAssignmentUidFromUrlParam();
		this.secondaryNavService = secondaryNavService;
		if (this.router.url.startsWith('/requests/')) {
			this.flowContext = 'requestDetails';
			this.isRequestDetailsFlow = true;
		}
		this.setHeaderButton();
	}

	ngOnInit(): void {
		this.setFlowSpecificDetails();
		this.loadAssignmentItem();
		this.subscribeSwitcherEvents();
	}

	ngOnDestroy(): void {
		this.unsubscribeSwitcherEvents();
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

	workflowClicked(mouseEvent: MouseEvent, workflowUid: string, workflowTypeVersion: number): void {
		this.linkClickInterceptor.sendEvent(mouseEvent, {
			workflowTypeUid: workflowUid,
			workflowTypeVersion: workflowTypeVersion
		}, null);
	}

	assetClicked(mouseEvent: MouseEvent, assetUid: string): void {
		this.linkClickInterceptor.sendEvent(mouseEvent, { AssetUid: assetUid }, 'assets/' + assetUid);
	}

	initiatorClicked(mouseEvent: MouseEvent, initiatorUid: string): void {
		this.linkClickInterceptor.sendEvent(mouseEvent, { ResourceUid: initiatorUid }, 'users/' + initiatorUid);
	}

	completeAssignmentModalClosed({ action }: {
		isBack: boolean,
		removeSelected: boolean,
		action?: string
	}): void {
		if (action?.toLowerCase() === 'complete') {
			this.assignmentItem = null;
			this.loadAssignmentItem();
			this.assignmentDetailsComponent.forceRefresh();
		}
		this.setHeaderButton();
		this.subscribeSwitcherEvents();
	}

	private setHeaderButton(): void {
		this.secondaryNavService.clearButtons();
		if (!this.isRequestDetailsFlow && this.workflowStepDetail?.ItemSettings?.hasPendingForms && this.workflowStepDetail?.IsAssignedLoginUser && !(this.workflowStepDetail?.CompletedOn) && this.workflowAssignment.isCurrentUserAssigned) {
			const completeAssignmentButton: DynamicButton = new DynamicButton($localize`Complete Assignment`);
			this.secondaryNavService.showButton(completeAssignmentButton);
			completeAssignmentButton.dynamicCallback = () => {
				completeAssignmentButton.isLoading = true;
				this.completeAssignmentButtonClicked();
			};
		}
	}

	private completeAssignmentButtonClicked(): void {
		this.unsubscribeSwitcherEvents();
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

	private subscribeSwitcherEvents() {
		this.linkInterceptorSubscription = this.linkClickInterceptor.getEvents().subscribe((ev): void => {
			this.selectedItem = { type: ev.type };
			this.linkClickInterceptor.handleEvent(this.sidePanelSwitcherComponent, ev);
			this.sidePanelOpen = true;
		});
	}

	updatePanelHeader(headerLabel: string): void {
		this.sidePanelButtons[0].label = headerLabel;
	}

	private unsubscribeSwitcherEvents() {
		this.linkInterceptorSubscription?.unsubscribe();
	}
}
