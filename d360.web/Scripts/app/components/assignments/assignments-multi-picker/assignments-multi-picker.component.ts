import { ChangeDetectorRef, ChangeDetectionStrategy, Component, OnInit, EventEmitter, Output } from '@angular/core';
import { SingleAssignment } from '../../../models/workflow.model';

@Component({
	selector: 'd3s-assignments-multi-picker',
	templateUrl: './assignments-multi-picker.component.html',
	styleUrls: ['./assignments-multi-picker.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssignmentsMultiPickerComponent implements OnInit {
	@Output() onAssignmentSelection = new EventEmitter<SingleAssignment[]>();

	isModalVisible: boolean = false;
	sidePanelOpen: boolean = false;
	workflowItemUid: string;
	stepUid: string;
	assetId: number;
	sidePanelStorageKey: string = 'MultiAssignments_Component';
	sidePanel: string = 'asset-details';

	assignments: SingleAssignment[] = [];
	selected: SingleAssignment[];

	constructor(private cdRef: ChangeDetectorRef) { }

	ngOnInit(): void {
	}

	public openModal(assignments: SingleAssignment[]) {
		this.assignments = assignments;
		this.isModalVisible = true;
		this.cdRef.markForCheck();
	}

	public closeDialog() {
		this.isModalVisible = false;
		this.cdRef.markForCheck();
	}

	onFormSubmit() {
		this.onAssignmentSelection.emit(this.selected);
	}

}
