import { Component, Input } from '@angular/core';
import { AssignmentItem, ChangeTypeInfo } from '../../../../models/workflow.model';
import { WorkflowService } from '../../../../services/workflow.service';
import { LinkClickInterceptor } from '../../../../services/href-click-service';

@Component({
	selector: 'd3s-assignment-information-general',
	templateUrl: './assignment-information-general.component.html',
	styleUrls: ['./assignment-information-general.component.less']
})
export class AssignmentInformationGeneralComponent {
	@Input({ required: true }) workflowTypeVersion: number;
	@Input({ required: true }) set workflowItemUid(value: string) {
		if (value) {
			this.loadAssignmentItem(value);
		}
	}
	@Input() isCollapsed: boolean = false;
	@Input() shouldBePadded: boolean = false;
	isLoading: boolean = false;
	changeTypeInfos: ChangeTypeInfo[] = [];
	assignmentItem: AssignmentItem;
	private workflowChangeType: string;
	private assetPathPartIndex: number = -1;

	get AssociatedWithEmpty(): boolean {
		if (this.assignmentItem?.initiatingObjectType === 'Relationship' || !this.assignmentItem?.AssetUid) {
			return this.assetPathTextPart.length === 0 ? true : false;
		} else {
			const assetpath = this.assetPathPartIndex >= 0 ? this.assignmentItem?.AssetPath?.substring(this.assetPathPartIndex + 3) : this.assignmentItem?.AssetPath;
			return !assetpath ? true : false;
		}
	}

	get assetPathLinkPart(): string {
		if (this.assignmentItem?.initiatingObjectType === 'Relationship' || !this.assignmentItem?.AssetUid) {
			return '';
		} else {
			return this.assetPathPartIndex >= 0 ? this.assignmentItem?.AssetPath?.substring(this.assetPathPartIndex + 3) : this.assignmentItem?.AssetPath;
		}
	}

	get assetPathTextPart(): string {
		if (this.assignmentItem?.initiatingObjectType === 'Relationship' || !this.assignmentItem?.AssetUid) {
			return this.assignmentItem?.AssetPath ? this.assignmentItem?.AssetPath : '';
		} else {
			return this.assetPathPartIndex >= 0 ? this.assignmentItem?.AssetPath?.substring(0, this.assetPathPartIndex + 3) : '';
		}
	}

	get workflowType(): string {
		return this.workflowChangeType + ': ' + this.assignmentItem?.initiatingObjectType;
	}

	constructor(private workflowService: WorkflowService, public linkClickInterceptor: LinkClickInterceptor) {
		this.workflowService.getChangeTypes().subscribe((response: ChangeTypeInfo[]) => this.changeTypeInfos = response);
	}

	onClickResource(event: MouseEvent): void {
		this.linkClickInterceptor.sendEvent(event, {
			ResourceUid: this.assignmentItem?.InitiatorUid
		}, 'users/' + this.assignmentItem?.InitiatorUid);
	}

	onClickAsset(event: MouseEvent): void {
		this.linkClickInterceptor.sendEvent(event, {
			AssetUid: this.assignmentItem?.AssetUid
		}, 'asset/' + this.assignmentItem?.AssetUid);
	}

	forceRefresh() {
		if (this.assignmentItem?.WorkflowItemUid) {
			this.loadAssignmentItem(this.assignmentItem.WorkflowItemUid);
		}
	}

	private loadAssignmentItem(workflowItemUid: string): void {
		this.isLoading = true;
		this.workflowService.getAssignmentItem(workflowItemUid).subscribe((response: AssignmentItem): void => {
			this.isLoading = false;
			this.assignmentItem = response;
			this.workflowChangeType = this.changeTypeInfos.find((changeTypeInfo: ChangeTypeInfo) => changeTypeInfo.Name === this.assignmentItem?.ChangeType)?.Description;
			this.assetPathPartIndex = this.assignmentItem?.AssetPath?.lastIndexOf(' > ') ?? -1;
		});
	}
}
