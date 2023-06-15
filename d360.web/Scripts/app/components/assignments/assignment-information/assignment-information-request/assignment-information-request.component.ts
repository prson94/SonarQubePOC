import {Component, Input, OnInit} from '@angular/core';
import {Actions} from '../../../../models/workflow.model';
import {WorkflowService} from '../../../../services/workflow.service';
import {FieldsObservableService} from '../../../../services/fieldsObservable.service';
import {FieldTypeAPIModelField} from '../../../../models/fieldtype-api.model';

@Component({
	selector: 'd3s-assignment-information-request',
	templateUrl: './assignment-information-request.component.html',
	styleUrls: ['./assignment-information-request.component.less']
})
export class AssignmentInformationRequestComponent implements OnInit {
	private _workflowActionUid: string;
	fieldTypeModelFields: FieldTypeAPIModelField[] = [];

	@Input() set workflowActionUid(value: string) {
		this._workflowActionUid = value;
		this.loadData();
	}

	isLoading: boolean;
	actions: Actions;

	constructor(private workflowService: WorkflowService,
				private fieldsObservableService: FieldsObservableService) {
	}

	ngOnInit(): void {
	}

	loadData() {
		this.isLoading = true;
		this.workflowService.getActions(this._workflowActionUid)
			.subscribe(response => {
				this.actions = response;
				if (this.actions.items?.length > 0) {
					this.fieldsObservableService.getFieldsV2(null, this.actions.items[0].ActionTypeUid, null)
						.subscribe(response => {
							this.fieldTypeModelFields = response;
							this.isLoading = false;
						});
				} else {
					this.isLoading = false;
				}
			});
	}

}
