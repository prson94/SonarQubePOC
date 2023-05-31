import {
	ChangeDetectorRef,
	Component,
	EventEmitter,
	Input,
	OnChanges,
	OnInit,
	Output,
	SimpleChanges
} from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { WorkflowService } from '../../../services/workflow.service';
import {
	StepType,
	WorkflowActivityType,
	WorkflowChangeType,
	WorkflowStepReassignment
} from '../../../models/workflow.model';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { WorkflowHelpers } from '../../../static/workflow-helpers';
import { map } from 'rxjs/operators';
import { Observable, of } from 'rxjs';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
	selector: 'd3s-assignment-progress-step-details',
	templateUrl: './assignment-progress-step-details.component.html',
	styleUrls: ['./assignment-progress-step-details.component.less']
})
export class AssignmentProgressStepDetailsComponent extends BaseComponent implements OnInit, OnChanges {
	@Input() itemStepId: number;
	@Input() itemId: number;
	@Input() visible: boolean = true;
	@Output() visibleChange = new EventEmitter();
	@Output() onCloseClick = new EventEmitter();
	step: any = null;
	activityType: string = '';
	reassignments: WorkflowStepReassignment[] = [];
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
		this.load().subscribe()
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes['itemStepId'] != null && (changes['itemStepId'].isFirstChange || (changes['itemStepId'].currentValue !== changes['itemStepId'].previousValue))) {
			this.load().subscribe();
		}
	}

	load(): Observable<any> {
		this.step = null;
		if (this.itemStepId != null) {
			this.isLoading = true;
			return this.workflowService.getWorkflowStepDetail(this.itemStepId)
				.pipe(
					map((r) => {
						this.isLoading = false;
						this.step = r;
						this.reassignments = [];
						this.activityType = this.getActivityType(this.step);
						if (this.step.ItemFields != null && this.step.ItemFields.Reassigned != null) {
							for (const element of this.step.ItemFields.Reassigned) {
								this.reassignments.push(new WorkflowStepReassignment(element));
							}
						}
						this.ref.markForCheck();
					})
				);
		} else {
			return of();
		}
	}


	get bulkReassignments(): WorkflowStepReassignment[] {
		return this.reassignments.filter((r: WorkflowStepReassignment) => r.IsBulkReassignment);
	}

	get reassignment(): null | WorkflowStepReassignment {
		if (this.reassignments == null || this.reassignments.length < 1) {
			return null;
		} else if (this.reassignments.length === 1 && !this.reassignments[0].IsBulkReassignment) {
			return this.reassignments[0];
		} else if (this.reassignments.length > 1) {
			return this.reassignments.find((r: WorkflowStepReassignment) => !r.IsBulkReassignment);
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
}