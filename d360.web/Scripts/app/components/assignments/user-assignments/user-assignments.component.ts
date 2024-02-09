import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LazyLoadEvent } from 'primeng/api';
import { Subscription } from 'rxjs';
import { FeatureFlagService } from '../../../guards/feature-flag.service';
import { SortOrder } from '../../../models/enums.model';
import { AssignmentItemStep, AssignmentSelection, WorkflowStateForUser, WorkflowUserGroupedAssignment } from '../../../models/workflow.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { WorkflowService } from '../../../services/workflow.service';
import { BaseComponent } from '../../shared/base.component';
import { AssignmentsMultiPickerComponent } from '../assignment-multi-picker/assignment-multi-picker.component';
import { CompleteAssignmentComponent } from '../complete-assignment/complete-assignment.component';

/*global $localize*/

@Component({
	selector: 'd3s-user-assignments',
	templateUrl: './user-assignments.component.html',
	styleUrls: ['./user-assignments.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserAssignmentsComponent extends BaseComponent implements OnInit, OnDestroy {
	@Input() userUid: string;
	@Input() isAdminPage: boolean = false;
	loadSub: Subscription;
	workflowTypeName: string;

	totalRecords: number;
	rowsPerPage: number = 10;
	currentPageNumber: number = 1;
	assignments: WorkflowUserGroupedAssignment[];
	selectedAssignment: WorkflowUserGroupedAssignment
	isMe: boolean = false;
	isReassign: boolean = false;
	sortField: string = "workflowName";
	sortOrder: SortOrder = SortOrder.Descending;
	storageKey = 'userAssignmentGrid' + this.settingsService.CurrentResourceID;
	canActivateAssignmentDetails: boolean = false;

	@ViewChild('completeAssignmentComponent') completeAssignmentComponent: CompleteAssignmentComponent;
	@ViewChild('multiAssignComponent') multiAssignComponent: AssignmentsMultiPickerComponent;

	urlWorkflowTypeUid: string = '';
	urlWorkflowStepUid: string = '';
	urlWorkflowVersion: number = 0;
	onlyAdminReassignMode: boolean = false;
	urlWorkflowItemUid: string = '';
	redirectToDetails: boolean = false;

	constructor(public settingsService: CompanySettingsService,
				private workflowService: WorkflowService,
				private route: ActivatedRoute,
				private changeDetectorRef: ChangeDetectorRef,
				private featureFlagService: FeatureFlagService,
				private router: Router) {
		super(settingsService);
		this.urlWorkflowTypeUid = this.urlWorkflowStepUid = '';
		this.workflowService.assignmentCompletedSubject.subscribe(() => {
			this.loadUserAssignments();
		});

		this.route.queryParams
			.subscribe((params: { workflowTypeUid: string, workflowItemStepUid?: string, version: number, workflowItemUid: string }) => {
				if (params.workflowTypeUid) {
					this.urlWorkflowTypeUid = (params.workflowTypeUid ?? "").toLowerCase();
					this.urlWorkflowStepUid = (params.workflowItemStepUid ?? "").toLowerCase();
					this.urlWorkflowVersion = +(params.version ?? 0);
					this.urlWorkflowItemUid = (params.workflowItemUid ?? "").toLowerCase();
				}
			});
	}

	ngOnDestroy() {
		if (this.loadSub) {
			this.loadSub.unsubscribe();
		}
	}

	ngOnInit(): void {
		if (!this.userUid) {
			this.userUid = this.settingsService.CurrentResourceUid;
		}
		if (this.userUid.toLowerCase() === this.settingsService.CurrentResourceUid.toLowerCase()) {
			this.isMe = true;
		}
		this.loadUserAssignments();
		this.canActivateAssignmentDetails = this.featureFlagService.canActivateAssignmentDetails();
	
	}

	loadWorkflowAssignmentItems(event: LazyLoadEvent): void {
		this.loadRowsPerPage(event);
		this.sortOrder = event.sortField == null ? SortOrder.Descending : event.sortOrder;
		this.sortField = event.sortField == null ? '' : event.sortField;
		this.currentPageNumber = (event.first / event.rows) + 1;
		this.loadUserAssignments();
	}

	setRowsPerPage(event): void {
		if (event?.rows) {
			localStorage.setItem(this.storageKey, event.rows);
		}
	}	

	loadRowsPerPage(event: LazyLoadEvent): void {
		const rowsPerPageStorage: string = localStorage.getItem(this.storageKey);
		this.rowsPerPage = rowsPerPageStorage != null ? Number(rowsPerPageStorage) : event?.rows;
	}

	loadUserAssignments(forcedRefresh: boolean = false) {
		if (!this.isMe && this.isAdminPage) {
			this.onlyAdminReassignMode = true;
		}

		if (this.loadSub) {
			this.loadSub.unsubscribe();
		}

		const params = { _pageSize: this.rowsPerPage, _pageNum: this.currentPageNumber, _order: this.sortField, _direction: this.sortOrder && this.sortOrder === SortOrder.Descending ? "desc" : "asc"  };

		this.isLoading = true;
		this.loadSub =
			this.workflowService.getUserAssignments(this.userUid, params)
				.subscribe((res) => {
					this.assignments = res.items;
					this.totalRecords = +res.total;
					this.assignments.forEach((item) => {
						if (item.AssociatedItems.length > 1) {
							item.AssociatedWith = $localize`${item.AssociatedItems.length} Assets`;
						}
						else {
							item.AssociatedWith = item.AssociatedItems[0]?.Name ?? '---';
						}
					});

					if (this.urlWorkflowStepUid) {
						this.workflowService.getWorkflowStateForUser(this.urlWorkflowStepUid)
							.subscribe((res) => {
								this.handleWorkflowItemLoad(res, forcedRefresh);			
							});
					}
					else if (this.urlWorkflowTypeUid) {
						this.handleWorkflowItemLoad({ exists: false, hasAccess: true, isCompleted: true, workflowItemUid: null, workflowName: null, assignmentCount: 0, isAssignee: true }, forcedRefresh);
					}
					else {
						this.isLoading = false;
						this.changeDetectorRef.markForCheck();
					}
				});
	}

	private handleWorkflowItemLoad(workflowState: WorkflowStateForUser, forcedRefresh: boolean) {
		// check if there is exiting step in assignments
		if (this.urlWorkflowItemUid) {
			this.workflowService.getAssignmentItemSteps(this.urlWorkflowItemUid)
				.subscribe((response: AssignmentItemStep[]): void => {
					const step = response.find(step => step.Uid === this.urlWorkflowStepUid);
					if (step?.ActivityType !== 'Form') {
						this.redirectToDetails = true;
					} else {
						this.redirectToDetails = false;
					}
					this.handleFormDialogLoad(workflowState, forcedRefresh);
				});
		} else {
			this.handleFormDialogLoad(workflowState, forcedRefresh);
		}	
		
	}

	private handleFormDialogLoad(workflowState: WorkflowStateForUser, forcedRefresh: boolean) {
		let assignmentItem = this.assignments.find((x) => x.Version === this.urlWorkflowVersion && x.AssociatedItems.some((ai) => ai.ItemStepUid.toLowerCase() === this.urlWorkflowStepUid.toLowerCase()));
		if (assignmentItem && !this.redirectToDetails) {
			this.onItemClick(null, assignmentItem);
			this.isLoading = false;			
		}
		else {
			// check if there is exiting workflow type + version combination to open form
			assignmentItem = this.assignments.find((x) => x.Version === this.urlWorkflowVersion && x.WorkflowTypeUid.toLowerCase() === this.urlWorkflowTypeUid.toLowerCase());
			if (assignmentItem && !this.redirectToDetails) {
				this.onItemClick(null, assignmentItem);
				this.isLoading = false;				
			} else {
				const params = { _workflowTypeUid: this.urlWorkflowTypeUid.toLowerCase(), _workflowVersion: this.urlWorkflowVersion };
				this.workflowService.getUserAssignments(this.userUid, params).subscribe((res) => {
					if (res?.items.length > 0 && !this.redirectToDetails) {
						this.onItemClick(null, res.items[0]);
					} else if (!forcedRefresh) {
						this.handleNoAssignments(workflowState);
					}
					this.isLoading = false;
					this.changeDetectorRef.markForCheck();
				});
			}
		}
		this.changeDetectorRef.markForCheck();
	}
	
	modalVisible: boolean = false;
	errorModalTitle: string;
	errorSubTitle: string;
	errorModalMessage: string;
	showAssignmentDetailsLink: boolean = false
	private handleNoAssignments(res: WorkflowStateForUser) {
		this.errorSubTitle = res.workflowName;
		if (!res.exists) {
			this.errorModalTitle = $localize`Assignment Not Found`;
			this.errorModalMessage = $localize`The Assignment cannot be found. It might have been deleted or the link is invalid. Contact your Administrator to remediate the issue.`;
			this.modalVisible = true;
		}
		else if (res.isCompleted && !this.redirectToDetails) {
			this.errorModalTitle = $localize`Assignment Completed`;
			this.errorModalMessage = $localize`The form has already been submitted by required assignees.`;
			this.modalVisible = true;
			this.showAssignmentDetailsLink = true;
		}
		else if (!res.hasAccess) {
			this.errorModalTitle = $localize`You Cannot View the Assignment`;
			this.errorModalMessage = $localize`You do not have permissions to view this Assignment. Contact your Administrator to remediate the issue.`;
			this.modalVisible = true;
			this.showAssignmentDetailsLink = false;
		}
		else if (!res.isAssignee && !this.redirectToDetails) {
			this.errorModalTitle = $localize`Not Assigned to You`;
			this.errorModalMessage = $localize`You are not an assignee for this form, but you can view the Assignment's details.`;
			this.modalVisible = true; 
			this.showAssignmentDetailsLink = true;
		} else if (this.redirectToDetails) {
			this.router.navigate(['assignmentDetails', this.urlWorkflowItemUid])
		}
	}

	onItemClick($event: MouseEvent, item: WorkflowUserGroupedAssignment) {
		if ($event) {
			$event.preventDefault();
			$event.stopPropagation();
		}
		this.selectedAssignment = item;
		if (item.AssociatedItems.length > 1) {
			this.workflowTypeName = item.WorkflowName;
			this.multiAssignComponent.openModal(item.AssociatedItems, item.WorkflowName, item.WorkflowTypeUid);
		}
		else {
			const assignment = item.AssociatedItems[0];
			this.completeAssignmentComponent.openModal({
				workflowItemUid: assignment.WorkflowItemUid,
				stepUid: assignment.ItemStepUid,
				assetId: assignment.AssetId,
				isReassign: this.isAdminPage && !this.isMe
			});
		}
	}

	onAssignmentSelection(event: AssignmentSelection) {
		this.isReassign = event?.isReassign ?? false;
		if (event.selectedItems.length === 0) {
			return;
		}
		else if (event.selectedItems.length === 1) {
			const assignment = event.selectedItems[0];
			
			this.completeAssignmentComponent.openModal({
				workflowItemUid: assignment.WorkflowItemUid,
				stepUid: assignment.ItemStepUid,
				assetId: assignment.AssetId,
				areAllMultiAssignmentsSelected: event.selectedAll,
				showBackButton: true,
				isReassign: (event?.isReassign ?? false)
			});
		}
		else {
			const mainItem = event.selectedItems[0];

			this.completeAssignmentComponent.openModal({
				workflowItemUid: mainItem.WorkflowItemUid,
				stepUid: mainItem.ItemStepUid,
				assetId: mainItem.AssetId,
				items: event.selectedItems,
				selectedAssignment: this.selectedAssignment,
				areAllMultiAssignmentsSelected: event.selectedAll,
				showBackButton: true,
				isReassign: (event?.isReassign ?? false)
			});
		}
	}

	onCompleteAssignmentModalClose(event: { isBack: boolean, removeSelected: boolean }) {
		if (event.isBack === false) {
			this.multiAssignComponent.closeDialog();
		} else {
			this.multiAssignComponent.subscribeSwitcherEvents();
		}

		if (event.removeSelected) {
			this.multiAssignComponent.removeSelected();
		}
		this.loadUserAssignments(true);
	}

	onAssignmentMultiPickerClose(): void {
		this.completeAssignmentComponent?.closeModal();
	}

	getAssignmentUrl(): string {
		return '/assignmentDetails/' + this.urlWorkflowItemUid;
	}
}
