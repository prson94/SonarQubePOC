import { ChangeDetectorRef, ChangeDetectionStrategy, Component, EventEmitter, Output, ViewEncapsulation, ElementRef, ViewChild, Input } from '@angular/core';
import { Table } from 'primeng/table';
import { SingleAssignment } from '../../../models/workflow.model';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { SidePanelService } from '../../../services/side-panel.service';
import { WorkflowService } from '../../../services/workflow.service';
import { D3SModal } from '../../shared/modal/gov-modal.component';

@Component({
	selector: 'd3s-assignments-multi-picker',
	templateUrl: './assignment-multi-picker.component.html',
	styleUrls: ['./assignment-multi-picker.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})
export class AssignmentsMultiPickerComponent {
	@Output() onAssignmentSelection = new EventEmitter<SingleAssignment[]>();
	@Input() onlyAdminReassignMode: boolean = false;

	workflowTypeName: string;

	isModalVisible: boolean = false;
	sidePanelOpen: boolean = false;
	stepUid: string;
	sidePanelStorageKey: string = 'MultiAssignments_Component';
	sidePanel: string = 'asset-details';
	version: number;
	selectedForInfoPanel: { assetUid: string, type: string, workflowItemUid: string, Version: number };

	assignments: SingleAssignment[] = [];
	selected: SingleAssignment[] = [];
	isLoading: boolean = false;
	formTitle: string = '';
	formDescription: string = '';

	@ViewChild('dt', { static: false }) tableEl: Table;
	@ViewChild('modal', { static: false }) modelEl: D3SModal;

	constructor(
		private cdRef: ChangeDetectorRef,
		private sidePanelService: SidePanelService,
		private workflowService: WorkflowService,
		private hrefService: LinkClickInterceptor
	) {
		this.hrefService.getEvents().subscribe((res) => {
			this.sidePanel = 'asset-details';
			this.selectedForInfoPanel = { type: res.objectType, assetUid: res.uid, workflowItemUid: null, Version: null };
		});
	}

	public openModal(assignments: SingleAssignment[], workflowTypeName: string) {
		this.isModalVisible = true;
		this.isLoading = true;
		this.assignments = assignments;
		this.workflowTypeName = workflowTypeName;
		this.sidePanel = 'asset-details';
		this.selectedForInfoPanel = { type: null, assetUid: null, workflowItemUid: null, Version: null };
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

	confirm() {
		this.onAssignmentSelection.emit(this.selected);
		this.modelEl.hide();
	}

	openAssetSidePanel(item: SingleAssignment) {
		this.sidePanel = 'step-details';
		this.selectedForInfoPanel = { type: null, assetUid: null, workflowItemUid: item.WorkflowItemUid, Version: this.version };
		this.sidePanelService.setSidePanelState({ expanded: true });
		this.cdRef.markForCheck();
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
				}
				else {
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
		if (element && target.nodeName !== "P-TABLECHECKBOX") {
			this.selected = [];
			this.selected.push(item);
			this.triggerRerenderOfSelection();
			this.lastSelectedElement = item;
		} else {
			if (this.selected.filter((x) => x.WorkflowItemUid === item.WorkflowItemUid).length > 0) {
				this.selected = this.selected.filter((x) => x.WorkflowItemUid !== item.WorkflowItemUid);
				this.triggerRerenderOfSelection();
			}
			else {
				this.selected.push(item);
				this.triggerRerenderOfSelection();
			}
			this.lastSelectedElement = item;
		}
	}

}
