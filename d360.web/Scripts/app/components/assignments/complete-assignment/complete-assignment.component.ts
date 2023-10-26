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
import { ResourcesService } from '../../../services/resources.service';
import { D3SModal } from '../../shared/modal/gov-modal.component';
import { JsonResult } from '../../../models/jsonresult.model';
import { SidePanelButton } from '../../../models/side-panel.model';
import { AppConstants } from '../../../static/constants';
import { AuthenticationService } from '../../../services/authentication.service';
import { StringHelpers } from '../../../static/string-helpers';

/*global $localize*/

@Component({
	selector: 'd3s-complete-assignment',
	templateUrl: './complete-assignment.component.html',
	styleUrls: ['./complete-assignment.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class CompleteAssignmentComponent extends BaseComponent implements OnInit, OnDestroy {
	@Input() workflowName: string;
	@Input() showBackButton: boolean = false;

	isModalAvailable: boolean = false;
	loading: boolean = false;
	isAssignmentProgressSelected: boolean = false;
	modalTitle: string = 'Assignment';
	sidePanelOpen: boolean = false;
	isSidePanelPopulated: boolean = false;
	workflowItemUid: string;
	stepUid: string;
	sidePanelStorageKey: string =
		'CompleteAssignment_' + this.settingsService.CurrentResourceID;
	sidePanel: string = 'asset-details';
	formTitle: string = '';
	formDescription: string = '';
	formDescriptionRaw: string = '';
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

	@Output() onModalClose = new EventEmitter<{ isBack: boolean, removeSelected: boolean, action?: string }>();

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
	selectedAssignment: WorkflowUserGroupedAssignments;
	areAllMultiAssignmentsSelected: boolean;

	hideDialog: boolean = false;
	defaultPagingOptions: number[] = AppConstants.DEFAULT_PAGING_OPTIONS;
	rowsPerPage: number = 10;
	hasForm: boolean;
	isUserDataLoading: boolean = false;
	isReassignmentAssetsLoading: boolean = false;
	showAssignmentProgressOnly: boolean = false;

	private linkInterceptorSubscription: Subscription;
	private loadSub: Subscription;
	private storageKey: string = 'completeAssignmentRowsPerPage';
	isReassign: boolean = false;
	isAdmin: boolean = false;
	private receivedFormFields: WorkflowFormField[] = [];
	isType: boolean = false;
	private hideSubmitButton: boolean = false;

	constructor(protected settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private linkClickInterceptor: LinkClickInterceptor,
		private cdRef: ChangeDetectorRef,
		private resourceService: ResourcesService,
		private authenticationService: AuthenticationService
	) {
		super(settingsService);
		this.authenticationService.checkCurrentUserAdmin().subscribe((res) => { this.isAdmin = res; });
		this.subscribeSwitcherEvents();
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
		showAssignmentProgress?: boolean,
		areAllMultiAssignmentsSelected?: boolean,
		showBackButton?: boolean,
		isReassign?: boolean,
		resetSidePanel?: boolean
	}): void {
		if (details) {
			this.multiSubmitionItems = [];
			this.isBulkRespond = false;
			this.isReassign = details.isReassign;

			if (this.isReassign) {
				this.radioSelectionValue = 'reassignUser';
			} else {
				this.radioSelectionValue = 'completeForm';
			}
			this.showBackButton = details.showBackButton ?? false;
			this.hideSubmitButton = details.showAssignmentProgress ?? false;
			this.showAssignmentProgressOnly = details.showAssignmentProgress ?? false;

			this.stepUid = details.stepUid;
			this.workflowItemUid = details.workflowItemUid;
			this.selectedAssignment = details.selectedAssignment;
			this.areAllMultiAssignmentsSelected = details.areAllMultiAssignmentsSelected ?? true;

			if (details.items) {
				this.multiSubmitionItems = details.items;
				this.isBulkRespond = true;
			}

			if (details.resetSidePanel) {
				//if we are coming from email link lets keep side panel closed
				//use random storage key to avoid overwriting user preference for this page
				this.sidePanelStorageKey = StringHelpers.generateRandomString(20);
			}

			this.subscribeSwitcherEvents();
			this.isLoading = true;
			this.cdRef.detectChanges();
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
							this.formDescriptionRaw = res.DescriptionRaw;
							this.formFields = this.formFields?.length > 0 ? this.formFields : res.Fields;
							this.receivedFormFields = structuredClone(res.Fields);
							this.request = res.Request;
							
							if (res.IssueObjectID) {
								this.assetName = res.IssueObjectName;
								this.assetId = res.IssueObjectID;
								this.isType = res.IssueObject.toLowerCase().endsWith("type");
								if (!this.isType) {
									this.onClickAsset(new MouseEvent('click'), this.assetId.toString());
								} else {									
									this.isSidePanelPopulated = false;
									this.sidePanelOpen = false;
									if (this.sidePanelSwitcherComponent) {
										this.sidePanelSwitcherComponent.clear();										
									}
								}
								
							} else {
								this.assetName = res.ObjectName;
								this.assetId = res.ObjectID;								
							}							
							this.allowReassignObject = res.AllowReassignObject;
							this.allowReassignResource = res.AllowReassignResource || this.isAdmin;

							if (this.allowReassignObject) {
								this.loadWorkflowReassignmentAssets();
							}
							if (this.allowReassignResource || this.radioSelectionValue === 'reassignUser') {
								this.loadAllUsersData();
							}
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
							if (!this.assetName) {
								this.assetName = results[2].AssetPath;
							}

							const assetUid = results[2].AssetUid;
							if (assetUid && assetUid !== "" && assetUid !== "00000000-0000-0000-0000-000000000000") {
								this.onClickAsset(new MouseEvent('click'), assetUid);
							}							
						}
						this.isLoading = false;
						this.cdRef.markForCheck();
					});

			if (details.showAssignmentProgress) {
				this.showAssignmentProgress()
			}
		}
		if (this.isModalAvailable) {
			this.hideDialog = false;
		}
		else {
			this.isModalAvailable = true;
		}

		this.cdRef.markForCheck();
	}

	getAssetDetails() {
		this.workflowService.getAssignmentItem(this.workflowItemUid).subscribe((res) => {
			this.assetName = res?.AssetPath
		})
	}

	showAssignment(): void {
		this.isAssignmentProgressSelected = false;
		this.modalTitle = 'Assignment';
		this.sidePanelSwitcherComponent.clear();
	}

	showAssignmentProgress(): void {
		this.isAssignmentProgressSelected = true;
		this.modalTitle = 'Assignment Progress and Information';
		if (this.sidePanelSwitcherComponent) {
			this.sidePanelSwitcherComponent.clear();
			this.isSidePanelPopulated = false;
		}
	}

	discardFormFunc(): void {
		this.formFields = structuredClone(this.receivedFormFields);
		this.workflowForm.reset();
	}

	onBack(): void {
		this.hideDialog = true;
		this.linkInterceptorSubscription?.unsubscribe();
		this.onModalClose.emit({ isBack: true, removeSelected: false });
	}

	onCloseClick(): void {
		this.closeModal();
		this.onModalClose.emit({ isBack: false, removeSelected: false });
	}

	onFormSubmit(): void {
		if (this.isMultiSubmition) {
			const obs: Observable<WorkflowFormResponse | JsonResult>[] = [];
			let action = "";
			if (this.radioSelectionValue === 'completeForm') {
				this.prepareValuesForSubmit();
				this.multiSubmitionItems.forEach((item) => {
					obs.push(this.workflowService.submitWorkflowFormByUid(item.WorkflowItemUid, item.ItemStepUid, this.formFields));
				});
				action = "complete"
			} else if (this.radioSelectionValue === 'reassignUser') {
				this.multiSubmitionItems.forEach((item) => {
					obs.push(this.workflowService.reassignWorkflowResourceByUid(item.ItemStepUid, this.tableRadioSelection.Uid, this.clearOtherAssignments, this.sendFormEmails));
				});
				action = "reassign"
			} else if (this.radioSelectionValue === 'changeAsset') {
				this.multiSubmitionItems.forEach((item) => {
					obs.push(this.workflowService.reassignWorkflowObjectByUid(item.WorkflowItemUid, this.workflowTypeUid, this.tableRadioSelection.ObjectID, this.tableRadioSelection.Object, item.ItemStepUid));
				});
				action = "change"
			}
			this.isLoading = true;
			forkJoin(obs).subscribe(() => {
				this.onModalClose.emit({ isBack: this.areAllMultiAssignmentsSelected ? false : true, removeSelected: true, action: action });
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
					this.onModalClose.emit({ isBack: !this.areAllMultiAssignmentsSelected, removeSelected: true, action: "complete" });
				});
			} else if (this.radioSelectionValue === 'reassignUser') {
				this.isLoading = true;
				this.workflowService.reassignWorkflowResourceByUid(this.stepUid, this.tableRadioSelection.Uid, this.clearOtherAssignments, this.sendFormEmails).subscribe((): void => {
					this.closeModal();
					this.modal.closePopUp();
					this.onModalClose.emit({ isBack: !this.areAllMultiAssignmentsSelected, removeSelected: true, action: "reassign" });

				});
			} else if (this.radioSelectionValue === 'changeAsset') {
				this.isLoading = true;
				this.workflowService.reassignWorkflowObjectByUid(this.workflowItemUid, this.workflowTypeUid, this.tableRadioSelection.ObjectID, this.tableRadioSelection.Object, this.stepUid)
					.subscribe(() => {
						this.closeModal();
						this.modal.closePopUp();
						this.onModalClose.emit({ isBack: !this.areAllMultiAssignmentsSelected, removeSelected: true, action: "change" });
					});
			}

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
		this.formFields = [];
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

	onClickAsset(event: MouseEvent, identifier: string): void {
		if (identifier) {
			if (!this.isType) {			
				this.linkClickInterceptor.sendEvent(
					event,
					{
						AssetUid: identifier
					},
					'asset/' + identifier
				);				
			}
			
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
					this.workflowForm.form.controls[`input_${i}`].value;
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
		this.isReassignmentAssetsLoading = true;
		this.workflowService
			.getWorkflowReassignmentAssetsByUid(this.workflowItemUid)
			.subscribe((result) => {
				this.assets = result;
				this.isReassignmentAssetsLoading = false;
				this.cdRef.markForCheck();
			});
	}

	loadAllUsersData(): void {
		this.isUserDataLoading = true;
		this.resourceService.getResources(false).subscribe((res) => {
			this.userData = res;
			this.isUserDataLoading = false;
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

	subscribeSwitcherEvents() {
		this.linkInterceptorSubscription = this.linkClickInterceptor
			.getEvents()
			.subscribe((ev) => {
				this.linkClickInterceptor.handleEvent(
					this.sidePanelSwitcherComponent,
					ev
				);
				this.isSidePanelPopulated = true;
				this.sidePanelOpen = true;
			});
	}
	protected readonly Number = Number;
}
