import { Component, Input } from '@angular/core';
import { NodeModel } from '../../../../../models/workflow.model';

@Component({
	selector: 'd3s-step-information-form-fields',
	templateUrl: './step-information-form-fields.component.html',
	styleUrls: ['step-information-form-fields.component.less']
})
export class StepInformationFormFieldsComponent {
	@Input() selectedNode: NodeModel;
}
