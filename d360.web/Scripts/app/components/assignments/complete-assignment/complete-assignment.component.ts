import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import {
	AssignmentByVersion,
	AssignmentItem,
	AssignmentItemStep,
	FormRequest,
	WorkflowForm,
	WorkflowFormField,
	WorkflowFormFieldType
} from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';
import { forkJoin, Subscription } from 'rxjs';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { SidePanelSwitcherComponent } from '../side-panel-switcher/side-panel-switcher.component';
import { NgForm } from '@angular/forms';
import { AssignmentService } from '../assignment.service';
import { ResourcesService } from '../../../services/resources.service';

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less']
})
export class CompleteAssignmentComponent extends BaseComponent implements OnInit, OnDestroy {
	isModalVisible: boolean = false;
	loading: boolean = false;
	isAssignmentProgressSelected: boolean = false;
	modalTitle: string = 'Assignment';
	sidePanelOpen: boolean = false;
	workflowItemUid: string;
	workflowName: string;
	stepUid: string;
	sidePanelStorageKey: string =
		'CompleteAssignment_' + this.settingsService.CurrentResourceID;
	sidePanel: string = 'asset-details';
	formTitle: string = '';
	formDescription: string = '';
	assetName: string = '';
	assetId: number;
	formFields: WorkflowFormField[] = [];
	assignmentItemStep: AssignmentItemStep;
	request: FormRequest;
	discardForm: boolean;
	workflowTypeUid: string;
	workflowTypeVersion: number;
	reassignAvailableTypes = [];
	hasObjectReassign: boolean = false;
	radioSelectionValue: string;
	assets = [];
	userData = [];

	@ViewChild('sidePanelSwitcherComponent')
	sidePanelSwitcherComponent: SidePanelSwitcherComponent;
	@ViewChild('workflowForm') public workflowForm: NgForm;
	@ViewChild('form', { static: false }) formElement: ElementRef;

	private linkInterceptorSubscription: Subscription;
	private loadSub: Subscription;

