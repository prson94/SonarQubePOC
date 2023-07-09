import { Component, Input, OnInit } from '@angular/core';
import {
	NodeSettings,
	WorkflowActivityType,
	WorkflowEventRegistration,
	WorkflowStepFieldChangeDetail
} from '../../../../../models/workflow.model';
import { WorkflowService } from '../../../../../services/workflow.service';
import { FieldType } from '../../../../../models/fields.model';

@Component({
	selector: 'd3s-step-information-field-change',
	templateUrl: './step-information-field-change.component.html',
	styleUrls: ['./step-information-field-change.component.less']
})
export class StepInformationFieldChangeComponent implements OnInit {
	@Input() settings: NodeSettings;
	@Input() workflowEvent: WorkflowEventRegistration;
	protected readonly WorkflowActivityType = WorkflowActivityType;
	fields: FieldType[] = [];

	constructor(private workflowService: WorkflowService) {
	}

	getFieldName(item: WorkflowStepFieldChangeDetail): string {
		if (item['@ObjectType'] === '') {
			return item['@FieldName'];
		} else if (item['@ObjectType'] === 'Issue') {
			return 'Action Field::' + item['@FieldName'];
		} else {
			const field = this.fields.find((f) => f.ID === +item['@FieldId']);
			if (field == null) {
				return '';
			}
			return $localize`Asset Field` + '::' + field.FriendlyName;
		}
	}

	ngOnInit(): void {
		if (this.workflowEvent) {
			this.workflowService.getWorkflowFieldTypes(this.workflowEvent.ObjectID, this.workflowEvent.Object, true, this.workflowEvent.IssueObject)
				.subscribe((fields: FieldType[]) => {
					this.fields = fields;
				});
		}
		if (!Array.isArray(this.settings.FieldUpdate.Field)) {
			this.settings.FieldUpdate.Field = [...this.settings.FieldUpdate.Field];
		}
	}

}
