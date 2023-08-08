import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { SingleAssignment, WorkflowUserGroupedAssignments } from '../../../models/workflow.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { WorkflowService } from '../../../services/workflow.service';
import { BaseComponent } from '../../shared/base.component';
import { AssignmentsMultiPickerComponent } from '../assignments-multi-picker/assignments-multi-picker.component';
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
	loadSub: Subscription;

	totalRecords: number;
	rowsPerPage: number = 10;
	currentPageNumber: number = 1;
	assignments: WorkflowUserGroupedAssignments[];

	isMe: boolean = false;
	@ViewChild('completeAssignmentComponent') completeAssignmentComponent: CompleteAssignmentComponent;
	@ViewChild('multiAssignComponent') multiAssignComponent: AssignmentsMultiPickerComponent;

	initialWorkflowItemUid: string = '';

	constructor(public settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private route: ActivatedRoute,
		private changeDetectorRef: ChangeDetectorRef) {
		super(settingsService);
		this.initialWorkflowItemUid = '';
		this.workflowService.assignmentCompletedSubject.subscribe(() => {
			this.loadUserAssignments();
		});

		this.route.queryParams
			.subscribe((params: { initialWorkflowItemUid: string }) => {
				if (params.initialWorkflowItemUid) {
					this.initialWorkflowItemUid = params.initialWorkflowItemUid.toLowerCase();
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
	}

	loadUserAssignments() {
		if (this.loadSub) {
			this.loadSub.unsubscribe();
		}
		this.isLoading = true;
		this.loadSub =
			this.workflowService.getUserAssignments(this.userUid)
				.subscribe((res) => {
					this.assignments = res;
					this.totalRecords = +res.length;

					this.assignments.forEach((item) => {
						if (item.AssociatedItems.length > 1) {
							item.AssociatedWith = $localize`${item.AssociatedItems.length} Assets`;
						}
						else {
							item.AssociatedWith = item.AssociatedItems[0]?.Name ?? '---';
						}
					});

					if (this.initialWorkflowItemUid) {
						this.workflowService.getWorkflowStateForUser(this.initialWorkflowItemUid)
							.subscribe((res) => {
								this.handleWorkflowItemLoad(res);

							});
					}

					this.isLoading = false;
					this.changeDetectorRef.markForCheck();
				});
	}

	private handleWorkflowItemLoad(res: { exists: boolean; hasAccess: boolean; isCompleted: boolean; }) {
        if (!res.hasAccess) {
            window.alert("no access");
        }
        else if (res.exists && res.hasAccess && !res.isCompleted) {
            const item = this.assignments.find((x) => x.AssociatedItems.some((ai) => ai.WorkflowItemUid.toLowerCase() === this.initialWorkflowItemUid));
            if (item) {
                this.onItemClick(null, item);
            }
        }
        else if (!res.exists) {
            window.alert("does not exist");
        }
        else if (res.isCompleted) {
            window.alert("completed");
        }
    }

	onItemClick($event: MouseEvent, item: WorkflowUserGroupedAssignments) {
		if ($event) {
			$event.preventDefault();
			$event.stopPropagation();
		}

		if (item.AssociatedItems.length > 1) {
			this.multiAssignComponent.openModal(item.AssociatedItems);
		}
		else {
			const assignment = item.AssociatedItems[0];
			this.completeAssignmentComponent.openModal({
				workflowItemUid: assignment.WorkflowItemUid,
				stepUid: assignment.ItemStepUid,
				assetId: assignment.AssetId
			});
		}
	}

	onAssignmentSelection(selectedItems: SingleAssignment[]) {
		if (selectedItems.length === 0) {
			return;
		}

		const mainItem = selectedItems[0];

		this.completeAssignmentComponent.openModal({
			workflowItemUid: mainItem.WorkflowItemUid,
			stepUid: mainItem.ItemStepUid,
			assetId: mainItem.AssetId,
			items: selectedItems
		});
	}
}
