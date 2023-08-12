import {
	ChangeDetectionStrategy,
	ChangeDetectorRef,
	Component,
	ElementRef,
	EventEmitter,
	OnDestroy,
	OnInit,
	Output,
	ViewChild
} from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import {
	AssignmentItem,
	AssignmentItemStep,
	FormRequest,
	SingleAssignment,
	WorkflowFormField,
	WorkflowFormFieldType,
	WorkflowFormResponse
} from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';
import { forkJoin, Observable, Subscription } from 'rxjs';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { SidePanelSwitcherComponent } from '../side-panel-switcher/side-panel-switcher.component';
import { NgForm } from '@angular/forms';
import { AssignmentService } from '../assignment.service';
import { ResourcesService } from '../../../services/resources.service';
import { D3SModal } from '../../shared/modal/gov-modal.component';

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

	@Output() onModalClose = new EventEmitter<{ isBack: boolean }>();

	@ViewChild('sidePanelSwitcherComponent') sidePanelSwitcherComponent: SidePanelSwitcherComponent;
	reassignAvailableTypes = [];
	hasObjectReassign: boolean = false;
	isLoading: boolean = false;
	radioSelectionValue: string;
	assets = [];
	userData = [];
	tableRadioSelection;

	@ViewChild('sidePanelSwitcherComponent')
	@ViewChild('workflowForm') public workflowForm: NgForm;
	@ViewChild('form', { static: false }) formElement: ElementRef;
	@ViewChild('modal', { static: false }) modal: D3SModal;

	multiSubmitionItems: SingleAssignment[] = [];
	isBulkRespond: boolean = false;
	private linkInterceptorSubscription: Subscription;
	private loadSub: Subscription;

	constructor(protected settingsService: CompanySettingsService,
				private workflowService: WorkflowService,
				private linkClickInterceptor: LinkClickInterceptor,
				private assignmentService: AssignmentService,
				private cdRef: ChangeDetectorRef,
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
						this.cdRef.markForCheck();
					});
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
				this.isLoading = false;
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
							value: 'object',
							text: 'Object'
						});
					}
					if (res[0].AllowReassignResource) {
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
		this.cdRef.markForCheck();
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

	onBack(): void {
		this.workflowForm.reset();
		this.onModalClose.emit({ isBack: true });
	}

	onCloseClick(): void {
		this.onModalClose.emit({ isBack: false });
	}

	onFormSubmit(): void {
		if (this.radioSelectionValue === 'completeForm') {
			this.prepareValuesForSubmit();
			this.workflowService
				.submitWorkflowFormByUid(
					this.workflowItemUid,
					this.stepUid,
					this.formFields
				)
				.subscribe();
		} else if (this.radioSelectionValue === 'reassignUser') {
			console.log(this.tableRadioSelection);
			this.workflowService.reassignWorkflowResourceByUid(this.stepUid, this.tableRadioSelection.ResourceID, false).subscribe((res) => {
				console.log(res);
			});
		} else if (this.radioSelectionValue === 'changeAsset') {
			this.workflowService.reassignWorkflowObjectByUid(this.workflowItemUid, this.workflowTypeUid, this.tableRadioSelection.ObjectID, this.tableRadioSelection.Object, this.stepUid)
				.subscribe((result) => {
					console.log(result);
				});
		}

		if (this.isMultiSubmition) {
			const obs: Observable<WorkflowFormResponse>[] = [];
			this.multiSubmitionItems.forEach((item) => {
				obs.push(this.workflowService.submitWorkflowFormByUid(item.WorkflowItemUid, item.ItemStepUid, this.formFields));
			});

			forkJoin(obs).subscribe(() => {
				this.closeModal();
				this.modal.closePopUp();
				this.onModalClose.emit({ isBack: false });
			});
		} else {
			//save form values with stepUid and itemUid
			this.workflowService.submitWorkflowFormByUid(this.workflowItemUid, this.stepUid, this.formFields).subscribe(() => {
				this.closeModal();
				this.modal.closePopUp();
				this.onModalClose.emit({ isBack: false });
			});
		}
	}

	stepClickChanged(assignmentItemStep: AssignmentItemStep): void {
		this.sidePanel = 'step-details';
		this.assignmentItemStep = assignmentItemStep;
	}

	closeModal(): void {
		this.isModalVisible = false;
		this.radioSelectionValue = '';
		this.linkInterceptorSubscription?.unsubscribe();
		this.cdRef.markForCheck();
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

	get isMultiSubmition(): boolean {
		return this.multiSubmitionItems.length > 1;
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
