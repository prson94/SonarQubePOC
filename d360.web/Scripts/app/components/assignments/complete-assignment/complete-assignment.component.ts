import {
	ChangeDetectionStrategy,
	ChangeDetectorRef,
	Component,
	ElementRef,
	EventEmitter,
	Input,
	OnDestroy,
	OnInit,
	Output,
	ViewChild
} from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { CompanySettingsService } from '../../../services/settings.service';
import {
	AssignmentItemStep,
	FormRequest,
	SingleAssignment,
	WorkflowFormField,
	WorkflowFormFieldType,
	WorkflowFormResponse,
	WorkflowUserGroupedAssignments,
} from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';
import { forkJoin, Observable, Subscription } from 'rxjs';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { SidePanelSwitcherComponent } from '../side-panel-switcher/side-panel-switcher.component';
import { NgForm } from '@angular/forms';
import { AssignmentService } from '../assignment.service';
import { ResourcesService } from '../../../services/resources.service';
import { D3SModal } from '../../shared/modal/gov-modal.component';
import { JsonResult } from '../../../models/jsonresult.model';
import { SidePanelButton } from '../../../models/side-panel.model';
import { AppConstants } from '../../../static/constants';

/*global $localize*/

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class CompleteAssignmentComponent extends BaseComponent implements OnInit, OnDestroy {
	@Input() onlyAdminReassignMode: boolean = false;

	isModalAvailable: boolean = false;
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
	sidePanelButtons: SidePanelButton[] = [new SidePanelButton({
		label: $localize`Information`,
		tooltip: $localize`Information`,
		disabledTooltip: null,
		nothingSelectedMessage: $localize`Select an item to display its information`,
		notApplicableMessage: $localize`Information data is not available for the selected item`,
		multipleSelectedMessage: $localize`Select a single item to display it’s information`,
		key: 'information',
		icon: 'fa-info-circle',
		disabled: false,
		visible: true,
		needsSelection: true
	})];

	@Output() onModalClose = new EventEmitter<{ isBack: boolean, isCompleteForm: boolean }>();

	@ViewChild('sidePanelSwitcherComponent') sidePanelSwitcherComponent: SidePanelSwitcherComponent;
	isLoading: boolean = false;
	radioSelectionValue: string;
	assets = [];
	userData = [];
	tableRadioSelection;
	@ViewChild('workflowForm') public workflowForm: NgForm;
	@ViewChild('form', { static: false }) formElement: ElementRef;
	@ViewChild('modal', { static: false }) modal: D3SModal;

	multiSubmitionItems: SingleAssignment[] = [];
	isBulkRespond: boolean = false;
	allowReassignObject: boolean = false;
	allowReassignResource: boolean = false;
	clearOtherAssignments: boolean = false;
	sendFormEmails: boolean = true;
	selectedAssignment: WorkflowUserGroupedAssignments
	hideDialog: boolean = false;
	defaultPagingOptions: number[] = AppConstants.DEFAULT_PAGING_OPTIONS;
	rowsPerPage: number = 10;

	hasForm: boolean;

	private linkInterceptorSubscription: Subscription;
	private loadSub: Subscription;
	private storageKey: string = 'completeAssignmentRowsPerPage';

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
		this.loadRowsPerPage();
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
		assetId?: number,
		items?: SingleAssignment[]
		selectedAssignment?: WorkflowUserGroupedAssignments,
		showAssignmentProgress?: boolean
	}): void {
		if (details) {
			this.multiSubmitionItems = [];
			this.isBulkRespond = false;
			if (this.onlyAdminReassignMode) {
				this.radioSelectionValue = 'reassignUser';
			} else {
				this.radioSelectionValue = 'completeForm';
			}

			this.stepUid = details.stepUid;
			this.workflowItemUid = details.workflowItemUid;
			this.selectedAssignment = details.selectedAssignment;
			if (details.items) {
				this.multiSubmitionItems = details.items;
				this.isBulkRespond = true;
			}
			if (this.isModalAvailable) {
				this.hideDialog = false;
				return;
			}
			if (this.onlyAdminReassignMode) {
				this.radioSelectionValue = 'reassignUser';
			} else {
				this.radioSelectionValue = 'completeForm';

			}
			this.stepUid = details.stepUid;

			this.workflowItemUid = details.workflowItemUid;

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
						this.hasForm = false;
						if (results[0]) {
							this.hasForm = true;
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
							this.allowReassignObject = res.AllowReassignObject;
							this.allowReassignResource = res.AllowReassignResource;

							if (this.allowReassignObject) {
								this.loadWorkflowReassignmentAssets();
							}
							if (this.allowReassignResource) {
								this.loadAllUsersData();
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

			if (details.showAssignmentProgress) {
				this.showAssignmentProgress()
			}
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
		this.isModalAvailable = true;
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

	onBack(): void {
		this.hideDialog = true;
		this.onModalClose.emit({ isBack: true, isCompleteForm: true });
	}

	onCloseClick(): void {
		this.closeModal();
		this.onModalClose.emit({ isBack: false, isCompleteForm: true });
	}

	onFormSubmit(): void {
		if (this.isMultiSubmition) {
			const obs: Observable<WorkflowFormResponse | JsonResult>[] = [];
			let isBack: boolean = false;

			if (this.radioSelectionValue === 'completeForm') {
				this.prepareValuesForSubmit();
				this.multiSubmitionItems.forEach((item) => {
					obs.push(this.workflowService.submitWorkflowFormByUid(item.WorkflowItemUid, item.ItemStepUid, this.formFields));
				});

			} else if (this.radioSelectionValue === 'reassignUser') {
				isBack = true;
				this.multiSubmitionItems.forEach((item) => {
					obs.push(this.workflowService.reassignWorkflowResourceByUid(item.ItemStepUid, this.tableRadioSelection.Uid, this.clearOtherAssignments, this.sendFormEmails));
				});
			} else if (this.radioSelectionValue === 'changeAsset') {
				this.multiSubmitionItems.forEach((item) => {
					isBack = true;
					obs.push(this.workflowService.reassignWorkflowObjectByUid(item.WorkflowItemUid, this.workflowTypeUid, this.tableRadioSelection.ObjectID, this.tableRadioSelection.Object, item.ItemStepUid));
				});
			}
			this.isLoading = true;
			forkJoin(obs).subscribe(() => {
				this.onModalClose.emit({ isBack, isCompleteForm });
				this.closeModal();
				this.modal.closePopUp();
				this.isLoading = false;
			});
		} else {
			//save form values with stepUid and itemUid
			if (this.radioSelectionValue === 'completeForm') {
				this.isLoading = true;
				this.prepareValuesForSubmit();
				this.workflowService.submitWorkflowFormByUid(this.workflowItemUid, this.stepUid, this.formFields).subscribe(() => {
					this.closeModal();
					this.modal.closePopUp();
				});
			} else if (this.radioSelectionValue === 'reassignUser') {
				this.isLoading = true;
				this.workflowService.reassignWorkflowResourceByUid(this.stepUid, this.tableRadioSelection.Uid, this.clearOtherAssignments, this.sendFormEmails).subscribe((): void => {
					this.closeModal();
					this.modal.closePopUp();

				});
			} else if (this.radioSelectionValue === 'changeAsset') {
				this.isLoading = true;
				this.workflowService.reassignWorkflowObjectByUid(this.workflowItemUid, this.workflowTypeUid, this.tableRadioSelection.ObjectID, this.tableRadioSelection.Object, this.stepUid)
					.subscribe(() => {
						this.closeModal();
						this.modal.closePopUp();
					});
			}

			// Common function to close the modal
			this.workflowService.getUserAssignments(this.settingsService.CurrentResourceUid)
				.subscribe((res) => {
					this.isLoading = false;

					const matchedAssignment = res.find((ele) =>
						ele.WorkflowTypeUid === this.selectedAssignment?.WorkflowTypeUid && ele.Version === this.selectedAssignment?.Version
					);

					let count = 0;
					if (matchedAssignment) {
						count = matchedAssignment.Count;
					}
					this.onModalClose.emit({ isBack: count > 0 ? true : false, isCompleteForm });
				});
		}
	}

	stepClickChanged(assignmentItemStep: AssignmentItemStep): void {
		this.sidePanel = 'step-details';
		this.assignmentItemStep = assignmentItemStep;
	}

	closeModal(): void {
		this.isModalAvailable = false;
		this.hideDialog = false;
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

	loadWorkflowReassignmentAssets(): void {
		this.workflowService
			.getWorkflowReassignmentAssetsByUid(this.workflowItemUid)
			.subscribe((result) => {
				this.assets = result;
				this.cdRef.markForCheck();
			});
	}

	loadAllUsersData(): void {
		this.resourceService.getResources(false).subscribe((res) => {
			this.userData = res;
			this.cdRef.markForCheck();
		});
	}

	setPanelHeader(event: string): void {
		this.sidePanelButtons[0].label = event;
		this.sidePanelButtons[0].tooltip = event;
		this.cdRef.markForCheck();
	}

	loadRowsPerPage(): void {
		this.rowsPerPage = Number(localStorage.getItem(this.storageKey)) || 10;
	}

	setRowsPerPage(event): void {
		if (event?.rows) {
			localStorage.setItem(this.storageKey, event.rows);
			this.rowsPerPage = event?.rows;
		}
	}

	protected readonly Number = Number;
}
