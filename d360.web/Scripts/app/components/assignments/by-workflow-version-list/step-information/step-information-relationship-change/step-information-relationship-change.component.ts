import { Component, Input, OnInit } from '@angular/core';
import { NodeSettings, WorkflowDiagramNode } from '../../../../../models/workflow.model';

@Component({
	selector: 'd3s-step-information-relationship-change',
	templateUrl: './step-information-relationship-change.component.html',
	styleUrls: ['./step-information-relationship-change.component.less']
})
export class StepInformationRelationshipChangeComponent implements OnInit {
	@Input() settings: NodeSettings;
	@Input() nodeList: WorkflowDiagramNode[];
	relationshipFormField: string = '';

	ngOnInit(): void {
		for (const workflowDiagramNode of this.nodeList) {
			if (workflowDiagramNode?.Key === this.settings?.RelationshipUpdate?.Relationship?.['@FormStepId'] &&
				Array.isArray(workflowDiagramNode?.FieldsObject?.form?.field)) {
				for (const fieldElement of workflowDiagramNode.FieldsObject.form.field) {
					if (fieldElement?.['@id'] === this.settings?.RelationshipUpdate?.Relationship?.['@FormFieldId']) {
						this.relationshipFormField = fieldElement['@label'];
						return;
					}
				}
			}
		}
	}
}
