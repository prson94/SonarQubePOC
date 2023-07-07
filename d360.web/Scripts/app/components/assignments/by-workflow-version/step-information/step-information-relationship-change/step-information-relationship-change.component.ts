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
			let formStepId: string = this.settings.RelationshipUpdate.Relationship['@FormStepId'];
			let formFieldId: string = this.settings.RelationshipUpdate.Relationship['@FormFieldId'];
			if (this.nodeList && this.nodeList.length > 0) {
				for (let i: number = 0; i < this.nodeList.length; i++) {
					if (this.nodeList[i].Key === formStepId) {
						for (let j: number = 0; j < this.nodeList[i].FieldsObject?.form?.field?.length; j++) {
							if (this.nodeList[i].FieldsObject.form.field[j]['@id'] === formFieldId) {
								this.relationshipFormField = this.nodeList[i].FieldsObject.form.field[j]['@label'];
							}
						}
					}
				}
			}
		}
	}

}
