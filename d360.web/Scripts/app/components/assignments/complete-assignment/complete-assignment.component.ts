import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import {
	AssignmentItemStep,
	FormRequest,
	WorkflowForm,
	WorkflowFormField,
	WorkflowFormFieldType
} from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';
import { Subscription } from 'rxjs';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { SidePanelSwitcherComponent } from '../side-panel-switcher/side-panel-switcher.component';
import { NgForm } from '@angular/forms';
import { AssignmentService } from '../assignment.service';

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
	workflowName:string;
	stepUid: string;
	assetId: number;
	sidePanelStorageKey: string = 'CompleteAssignment_' + this.settingsService.CurrentResourceID;
	sidePanel: string = 'asset-details';
	formTitle: string = '';
	formDescription: string = '';
	assetName: string = '';
	formFields: WorkflowFormField[] = [];
	assignmentItemStep: AssignmentItemStep;
	request: FormRequest;
	discardForm: boolean;
	@ViewChild('sidePanelSwitcherComponent') sidePanelSwitcherComponent: SidePanelSwitcherComponent;
	@ViewChild('workflowForm') public workflowForm: NgForm;

	private linkInterceptorSubscription: Subscription;
	@ViewChild('form', { static: false }) formElement: ElementRef;

	constructor(protected settingsService: CompanySettingsService,
				private workflowService: WorkflowService,
				private linkClickInterceptor: LinkClickInterceptor,
				private assignmentService: AssignmentService
	) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.isAssignmentProgressSelected = false;
	}

	onFormInput(message) {
		this.discardForm = message;
	}


	openModal(details: {
		workflowItemUid: string,
		stepUid: string,
		assetId: number
	},selectedWorkflowItems?): void {
		this.workflowName=selectedWorkflowItems[0]?.workflowName
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
		this.sidePanelSwitcherComponent.clear();

	}

	getFormDetails(): void {
		this.isLoading = true;
		this.workflowService.getWorkflowFormByUid(this.workflowItemUid, this.stepUid)
			.subscribe((res: WorkflowForm) => {
				this.isLoading = false;
				if (res) {
					this.formTitle = res.Title;
					this.formDescription = res.Description;
					this.formFields = res.Fields;
					this.request = res.Request;
					this.assetName = res.ObjectName;
					this.assignmentService.setFormValidators.next();
				}
			});
	}

	showAssignmentProgress(): void {
		this.isAssignmentProgressSelected = true;
		this.modalTitle = 'Assignment Progress and Information';
		this.sidePanelSwitcherComponent.clear();
	}

	discardFormFunc() {
		this.workflowForm.reset();
	}

	onFormSubmit(): void {
		this.prepareValuesForSubmit();

		//save form values with stepUid and itemUid
		this.workflowService.submitWorkflowFormByUid(this.workflowItemUid, this.stepUid, this.formFields).subscribe();
	}

	stepClickChanged(assignmentItemStep: AssignmentItemStep): void {
		this.sidePanel = 'step-details';
		this.assignmentItemStep = assignmentItemStep;
	}

	closeModal(): void {
		this.isModalVisible = false;
		this.linkInterceptorSubscription?.unsubscribe();
	}

	onClickResource(event: MouseEvent): void {
		if (this.request?.Action) {
			this.linkClickInterceptor.sendEvent(event, {
				ResourceUid: this.request.Action.CreatedBy
			}, 'users/' + this.request.Action.CreatedBy);
		}
	}

	onClickAsset(event: MouseEvent): void {
		if (this.assetId) {
			this.linkClickInterceptor.sendEvent(event, {
				AssetId: this.assetId
			}, 'asset/' + this.assetId);
		}
	}

	public prepareValuesForSubmit(): void {
		this.formFields.forEach((x, i) => {
			if (x.FieldType === WorkflowFormFieldType.Link) {

				const name = this.workflowForm.form.controls[`inputName_${i}`].value;
				const url = this.workflowForm.form.controls[`inputUrl_${i}`].value;
				x.Value =
					name.length + url.length === 0 ? '' : name + '|' + url;
			} else if (Array.isArray(x.Value)) {
				x.Value = x.Value.join();
			}
		});
	}

}
