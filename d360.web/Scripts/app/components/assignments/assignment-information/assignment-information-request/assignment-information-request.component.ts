import { Component, Input, OnInit } from '@angular/core';
import { ActionItems, Actions, FormRequest, WorkflowForm } from '../../../../models/workflow.model';
import { WorkflowService } from '../../../../services/workflow.service';
import { FieldsObservableService } from '../../../../services/fieldsObservable.service';
import { FieldTypeAPIModelField } from '../../../../models/fieldtype-api.model';
import { FormMode } from '../../../../models/form.model';

@Component({
	selector: 'd3s-assignment-information-request',
	templateUrl: './assignment-information-request.component.html',
	styleUrls: ['./assignment-information-request.component.less']
})
export class AssignmentInformationRequestComponent implements OnInit {
	fieldTypeModelFields: FieldTypeAPIModelField[] = [];

	@Input() workflowActionUid: string;
	@Input() workflowItemUid: string;
	@Input() stepUid: string;
	@Input() showSubmittedByData: boolean = false;
	request: FormRequest;

	isLoading: boolean;
	actionItems: ActionItems;

	constructor(private workflowService: WorkflowService,
				private fieldsObservableService: FieldsObservableService) {
	}

	ngOnInit(): void {
		this.loadData();
		if (this.showSubmittedByData) {
			this.loadFormDetails();
		}
	}

	loadData(): void {
		this.isLoading = true;
		this.workflowService.getActions(this.workflowActionUid)
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
					this.request = res.Request;
				}
			});
	}

	protected readonly Object = Object;
	protected readonly FormMode = FormMode;
	protected readonly JSON = JSON;
}
