import { ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
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

	@Input({ required: true }) set workflowActionUid(value: string) {
		this.fieldTypeModelFields = [];
		this.actionItems = null;
		if (value) {
			this.loadActionDetails(value);
		}
	}

	@Input() workflowItemUid: string;
	@Input() stepUid: string;
	@Input() showSubmittedByData: boolean = false;

	request: FormRequest;

	isFormDetailsLoading: boolean = false;
	isActionDetailsLoading: boolean = false;
	actionItems: ActionItems;

	constructor(private workflowService: WorkflowService,
				private linkClickInterceptor: LinkClickInterceptor,
				private fieldsObservableService: FieldsObservableService,
				private changeDetectorRef: ChangeDetectorRef) {
	}

	ngOnInit(): void {
		if (this.showSubmittedByData) {
			this.loadFormDetails();
		}
	}

	private loadActionDetails(workflowActionUid: string): void {
		this.isActionDetailsLoading = true;
		this.workflowService.getActions(workflowActionUid)
			.subscribe((response: Actions) => {
				if (response?.items?.length > 0) {
					this.actionItems = response.items[0];
					this.fieldsObservableService.getFieldsV2(null, this.actionItems.ActionTypeUid, null)
						.subscribe((response: FieldTypeAPIModelField[]): void => {
							this.fieldTypeModelFields = response;
							this.isActionDetailsLoading = false;
							this.changeDetectorRef.markForCheck();
						});
				} else {
					this.isActionDetailsLoading = false;
					this.changeDetectorRef.markForCheck();
				}
			});
	}

	private loadFormDetails(): void {
		this.isFormDetailsLoading = true;
		this.workflowService.getWorkflowFormByUid(this.workflowItemUid, this.stepUid)
			.subscribe((res: WorkflowForm) => {
				this.isFormDetailsLoading = false;
				this.request = res?.Request;
				this.changeDetectorRef.markForCheck();
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
