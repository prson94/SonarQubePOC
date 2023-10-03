import { Component, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CompanySettingsService } from '../../../services/settings.service';
import { SidePanelService } from '../../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { BaseComponent } from '../../shared/base.component';
import { SidePanelButton } from '../../../models/side-panel.model';
import { AssignmentGridComponent } from '../assignment-grid/assignment-grid.component';
import { CompleteAssignmentComponent } from '../complete-assignment/complete-assignment.component';
import { AssignmentItemStep, WorkflowAssignmentItem } from '../../../models/workflow.model';
import { AssignmentProgressComponent } from '../assignment-progress/assignment-progress.component';
import { Router } from '@angular/router';
import { AuthenticationService } from '../../../services/authentication.service';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { Subscription } from 'rxjs';
import { SidePanelSwitcherComponent } from '../side-panel-switcher/side-panel-switcher.component';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';

/*global $localize*/

@Component({
	selector: 'd3s-assignment-list',
	templateUrl: './assignment-list.component.html'
})
export class AssignmentListComponent extends BaseComponent implements OnInit, OnDestroy {

	@Input() showPageHeader: boolean = true;
	@Input() assetTypeUid: string;
	@Input() assetUid: string;
	@ViewChild(AssignmentProgressComponent) assignmentProgressComponent: AssignmentProgressComponent;
	@ViewChild('sidePanelSwitcherComponent') sidePanelSwitcherComponent: SidePanelSwitcherComponent;
	isRequestsFlow: boolean = false; // flag checks if url is requests
	flowContext: string = 'Assignment'; // to store flow context
	sidePanelStorageKey: string;
	showSidePanel: boolean = true;
	sidePanelOpen: boolean = false;
	sidePanelTab: string = 'information';
	isAdmin: boolean = false;
	secondarySidePanelOpen: boolean = false;
	selectedWorkflowItems: WorkflowAssignmentItem[] = [];
	assignmentItemStep: AssignmentItemStep;
	sidePanelButtons: SidePanelButton[] = [];

	sidePanelMultiSelectButtons: SidePanelButton[] = [
		new SidePanelButton({
			label: $localize`${this.selectedWorkflowItems?.length} Assignments Selected`,
			tooltip: $localize`${this.selectedWorkflowItems?.length} Assignments Selected`,
			disabledTooltip: null,
			nothingSelectedMessage: $localize`Select multiple assignments from the list to display its information`,
			notApplicableMessage: $localize`Information data is not available for the selected assignments`,
			multipleSelectedMessage: $localize`multiple assignments selected`,
			key: 'delete',
			icon: 'fa-info-circle',
			disabled: false,
			visible: true,
			needsSelection: true
		})
	];
	@ViewChild('assignmentGridComponent') assignmentGridComponent: AssignmentGridComponent;
	@ViewChild('completeAssignmentComponent') completeAssignmentComponent: CompleteAssignmentComponent;
	private linkInterceptorSubscription: Subscription;

	constructor(
		public sidePanelService: SidePanelService,
		private router: Router,
		private authenticationService: AuthenticationService,
		protected settingsService: CompanySettingsService,
		private headerBreadcrumbService: HeaderBreadcrumbService,
		private linkClickInterceptor: LinkClickInterceptor) {
		super(settingsService);
		this.authenticationService.checkCurrentUserAdmin().subscribe((res) => { this.isAdmin = res; });
	}

	ngOnInit(): void {
		if (this.router.url === '/requests') {
			this.flowContext = 'Request';
			this.isRequestsFlow = true;
		}
		this.setFlowSpecificDetails();
		if (this.router.url === '/requests' || this.router.url === '/assignments') {
			this.setHeaderBreadcrumbs();
		}
		this.linkInterceptorSubscription = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			this.linkClickInterceptor.handleEvent(this.sidePanelSwitcherComponent, ev);
			this.secondarySidePanelOpen = true;
		});
	}

	setHeaderBreadcrumbs(): void {
		this.headerBreadcrumbService.clearBreadcrumbs();
		const siteUrl: string = this.isRequestsFlow ? SiteUrlHelpers.SITE_URL_REQUESTS_ROOT : SiteUrlHelpers.SITE_URL_ASSIGNMENTS_ROOT;
		this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`${this.flowContext}s`, siteUrl));
	}

	setFlowSpecificDetails(): void {
		this.sidePanelStorageKey = this.flowContext + '_' + this.settingsService.CurrentResourceID;
		this.sidePanelButtons = [
			new SidePanelButton({
				label: $localize`${this.flowContext} Progress`,
				tooltip: $localize`${this.flowContext} Progress`,
				disabledTooltip: null,
				nothingSelectedMessage: $localize`Select ${this.flowContext} from the list to display its progress`,
				notApplicableMessage: $localize`Progress data is not available for the selected Assignment`,
				multipleSelectedMessage: $localize`Select a single ${this.flowContext} to display it’s progress`,
				key: 'progress',
				icon: 'fa-step-forward',
				disabled: false,
				visible: true,
				needsSelection: true
			}), new SidePanelButton({
				label: $localize`${this.flowContext} Information`,
				tooltip: $localize`${this.flowContext} Information`,
				disabledTooltip: null,
				nothingSelectedMessage: $localize`Select ${this.flowContext} from the list to display its information`,
				notApplicableMessage: $localize`Information data is not available for the selected Assignment`,
				multipleSelectedMessage: $localize`Select a single ${this.flowContext} to display it’s information`,
				key: 'information',
				icon: 'fa-info-circle',
				disabled: false,
				visible: true,
				needsSelection: true
			})
		];
	}

	selectRow(rows: WorkflowAssignmentItem[]): void {
		this.secondarySidePanelOpen = false;
		if (this.isAdmin && !this.isRequestsFlow && rows?.length > 1) {
			this.sidePanelMultiSelectButtons[0].label = $localize`${this.selectedWorkflowItems?.length} Assignments Selected`;
			this.sidePanelMultiSelectButtons[0].tooltip = $localize`${this.selectedWorkflowItems?.length} Assignments Selected`;
			this.sidePanelTab = 'delete';
			this.sidePanelService.setSidePanelState({ expanded: true });
		} else {
			if (this.sidePanelTab === 'delete') {
				this.sidePanelTab = 'information';
			}				
		}
	}

	getSidePanelWidth(): number {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
	}

	getSidePanelMaxWidth(): number {
		return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
	}

	getSidePanelMinWidth(): number {
		return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
	}

	onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
		this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
	}

	workflowSelectionChanged(workflowMonitorItems: WorkflowAssignmentItem[]): void {
		this.selectedWorkflowItems = workflowMonitorItems;
		this.selectRow(workflowMonitorItems);
	}

	deleteAssignments(event: boolean): void {
		if (event) {
			this.assignmentGridComponent.clickMenuItem({
                value: $localize`Delete`.toLowerCase(),
                action: '',
                event: undefined,
                data: undefined
            });
		}
	}

	stepClicked(assignmentItemStep: AssignmentItemStep): void {
		this.secondarySidePanelOpen = true;
		this.assignmentItemStep = assignmentItemStep;
	}

	closeSecondarySidePanel(): void {
		this.secondarySidePanelOpen = false;
		this.assignmentProgressComponent?.deselectWorkflowSteps();
		this.sidePanelSwitcherComponent?.clear();
	}

	ngOnDestroy(): void {
		this.linkInterceptorSubscription?.unsubscribe();
	}

	onCompleteAssignmentModalClose() {
		this.assignmentGridComponent.loadData();
	}
}
