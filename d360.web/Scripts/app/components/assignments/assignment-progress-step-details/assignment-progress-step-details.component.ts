import {
	ChangeDetectorRef,
	Component,
	EventEmitter,
	Input,
	OnChanges,
	OnInit,
	Output,
	SimpleChanges,
	ViewChild
} from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { WorkflowService } from '../../../services/workflow.service';
import {
	StepType,
	WorkflowActivityType,
	WorkflowChangeType,
	WorkflowStepDetail,
	WorkflowStepReassignment
} from '../../../models/workflow.model';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { WorkflowHelpers } from '../../../static/workflow-helpers';
import { map } from 'rxjs/operators';
import { Observable, of } from 'rxjs';
import { CompanySettingsService } from '../../../services/settings.service';
import { AssignmentFormResponseComponent } from './assignment-form-response/assignment-form-response.component';

@Component({
	selector: 'd3s-assignment-progress-step-details',
	templateUrl: './assignment-progress-step-details.component.html',
	styleUrls: ['./assignment-progress-step-details.component.less']
})
export class AssignmentProgressStepDetailsComponent extends BaseComponent implements OnInit, OnChanges {
	@Input() itemStepUid: string;
	@Input() itemId: string;
	@Input() visible: boolean = true;
	@Output() visibleChange = new EventEmitter();
	@Output() onCloseClick = new EventEmitter();
	@ViewChild(AssignmentFormResponseComponent) assignmentFormResponseComponent: AssignmentFormResponseComponent;
	step: WorkflowStepDetail = null;
	activityType: string = '';
	viewFormResponses: string = '';
	bulkReassignments: WorkflowStepReassignment[] = [];
	reassignment: WorkflowStepReassignment = null;
	StepType = StepType;
	WorkflowActivityType = WorkflowActivityType;
	WorkflowChangeType = WorkflowChangeType;
	helper = WorkflowHelpers;

	constructor(
		private responsibilityService: ResponsibilityTypeService,
		protected settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private ref: ChangeDetectorRef) {
		super(settingsService);
	}

	ngOnInit() {
		this.load().subscribe();
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes['itemStepUid'] != null && (changes['itemStepUid'].isFirstChange || (changes['itemStepId'].currentValue !== changes['itemStepUid'].previousValue))) {
			this.load().subscribe();
		}
	}

	load(): Observable<any> {
		this.step = null;
		if (this.itemStepUid != null) {
			this.isLoading = true;
			return this.workflowService.getAssignmentStepDetail(this.itemStepUid)
				.pipe(
					map((r) => {
						this.isLoading = false;
						this.step = r;
						this.activityType = this.getActivityType(this.step);
						if (this.step.ItemFields?.['@NumberOfResponses']) {
							this.viewFormResponses = `View Form Responses (${this.step.ItemFields['@NumberOfResponses']})`;
						} else {
							this.viewFormResponses = '';
						}
						let reassignments: WorkflowStepReassignment[] = [];
						if (this.step.ItemFields?.Reassigned != null) {
							for (const element of this.step.ItemFields.Reassigned) {
								reassignments.push(new WorkflowStepReassignment(element));
							}
						}
						this.bulkReassignments = this.getBulkReassignments(reassignments);
						this.reassignment = this.getReassignment(reassignments);
						this.ref.markForCheck();
					})
				);
		} else {
			return of();
		}
	}


	private getBulkReassignments(reassignments: WorkflowStepReassignment[]): WorkflowStepReassignment[] {
		return reassignments.filter((r: WorkflowStepReassignment) => r.IsBulkReassignment);
	}

	private getReassignment(reassignments: WorkflowStepReassignment[]): null | WorkflowStepReassignment {
		if (reassignments == null || reassignments.length < 1) {
			return null;
		} else if (reassignments.length === 1 && !reassignments[0].IsBulkReassignment) {
			return reassignments[0];
		} else if (reassignments.length > 1) {
			return reassignments.find((r: WorkflowStepReassignment) => !r.IsBulkReassignment);
		} else {
			return null;
		}
	}

	getActivityType(step: any): string {
		if (step.ActivityType !== 0) {
			return this.helper.activityTypeName(step.ActivityType);
		} else {
			return this.helper.stepTypeName(step.StepType);
		}
	}

	openFormResponsesModal(): void {
		this.assignmentFormResponseComponent.openModal(this.step)
	}
}
