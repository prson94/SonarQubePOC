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
		if (this.settings?.RelationshipUpdate?.Relationship) {
			const formStepId: string = this.settings.RelationshipUpdate.Relationship['@FormStepId'];
			const formFieldId: string = this.settings.RelationshipUpdate.Relationship['@FormFieldId'];
			if (this.nodeList?.length > 0) {
				for (const workflowDiagramNode of this.nodeList) {
					if (workflowDiagramNode?.Key === formStepId) {
						for (const fieldElement of workflowDiagramNode?.FieldsObject?.form?.field) {
							if (fieldElement?.['@id'] === formFieldId) {
								this.relationshipFormField = fieldElement['@label'];
							}
						}
					}
				}
			}
		}
	}
}
