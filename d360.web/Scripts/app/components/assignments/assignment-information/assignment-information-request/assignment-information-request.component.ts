import { Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import { ActionItems, Actions, FormRequest, WorkflowForm } from '../../../../models/workflow.model';
import { WorkflowService } from '../../../../services/workflow.service';
import { FieldsObservableService } from '../../../../services/fieldsObservable.service';
import { FieldTypeAPIModelField } from '../../../../models/fieldtype-api.model';
import { FormMode } from '../../../../models/form.model';
import { LinkClickInterceptor } from '../../../../services/href-click-service';

@Component({
	selector: 'd3s-assignment-information-request',
	templateUrl: './assignment-information-request.component.html',
	styleUrls: ['./assignment-information-request.component.less']
})
export class AssignmentInformationRequestComponent implements OnInit {
	fieldTypeModelFields: FieldTypeAPIModelField[] = [];

	@Input() set workflowActionUid(value: string) {
		this.loadData(value);
	}

	@Input() workflowItemUid: string;
	@Input() stepUid: string;
	@Input() showSubmittedByData: boolean = false;
	@ViewChild('assetClick') assetClick: ElementRef

	request: FormRequest;

	isLoading: boolean;
	actionItems: ActionItems;

	constructor(private workflowService: WorkflowService,
				private linkClickInterceptor: LinkClickInterceptor,
				private fieldsObservableService: FieldsObservableService) {
	}

	ngOnInit(): void {
		if (this.showSubmittedByData) {
			this.loadFormDetails();
		}
		setTimeout(() => {
			this.assetClick.nativeElement.click();
			});
	}

	loadData(workflowActionUid: string): void {
		this.isLoading = true;
		this.workflowService.getActions(workflowActionUid)
			.subscribe((response: Actions) => {
				if (response?.items?.length > 0) {
					this.actionItems = response.items[0];
					this.fieldsObservableService.getFieldsV2(null, this.actionItems.ActionTypeUid, null)
						.subscribe((response: FieldTypeAPIModelField[]): void => {
							this.fieldTypeModelFields = response;
							this.isLoading = false;
						});
				} else {
					this.isLoading = false;
				}
			});
	}

	private loadFormDetails(): void {
		this.isLoading = true;
		this.workflowService.getWorkflowFormByUid(this.workflowItemUid, this.stepUid)
			.subscribe((res: WorkflowForm) => {
				this.isLoading = false;
				if (res) {
					this.request = res.Request
				}
			});
	}

	onClickResource(event: MouseEvent): void {
		if (this.request?.Action) {
			this.linkClickInterceptor.sendEvent(event, {
				ResourceUid: this.request.Action.CreatedBy
			}, 'users/' + this.request.Action.CreatedBy);
		}
	}

	onClickAsset(event): void {
		if (this.request?.Action) {
			this.linkClickInterceptor.sendEvent(event, {
				AssetUid: this.request.Action.AssociatedAssetUid
			}, 'asset/' + this.request.Action.AssociatedAssetUid);
		}
	}


	isJsonStructure(actionItem: string): boolean {
		try {
			JSON.parse(actionItem);
			return true;
		} catch {
			return false;
		}
	}

	protected readonly Object = Object;
	protected readonly FormMode = FormMode;
	protected readonly JSON = JSON;
}
