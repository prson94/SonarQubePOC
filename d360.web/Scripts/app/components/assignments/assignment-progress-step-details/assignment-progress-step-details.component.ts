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
import { ResponsibilityType } from '../../../models/responsibility-type.model';
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
	responsibilities: ResponsibilityType[];
	fields: any[] = [];
	helper = WorkflowHelpers;
	private showAllAnyCondition: boolean = false;
	private isSatisfyAll: boolean = true;

	constructor(
		private responsibilityService: ResponsibilityTypeService,
		protected settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private ref: ChangeDetectorRef) {
		super(settingsService);
	}

	ngOnInit() {
		this.load()
			.pipe(
				map(() => {
					this.responsibilityService.getResponsibilityTypes().subscribe((r) => {
						this.responsibilities = r;
					});
				}),
				map(() => {
					if (this.step != null) {
						this.workflowService.getWorkflowFieldTypes(this.step.ObjectTypeID, this.step.ObjectType, true)
							.subscribe((r) => {
								this.fields = r;
							});
					}
				})
			).subscribe();
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
					}),
					map(() => {
						if (typeof this.step.Condition !== 'undefined' && typeof this.step.Condition.length !== 'undefined') {

							this.showAllAnyCondition = this.step.Condition.filter((x) => x['@FieldTypeID']).length > 1;
							this.isSatisfyAll = this.step.Condition.every((x) => x['@Connector'] === 'AND');
						}
					})
				);
		} else {
			return of();
		}
	}


	private get bulkReassignments() {
		return this.reassignments.filter((r) => r.IsBulkReassignment);
	}

	private get reassignment() {
		if (this.reassignments == null || this.reassignments.length < 1) {
			return null;
		} else if (this.reassignments.length === 1 && !this.reassignments[0].IsBulkReassignment) {
			return this.reassignments[0];
		} else if (this.reassignments.length > 1) {
			return this.reassignments.find((r) => !r.IsBulkReassignment);
		} else {
			return null;
		}
	}

	private close() {
		this.visible = false;
		this.visibleChange.emit(false);
		this.ref.markForCheck();
	}

	get reassignmentFieldName(): string {
		if (this.reassignment) {
			if (this.reassignment.ReassignType === 'Object') {
				return $localize`Action was reassigned to another object`;
			} else if (this.reassignment.ReassignType === 'Resource') {
				return $localize`Action is reassigned to Resource`;
			}
		}

		return $localize`Action was reassigned`;
	}

	getActivityType(step: any): string {
		if (step.ActivityType !== 0) {
			return this.helper.activityTypeName(step.ActivityType);
		} else {
			return this.helper.stepTypeName(step.StepType);
		}
	}
}