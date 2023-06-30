import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AssignmentItem, ChangeTypeInfo } from '../../../../models/workflow.model';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
	selector: 'd3s-assignment-information-general',
	templateUrl: './assignment-information-general.component.html',
	styleUrls: ['./assignment-information-general.component.less']
})
export class AssignmentInformationGeneralComponent {
	isLoading: boolean = false;
	changeTypeInfos: ChangeTypeInfo[] = [];
	private workflowChangeType: string;
	private _assignmentItem: AssignmentItem;
	private assetPathPartIndex: number = -1;

	@Input() set workflowItemUid(value: string) {
		if (value) {
			this.load(value);
		}
	}

	@Input() set assignmentItem(value: AssignmentItem) {
		this._assignmentItem = value;
		this.workflowChangeType = this.changeTypeInfos.find((changeTypeInfo: ChangeTypeInfo) => changeTypeInfo.Name === this.assignmentItem?.ChangeType)?.Description;
		this.assetPathPartIndex = this.assignmentItem?.AssetPath?.lastIndexOf(' > ') ?? -1;
	}

	get assignmentItem(): AssignmentItem {
		return this._assignmentItem;
	}

	@Output() linkClick: EventEmitter<{ objectType: string, objectUid: string }> = new EventEmitter<{
		objectType: string,
		objectUid: string
	}>();

	get assetPathLinkPart(): string {
		return this.assetPathPartIndex >= 0 ? this.assignmentItem?.AssetPath?.substring(this.assetPathPartIndex + 3) : this.assignmentItem?.AssetPath;
	}

	get assetPathTextPart(): string {
		return this.assetPathPartIndex >= 0 ? this.assignmentItem?.AssetPath?.substring(0, this.assetPathPartIndex + 3) : '';
	}

	constructor(private workflowService: WorkflowService) {
		this.workflowService.getChangeTypes().subscribe((response: ChangeTypeInfo[]) => this.changeTypeInfos = response);
	}

	load(workflowItemUid: string): void {
		this.isLoading = true;
		this.workflowService.getAssignmentItem(workflowItemUid).subscribe((response: AssignmentItem): void => {
			this.isLoading = false;
			this.assignmentItem = response;
		});
	}

	get workflowType(): string {
		return this.workflowChangeType + ': ' + this.assignmentItem?.initiatingObjectType;
	}
}