	constructor(protected settingsService: CompanySettingsService,
				private workflowService: WorkflowService,
				private linkClickInterceptor: LinkClickInterceptor,
				private assignmentService: AssignmentService,
				private resourceService: ResourcesService
	) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.isAssignmentProgressSelected = false;
	}

	ngOnDestroy(): void {
		if (this.loadSub) {
			this.loadSub.unsubscribe();
		}
	}

	onFormInput(message): void {
		this.discardForm = message;
	}


	openModal(details: {
		workflowItemUid: string,
		stepUid: string
	}): void {
		if (details) {
			this.stepUid = details.stepUid;
			this.workflowItemUid = details.workflowItemUid;
			forkJoin(
				this.workflowService.getWorkflowFormByUid(
					this.workflowItemUid,
					this.stepUid
				),
				this.workflowService.getAssignmentsByVersion(
					1,
					1,
					null,
					null,
					null,
					null,
					this.workflowItemUid
				),
				this.workflowService.getAssignmentItem(this.workflowItemUid)
			).subscribe((res) => {
				if (res[0]) {
					this.formTitle = res[0].Title;
					this.formDescription = res[0].Description;
					this.formFields = res[0].Fields;
					this.request = res[0].Request;
					if (res[0].IssueObjectID) {
						this.assetName = res[0].IssueObjectName;
						this.assetId = res[0].IssueObjectID;
					} else {
						this.assetName = res[0].ObjectName;
						this.assetId = res[0].ObjectID;
					}
					if (res[0].AllowReassignObject) {
						this.reassignAvailableTypes.push({
							value: "object",
							text: "Object",
						});
					}
					if (res[0].AllowReassignResource) {
						this.reassignAvailableTypes.push({
							value: "resource",
							text: "Resource",
						});
					}

					this.hasObjectReassign =
						this.reassignAvailableTypes.length > 0;
					if (this.hasObjectReassign) {
						this.getWorkflowReassignmentAssets();
						this.getAllUsersData();
					}
					this.assignmentService.setFormValidators.next();
				}
				if (res[1]?.items?.length > 0) {
					this.workflowTypeUid = res[1].items[0].WorkflowTypeUid;
					this.workflowTypeVersion = res[1].items[0].Version;
				}
				this.workflowName = res[2].WorkflowName;
			});
		}
		this.linkInterceptorSubscription = this.linkClickInterceptor
			.getEvents()
			.subscribe((ev) => {
				this.linkClickInterceptor.handleEvent(
					this.sidePanelSwitcherComponent,
					ev
				);
				this.sidePanelOpen = true;
			});
		this.isModalVisible = true;
		this.radioSelectionValue = 'completeForm';
	}

	showAssignment(): void {
		this.isAssignmentProgressSelected = false;
		this.modalTitle = 'Assignment';
		this.sidePanelSwitcherComponent.clear();
	}

	showAssignmentProgress(): void {
		this.isAssignmentProgressSelected = true;
		this.modalTitle = 'Assignment Progress and Information';
		this.sidePanelSwitcherComponent.clear();
	}

	discardFormFunc(): void {
		this.workflowForm.reset();
	}

	onFormSubmit(): void {
		this.prepareValuesForSubmit();

		//save form values with stepUid and itemUid
		this.workflowService
			.submitWorkflowFormByUid(
				this.workflowItemUid,
				this.stepUid,
				this.formFields
			)
			.subscribe();
	}

	stepClickChanged(assignmentItemStep: AssignmentItemStep): void {
		this.sidePanel = 'step-details';
		this.assignmentItemStep = assignmentItemStep;
	}

	closeModal(): void {
		this.isModalVisible = false;
		this.radioSelectionValue = '';
		this.linkInterceptorSubscription?.unsubscribe();
	}

	onClickResource(event: MouseEvent, resourceId: number): void {
		this.linkClickInterceptor.sendEvent(
			event,
			{
				ResourceID: resourceId
			},
			'users/' + resourceId
		);
	}

	onClickAsset(event: MouseEvent, assetId: number): void {
		if (this.assetId) {
			this.linkClickInterceptor.sendEvent(
				event,
				{
					AssetId: assetId
				},
				'asset/' + assetId
			);
		}
	}

	showRequestSidePanel(event: MouseEvent): void {
		if (this.request) {
			this.linkClickInterceptor.sendEvent(
				event,
				{
					workflowActionUid: this.request.Action.Uid,
					itemStepUid: this.stepUid,
					workflowItemUid: this.workflowItemUid
				},
				''
			);
		}
	}

	prepareValuesForSubmit(): void {
		this.formFields.forEach((x, i) => {
			if (x.FieldType === WorkflowFormFieldType.Link) {
				const name =
					this.workflowForm.form.controls[`inputName_${i}`].value;
				const url =
					this.workflowForm.form.controls[`inputUrl_${i}`].value;
				x.Value =
					name.length + url.length === 0 ? '' : name + '|' + url;
			} else if (Array.isArray(x.Value)) {
				x.Value = x.Value.join();
			}
		});
	}

	private loadFormDetails(): void {
		this.isLoading = true;
		this.workflowService
			.getWorkflowFormByUid(this.workflowItemUid, this.stepUid)
			.subscribe((res: WorkflowForm) => {
				this.isLoading = false;
				if (res) {
					this.formTitle = res.Title;
					this.formDescription = res.Description;
					this.formFields = res.Fields;
					this.request = res.Request;
					if (res.IssueObjectID) {
						this.assetName = res.IssueObjectName;
						this.assetId = res.IssueObjectID;
					} else {
						this.assetName = res.ObjectName;
						this.assetId = res.ObjectID;
					}
					if (res.AllowReassignObject) {
						this.reassignAvailableTypes.push({
							value: 'object',
							text: 'Object'
						});
					}
					if (res.AllowReassignResource) {
						this.reassignAvailableTypes.push({
							value: 'resource',
							text: 'Resource'
						});
					}

					this.hasObjectReassign =
						this.reassignAvailableTypes.length > 0;
					if (this.hasObjectReassign) {
						this.getWorkflowReassignmentAssets();
						this.getAllUsersData();
					}
					this.assignmentService.setFormValidators.next();
				}
			});
	}

	private loadWorkflowTypeDetails(): void {
		this.workflowService
			.getAssignmentsByVersion(
				1,
				1,
				null,
				null,
				null,
				null,
				this.workflowItemUid
			)
			.subscribe((response: AssignmentByVersion): void => {
				if (response?.items?.length > 0) {
					this.workflowTypeUid = response.items[0].WorkflowTypeUid;
					this.workflowTypeVersion = response.items[0].Version;
				}
			});
	}

	getWorkFlowData(): void {
		this.workflowService
			.getAssignmentItem(this.workflowItemUid)
			.subscribe((res: AssignmentItem): void => {
				this.workflowName = res.WorkflowName;
			});
	}

	getWorkflowReassignmentAssets(): void {
		this.workflowService
			.getWorkflowReassignmentAssetsByUid(this.workflowItemUid)
			.subscribe((result) => {
				this.assets = result;
			});
	}

	getAllUsersData(): void {
		this.resourceService.getResources(false).subscribe((res) => {
			this.userData = res;
		});
	}

	protected readonly Number = Number;
}
