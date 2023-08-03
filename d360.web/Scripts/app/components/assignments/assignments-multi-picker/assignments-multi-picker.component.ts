import { ChangeDetectorRef, ChangeDetectionStrategy, Component, OnInit, EventEmitter, Output, ViewEncapsulation } from '@angular/core';
import { SingleAssignment } from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';

@Component({
	selector: 'd3s-assignments-multi-picker',
	templateUrl: './assignments-multi-picker.component.html',
	styleUrls: ['./assignments-multi-picker.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
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
	selected: SingleAssignment[] = [];
	isLoading: boolean = false;
	formTitle: string = '';
	formDescription: string = '';
	constructor(
		private cdRef: ChangeDetectorRef,
		private workflowService: WorkflowService
	) { }

	ngOnInit(): void {
	}

	public openModal(assignments: SingleAssignment[]) {
		this.isModalVisible = true;
		this.isLoading = true;
		this.assignments = assignments;
		this.cdRef.detectChanges();

		this.workflowService.getAssignmentStepDetail(this.assignments[0].ItemStepUid).subscribe((res) => {
			console.log(res);
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

	confirm() {
		console.log("here");
		this.onAssignmentSelection.emit(this.selected);

	}

	openAssetSidePanel(item: SingleAssignment) {
		console.log(item);
	}
}
