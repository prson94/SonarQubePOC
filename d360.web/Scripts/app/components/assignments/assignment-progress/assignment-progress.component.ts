import { Component, Input, OnInit } from '@angular/core'
import { WorkflowService } from '../../../services/workflow.service'
import { WorkflowItemStep } from '../../../models/workflow.model'

@Component({
	selector: 'd3s-assignment-progress',
	templateUrl: './assignment-progress.component.html',
	styleUrls: ['./assignment-progress.component.less']
})
export class AssignmentProgressComponent implements OnInit {
	private _workflowItemId: number
	private itemSteps: WorkflowItemStep[]

	@Input() workflowItemId(value: number) {
		this._workflowItemId = value
		this.loadData()
	}

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
	}

	private loadData(): void {
		this.itemSteps = null
		// this.object = null
		// this.objectId = 0
		// this.isIssueType = false
		// this.selection = null
		if (this._workflowItemId) {
			this.workflowService.getWorkflowItemSteps(this._workflowItemId)
				.subscribe((response: WorkflowItemStep[]) => {
					this.itemSteps = response
					if (this.itemSteps) {
						// this.selectionChange.emit(this.selection)
						// this.isIssueType = this.itemSteps[0].IsIssueType

						// this.object = this.itemSteps[0].Object
						// this.objectId = this.itemSteps[0].ObjectID
					}
					// this.ref.markForCheck()
					//console.log('loaded', this.itemSteps);
				})
		}
	}
}
