import { Component, OnInit, ViewChild } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import { AssignmentItemStep, FormRequest, WorkflowForm, WorkflowFormField } from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';
import { WorkflowFormFieldsComponent } from '../../workflow/workflow-form-fields.component';
import { Subscription } from 'rxjs';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { SidePanelSwitcherComponent } from '../side-panel-switcher/side-panel-switcher.component';

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less']
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
	request: FormRequest;
	@ViewChild('fieldsComponent', { static: false }) fieldsComponent: WorkflowFormFieldsComponent;
	@ViewChild('sidePanelSwitcherComponent') sidePanelSwitcherComponent: SidePanelSwitcherComponent;
	private linkInterceptorSubscription: Subscription;

	constructor(protected settingsService: CompanySettingsService,
				private workflowService: WorkflowService,
				private linkClickInterceptor: LinkClickInterceptor) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.isAssignmentProgressSelected = false;
	}

	openModal(details: {
		workflowItemUid: string,
		stepUid: string,
		assetId: number
	}): void {
		if (details) {
			this.assetId = details.assetId;
			this.stepUid = details.stepUid;
			this.workflowItemUid = details.workflowItemUid;
			this.getFormDetails();
		}
		this.linkInterceptorSubscription = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			this.linkClickInterceptor.handleEvent(this.sidePanelSwitcherComponent, ev);
			this.sidePanelOpen = true;
		});
		this.isModalVisible = true;
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
				this.request = res.Request;
				this.fieldsComponent.setValidators();
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

		//save form values with stepUid and itemUid
		this.workflowService.submitWorkflowFormByUid(this.workflowItemUid, this.stepUid, this.formFields).subscribe();
	}

	stepClickChanged(assignmentItemStep: AssignmentItemStep): void {
		this.sidePanel = 'step-details';
		this.assignmentItemStep = assignmentItemStep;
	}

	closeModal(): void {
		this.isModalVisible = false;
		this.linkInterceptorSubscription?.unsubscribe()
	}

	onClickResource(event: MouseEvent): void {
		if(this.request?.Action) {
			this.linkClickInterceptor.sendEvent(event, {
				ResourceUid: this.request.Action.CreatedBy
			}, 'users/' + this.request.Action.CreatedBy);
		}
	}

}
