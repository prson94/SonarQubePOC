import { Component, Input, OnInit } from '@angular/core';
import { NodeModel } from '../../../../models/workflow.model';

@Component({
  selector: 'd3s-step-information',
  templateUrl: './step-information.component.html',
  styleUrls: ['./step-information.component.less']
})
export class StepInformationComponent implements OnInit{
	@Input() selectedNode: NodeModel
	isLoading: boolean = false

	ngOnInit(): void {
		console.log(this.selectedNode)
	}

}
