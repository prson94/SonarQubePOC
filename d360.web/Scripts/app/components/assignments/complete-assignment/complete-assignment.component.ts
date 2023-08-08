import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, ViewChild } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import { AssignmentItemStep, SingleAssignment, WorkflowForm, WorkflowFormField } from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowFormFieldsComponent } from '../../workflow/workflow-form-fields.component';

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class CompleteAssignmentComponent extends BaseComponent implements OnInit {

	isModalVisible: boolean = false;
	loading: boolean = false;
	isAssignmentProgressSelected: boolean = false;
	modalTitle: string = 'Assignment';
	sidePanelOpen: boolean = false;
	workflowItemUid: string;
	stepUid: string;
	assetId: number;
	sidePanelStorageKey: string = 'CompleteAssignment_' + this.settingsService.CurrentResourceID;
	sidePanel: string = 'asset-details';
	formTitle: string = '';
	formDescription: string = '';
	formFields: WorkflowFormField[] = [];
	assignmentItemStep: AssignmentItemStep;
	@ViewChild('fieldsComponent', { static: false }) fieldsComponent: WorkflowFormFieldsComponent;

	multiSubmitionItems: SingleAssignment[] = [];
	isBulkRespond: boolean = false;

	constructor(protected settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private cdRef: ChangeDetectorRef
	) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.isAssignmentProgressSelected = false;
	}

	openModal(details: {
		workflowItemUid: string,
		stepUid: string,
		assetId: number,
		items?: SingleAssignment[]
	}): void {
		if (details) {
			this.assetId = details.assetId;
			this.stepUid = details.stepUid;
			this.workflowItemUid = details.workflowItemUid;
			if (details.items) {
				this.multiSubmitionItems = details.items;
				this.isBulkRespond = true;
			}
			else {
				this.multiSubmitionItems = [];
				this.isBulkRespond = false;
			}
			this.getFormDetails();
		}
		this.isModalVisible = true;
		this.cdRef.markForCheck();
	}

	showAssignment(): void {
		this.isAssignmentProgressSelected = false;
		this.modalTitle = 'Assignment';
	}

	getFormDetails(): void {
		this.isLoading = true;
		this.workflowService.getWorkflowFormByUid(this.workflowItemUid, this.stepUid)
			.subscribe((res: WorkflowForm) => {
				this.formTitle = res.Title;
				this.formDescription = res.Description;
				this.formFields = res.Fields;
				this.isLoading = false;
				this.cdRef.markForCheck();
				setTimeout(() => {
					this.fieldsComponent.setValidators();
				}, 100);
			});
	}

	showAssignmentProgress(): void {
		this.isAssignmentProgressSelected = true;
		this.modalTitle = 'Assignment Progress and Information';
	}

	onFormSubmit(): void {
		if (this.fieldsComponent.setValidators()) {
			return;
		}
		this.fieldsComponent.prepareValuesForSubmit();

		if (this.isMultiSubmition) {
			this.multiSubmitionItems.forEach((item) => {
				this.workflowService.submitWorkflowFormByUid(item.WorkflowItemUid, item.ItemStepUid, this.formFields).subscribe();
			});
		}
		else {
			//save form values with stepUid and itemUid
			this.workflowService.submitWorkflowFormByUid(this.workflowItemUid, this.stepUid, this.formFields).subscribe();
		}
	}

	stepClickChanged(assignmentItemStep: AssignmentItemStep): void {
		this.sidePanel = 'step-details';
		this.assignmentItemStep = assignmentItemStep;
	}

	get isMultiSubmition(): boolean {
		return this.multiSubmitionItems.length > 1;
	}
}
