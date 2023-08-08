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
import { result } from 'lodash-es';

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
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
	sidePanelStorageKey: string = 'CompleteAssignment_' + this.settingsService.CurrentResourceID;
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
	@ViewChild('sidePanelSwitcherComponent') sidePanelSwitcherComponent: SidePanelSwitcherComponent;
	@ViewChild('workflowForm') public workflowForm: NgForm;
	@ViewChild('form', { static: false }) formElement: ElementRef;

	multiSubmitionItems: SingleAssignment[] = [];
	isBulkRespond: boolean = false;
	private linkInterceptorSubscription: Subscription;
	private loadSub: Subscription;

	constructor(protected settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private linkClickInterceptor: LinkClickInterceptor,
		private assignmentService: AssignmentService,
		private cdRef: ChangeDetectorRef
	) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.isAssignmentProgressSelected = false;
	}

	ngOnDestroy() {
		if (this.loadSub) {
			this.loadSub.unsubscribe();
		}
	}

	onFormInput(message): void {
		this.discardForm = message;
	}

	openModal(details: {
		workflowItemUid: string,
		stepUid: string,
		assetId: number,
		items?: SingleAssignment[]
	}): void {
		if (details) {
			this.stepUid = details.stepUid;
			this.workflowItemUid = details.workflowItemUid;

			if (details.items) {
				this.multiSubmitionItems = details.items;
				this.isBulkRespond = true;
			}

			this.isLoading = true;
			if (this.loadSub) {
				this.loadSub.unsubscribe();
			}
			this.loadSub =
				forkJoin(
					this.workflowService.getWorkflowFormByUid(this.workflowItemUid, this.stepUid),
					this.workflowService.getAssignmentsByVersion(1, 1, null, null, null, null, this.workflowItemUid),
					this.workflowService.getAssignmentItem(this.workflowItemUid))
					.subscribe((results) => {
						if (results[0]) {
							const res = results[0];
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
							this.assignmentService.setFormValidators.next();
						}

						if (results[1]) {
							const assignmentResponse = results[1];
							if (assignmentResponse?.items?.length > 0) {
								this.workflowTypeUid = assignmentResponse.items[0].WorkflowTypeUid;
								this.workflowTypeVersion = assignmentResponse.items[0].Version;
							}
						}
						if (results[2]) {
							this.workflowName = results[2].WorkflowName;
						}

						this.isLoading = false;
					});
		}
		this.linkInterceptorSubscription = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			this.linkClickInterceptor.handleEvent(this.sidePanelSwitcherComponent, ev);
			this.sidePanelOpen = true;
		});
		this.isModalVisible = true;
		this.cdRef.markForCheck();
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

	showRequestSidePanel(event: MouseEvent): void {
		if (this.request) {
			this.linkClickInterceptor.sendEvent(event, {
				workflowActionUid: this.request.Action.Uid,
				itemStepUid: this.stepUid,
				workflowItemUid: this.workflowItemUid
			}, '');
		}
	}

	prepareValuesForSubmit(): void {
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

	get isMultiSubmition(): boolean {
		return this.multiSubmitionItems.length > 1;
	}
}
