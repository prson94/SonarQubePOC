import { Component, Input } from '@angular/core';
import { ActionItems, Actions } from '../../../../models/workflow.model';
import { WorkflowService } from '../../../../services/workflow.service';
import { FieldsObservableService } from '../../../../services/fieldsObservable.service';
import { FieldTypeAPIModelField } from '../../../../models/fieldtype-api.model';
import { FormMode } from '../../../../models/form.model';

@Component({
	selector: 'd3s-assignment-information-request',
	templateUrl: './assignment-information-request.component.html',
	styleUrls: ['./assignment-information-request.component.less']
})
export class AssignmentInformationRequestComponent {
	private _workflowActionUid: string;
	fieldTypeModelFields: FieldTypeAPIModelField[] = [];

	@Input() set workflowActionUid(value: string) {
		this._workflowActionUid = value;
		this.loadData();
	}

	isLoading: boolean;
	actionItems: ActionItems;

	constructor(private workflowService: WorkflowService,
				private fieldsObservableService: FieldsObservableService) {
	}

	loadData() {
		this.isLoading = true;
		this.workflowService.getActions(this._workflowActionUid)
			.subscribe((response: Actions) => {
				if (response?.items?.length > 0) {
					this.actionItems = response.items[0]
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

	protected readonly Object = Object;
	protected readonly FormMode = FormMode;
}
