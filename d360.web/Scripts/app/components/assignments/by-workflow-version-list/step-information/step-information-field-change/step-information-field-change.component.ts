import { Component, Input, OnInit } from '@angular/core';
import {
	NodeSettings,
	WorkflowActivityType,
	WorkflowEventRegistration,
	WorkflowStepFieldChangeDetail
} from '../../../../../models/workflow.model';
import { WorkflowService } from '../../../../../services/workflow.service';
import { FieldType } from '../../../../../models/fields.model';

/*global $localize*/

@Component({
	selector: 'd3s-step-information-field-change',
	templateUrl: './step-information-field-change.component.html'
})
export class StepInformationFieldChangeComponent implements OnInit {
	@Input() settings: NodeSettings;
	@Input() workflowEvent: WorkflowEventRegistration;
	protected readonly WorkflowActivityType = WorkflowActivityType;
	fields: FieldType[] = [];

	constructor(private workflowService: WorkflowService) {
	}

	getFieldName(item: WorkflowStepFieldChangeDetail): string {
		const field: FieldType = this.fields.find((f) => f.ID === +item['@FieldId']);
		if (field == null) {
			return '';
		}
		if (this.workflowEvent?.IssueObject === '') {
			return field.FriendlyName;
		}
		if (item['@ObjectType'] === 'Issue') {
			return $localize`Action Field::` + field.FriendlyName;
		} else {
			return $localize`Asset Field::` + field.FriendlyName;
		}
	}

	getChangeType(item: WorkflowStepFieldChangeDetail): string {
		if (item['@AppendValue'] === 'true') {
			return 'Append';
		} else if (item['@ClearValue'] === 'true') {
			return 'Clear';
		} else {
			return 'Replace';
		}
	}

	getValueSource(item: WorkflowStepFieldChangeDetail): string {
		if (item['@UseFormValue'] === true) {
			if (item['@ObjectType'] === 'Issue') {
				return 'Action Form Input';
			} else {
				return 'Form Input';
			}
		} else if (item['@ClearValue'] === 'true') {
			return '--';
		} else {
			return 'Specific Value';
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
