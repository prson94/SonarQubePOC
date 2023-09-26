import {
	ChangeDetectionStrategy,
	ChangeDetectorRef,
	Component,
	ElementRef,
	EventEmitter,
	Input,
	OnDestroy,
	Output,
	ViewChild,
	ViewEncapsulation
} from '@angular/core';
import { Table } from 'primeng/table';
import { AssignmentSelection, SingleAssignment, WorkflowUserGroupedAssignments } from '../../../models/workflow.model';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { SidePanelService } from '../../../services/side-panel.service';
import { WorkflowService } from '../../../services/workflow.service';
import { D3SModal } from '../../shared/modal/gov-modal.component';
import { AppConstants } from '../../../static/constants';
import { Subscription } from 'rxjs';
import { SidePanelSwitcherComponent } from '../side-panel-switcher/side-panel-switcher.component';
import { SidePanelButton } from '../../../models/side-panel.model';

@Component({
	selector: 'd3s-assignments-multi-picker',
	templateUrl: './assignment-multi-picker.component.html',
	styleUrls: ['./assignment-multi-picker.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})
export class AssignmentsMultiPickerComponent implements OnDestroy {
	@Output() onAssignmentSelection = new EventEmitter<AssignmentSelection>();
	@Input() onlyAdminReassignMode: boolean = false;
	@Output() onModalClose: EventEmitter<void> = new EventEmitter<void>();

	assignmentAssetTypeName: string = null;
	workflowTypeName: string;

	isModalVisible: boolean = false;
	sidePanelOpen: boolean = false;
	stepUid: string;
	sidePanelStorageKey: string = 'MultiAssignments_Component';
	version: number;

	defaultPagingOptions: number[] = AppConstants.DEFAULT_PAGING_OPTIONS;
	rowsPerPage: number = 10;
	assignments: SingleAssignment[] = [];
	selected: SingleAssignment[] = [];
	isLoading: boolean = false;
	formTitle: string = '';
	formDescription: string = '';
	selectedAssignment: WorkflowUserGroupedAssignments;
	@ViewChild('dt', { static: false }) tableEl: Table;
	@ViewChild('modal', { static: false }) modelEl: D3SModal;
	@ViewChild('sidePanelSwitcherComponent') sidePanelSwitcherComponent: SidePanelSwitcherComponent;
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

	private storageKey: string = 'assignmentMultiPickerRowsPerPage';
	private linkInterceptorSubscription: Subscription;
	private workflowTypeUid: string;

	constructor(
		private cdRef: ChangeDetectorRef,
		private sidePanelService: SidePanelService,
		private workflowService: WorkflowService,
		private linkClickInterceptor: LinkClickInterceptor
	) {
		this.subscribeSwitcherEvents();
		this.loadRowsPerPage();
	}

	public openModal(assignments: SingleAssignment[], workflowTypeName: string, workflowTypeUid: string, item?: WorkflowUserGroupedAssignments) {
		this.isModalVisible = true;
		this.isLoading = true;
		this.assignments = assignments;
		this.workflowTypeUid = workflowTypeUid;

		const uniqueTypeNames = Array.from(new Set(this.assignments.map(x => x.AssetTypePath)));
		this.assignmentAssetTypeName = uniqueTypeNames.length === 1 ? uniqueTypeNames[0] : null;

		this.workflowTypeName = workflowTypeName;
		this.selectedAssignment = item;
		this.cdRef.detectChanges();

		this.workflowService.getAssignmentStepDetail(this.assignments[0].ItemStepUid).subscribe((res) => {
			this.version = res.Version;
			if (res.Fields.form) {
				this.formTitle = res.Fields.form['@title'];
				this.formDescription = res.Fields.form['@description'];
			}

			this.cdRef.markForCheck();
			this.isLoading = false;
		});
	}

	public closeDialog() {
		this.isModalVisible = false;
		this.selected = [];
		this.onModalClose.emit();
		this.cdRef.markForCheck();
	}

	public removeSelected() {
		this.selected.forEach((item) => {
			const idx = this.assignments.indexOf(item);
			this.assignments.splice(idx, 1);
		});
		this.selected = [];
		if (this.assignments.length === 0) {
			this.closeDialog();
		}
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

	confirm() {
		this.onAssignmentSelection.emit(
			{
				selectedItems: this.selected,
				selectedAll: this.selected.length === this.assignments.length
			});
		this.linkInterceptorSubscription?.unsubscribe();
		this.modelEl.hide();
	}

	openAssignmentDetails(event: MouseEvent, item: SingleAssignment): void {
		this.sidePanelService.setSidePanelState({ expanded: true });
		this.cdRef.detectChanges();
		setTimeout(() => this.linkClickInterceptor.sendEvent(event, {
			workflowItemUid: item.WorkflowItemUid,
			workflowTypeVersion: this.version,
			workflowTypeUid: this.workflowTypeUid
		}, null));
	}

	private lastSelectedElement: SingleAssignment;

	private triggerRerenderOfSelection() {
		// primeNg library expects us to pass new array whenever we want to change contents of array
		this.selected = this.selected.slice();
		this.cdRef.markForCheck();
	}

	// ignore complexity
	// eslint-disable-next-line
	selectSingleItem(event: MouseEvent, item: SingleAssignment, element: ElementRef = null) {
		//p table options and eventing doesnt handle multiple selection well, this is custom implementation of ctrl/shift holding while selecting
		if (event && element) {
			if ((event.ctrlKey || event.metaKey) && !event.shiftKey) {
				if (this.selected.filter((x) => x.WorkflowItemUid === item.WorkflowItemUid).length > 0) {
					this.selected = this.selected.filter((x) => x.WorkflowItemUid !== item.WorkflowItemUid);
					this.triggerRerenderOfSelection();
				} else {
					this.selected.push(item);
					this.triggerRerenderOfSelection();
				}

				this.lastSelectedElement = item;
				return;
			}
			if (event.shiftKey) {
				let lastIndex = this.assignments.indexOf(this.lastSelectedElement);
				if (lastIndex === -1 && this.selected.length === 1) {
					lastIndex = this.assignments.indexOf(this.selected[0]);
				}
				let currentIndex = this.assignments.indexOf(item);

				if (lastIndex > currentIndex) {
					lastIndex += currentIndex;
					currentIndex = lastIndex - currentIndex;
					lastIndex -= currentIndex;
				}

				const tableRows = (<Table>this.tableEl).el.nativeElement.querySelectorAll('table tbody tr');
				for (let i = lastIndex; i <= currentIndex; i++) {
					if (!tableRows[`${i}`].classList.contains('p-highlight')) {
						this.selected.push(this.assignments[`${i}`]);
						this.triggerRerenderOfSelection();
					}
				}

				this.lastSelectedElement = item;
				return;
			}

		}

		// ignore casting to any error, EventTarget class to do not expose public member nodeName which is used in this code
		// eslint-disable-next-line
		const target = (<any>(event.target));
		if (element && target.nodeName !== 'P-TABLECHECKBOX') {
			this.selected = [];
			this.selected.push(item);
			this.triggerRerenderOfSelection();
			this.lastSelectedElement = item;
		} else {
			if (this.selected.filter((x) => x.WorkflowItemUid === item.WorkflowItemUid).length > 0) {
				this.selected = this.selected.filter((x) => x.WorkflowItemUid !== item.WorkflowItemUid);
				this.triggerRerenderOfSelection();
			} else {
				this.selected.push(item);
				this.triggerRerenderOfSelection();
			}
			this.lastSelectedElement = item;
		}
	}

	setPanelHeader(event: string): void {
		this.sidePanelButtons[0].label = event;
		this.sidePanelButtons[0].tooltip = event;
		this.cdRef.markForCheck();
	}

	ngOnDestroy(): void {
		this.linkInterceptorSubscription?.unsubscribe();
	}

	subscribeSwitcherEvents() {
		this.linkInterceptorSubscription = this.linkClickInterceptor
			.getEvents()
			.subscribe((ev) => {
				this.linkClickInterceptor.handleEvent(
					this.sidePanelSwitcherComponent,
					ev
				);
				this.sidePanelOpen = true;
			});
	}
}
