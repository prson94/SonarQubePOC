import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { WorkflowUserGroupedAssignments } from '../../../models/workflow.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { WorkflowService } from '../../../services/workflow.service';
import { BaseComponent } from '../../shared/base.component';
import { CompleteAssignmentComponent } from '../complete-assignment/complete-assignment.component';

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

	constructor(public settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private changeDetectorRef: ChangeDetectorRef) {
		super(settingsService)

		this.workflowService.assignmentCompletedSubject.subscribe((res) => {
			console.log("here");
			this.loadUserAssignments();
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

					this.isLoading = false;
					this.changeDetectorRef.markForCheck();
				});
	}

	onItemClick($event: MouseEvent, item: WorkflowUserGroupedAssignments) {
		$event.preventDefault();
		$event.stopPropagation();

		//dd4535e7-e2f9-42ab-9a3c-25593e1c52f3
		//80e7b86f-2bf9-432c-b328-c34059c04224
		//8850
		console.log(item);

		if (item.AssociatedItems.length > 1) {
			window.alert("multi assignment completion not yet implemented");
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
}
